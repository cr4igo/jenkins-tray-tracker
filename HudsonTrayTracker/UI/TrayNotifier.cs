using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Hudson.TrayTracker.BusinessComponents;
using Common.Logging;
using System.Reflection;
using Hudson.TrayTracker.Entities;
using Hudson.TrayTracker.Utils.Logging;
using Iesi.Collections.Generic;
using DevExpress.XtraEditors;
using Hudson.TrayTracker.Utils;
using Spring.Context.Support;
using DevExpress.Utils;

namespace Hudson.TrayTracker.UI
{
    public partial class TrayNotifier : Component
    {
        static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static TrayNotifier Instance
        {
            get
            {
                TrayNotifier instance = (TrayNotifier)ContextRegistry.GetContext().GetObject("TrayNotifier");
                return instance;
            }
        }

        BuildStatus lastBuildStatus;
        IDictionary<Project, AllBuildDetails> lastProjectsBuildDetails = new Dictionary<Project, AllBuildDetails>();
        IDictionary<Project, BuildStatus> acknowledgedStatusByProject = new Dictionary<Project, BuildStatus>();
        IDictionary<string, Icon> iconsByKey;

        public ConfigurationService ConfigurationService { get; set; }
        public HudsonService HudsonService { get; set; }
        public ProjectsUpdateService UpdateService { get; set; }
        public NotificationService NotificationService { get; set; }

        public TrayNotifier()
        {
            InitializeComponent();
            LoadIcons();
        }

        public void Initialize()
        {
            ConfigurationService.ConfigurationUpdated += configurationService_ConfigurationUpdated;
            UpdateService.ProjectsUpdated += updateService_ProjectsUpdated;

            Disposed += delegate
            {
                ConfigurationService.ConfigurationUpdated -= configurationService_ConfigurationUpdated;
                UpdateService.ProjectsUpdated -= updateService_ProjectsUpdated;
            };
        }

        void configurationService_ConfigurationUpdated()
        {
            UpdateNotifier();
        }

#if false
        private delegate void ProjectsUpdatedDelegate();
        private void updateService_ProjectsUpdated()
        {
            Delegate del = new ProjectsUpdatedDelegate(OnProjectsUpdated);
            MainForm.Instance.BeginInvoke(del);
        }
        private void OnProjectsUpdated()
        {
            UpdateGlobalStatus();
        }
#else
        private void updateService_ProjectsUpdated()
        {
            UpdateNotifier();
        }
#endif

        // FIXME: the framework doesn't fire correctly MouseClick and MouseDoubleClick,
        // so this is deactivated
        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            try
            {
                // order the projects by build status
                var projectsByStatus = new Dictionary<BuildStatusEnum, SortedSet<Project>>();
                foreach (KeyValuePair<Project, AllBuildDetails> pair in lastProjectsBuildDetails)
                {
                    BuildStatusEnum status = BuildStatusEnum.Unknown;
                    if (pair.Value != null)
                        status = BuildStatusUtils.DegradeStatus(pair.Value.Status).Value;
                    SortedSet<Project> projects = new SortedSet<Project>();
                    if (projectsByStatus.TryGetValue(status, out projects) == false)
                    {
                        projects = new SortedSet<Project>();
                        projectsByStatus.Add(status, projects);
                    }
                    projects.Add(pair.Key);
                }

                StringBuilder text = new StringBuilder();
                string prefix = null;
                foreach (KeyValuePair<BuildStatusEnum, SortedSet<Project>> pair in projectsByStatus)
                {
                    // don't display successful projects unless this is the only status
                    if (pair.Key == BuildStatusEnum.Successful || projectsByStatus.Count == 1)
                        continue;

                    if (prefix != null)
                        text.Append(prefix);
                    string statusText = HudsonTrayTrackerResources.ResourceManager
                        .GetString("BuildStatus_" + pair.Key.ToString());
                    text.Append(statusText);
                    foreach (Project project in pair.Value)
                    {
                        text.Append("\n  - ").Append(project.DisplayName);

                        BuildDetails lastFailedBuild = project.LastFailedBuild;
                        if (lastFailedBuild != null && lastFailedBuild.Users != null && lastFailedBuild.Users.Count > 0)
                        {
                            string users = StringUtils.Join(lastFailedBuild.Users, ", ");
                            text.Append(" (").Append(users).Append(")");
                        }
                    }
                    prefix = "\n";
                }

                string textToDisplay = text.ToString();
                if (string.IsNullOrEmpty(textToDisplay))
                    textToDisplay = HudsonTrayTrackerResources.DisplayBuildStatus_NoProjects;
                notifyIcon.ShowBalloonTip(10000, HudsonTrayTrackerResources.DisplayBuildStatus_Caption,
                    textToDisplay, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(logger, ex);
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            MainForm.ShowOrFocus();
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            MainForm.ShowOrFocus();
        }

        private void refreshMenuItem_Click(object sender, EventArgs e)
        {
            UpdateService.UpdateProjects();
        }

        private void settingsMenuItem_Click(object sender, EventArgs e)
        {
            MainForm.Instance.Show();
            SettingsForm.ShowDialogOrFocus();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainForm.Instance.Exit();
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            MainForm.Instance.Show();
            AboutForm.ShowDialogOrFocus();
        }

        public void UpdateNotifier()
        {
            try
            {
                DoUpdateNotifier();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(logger, ex);
                UpdateIcon(BuildStatus.UNKNOWN_BUILD_STATUS);
            }
        }

        private void DoUpdateNotifier()
        {
            BuildStatusEnum? worstBuildStatus = null;
            bool buildInProgress = false;
            bool buildIsStuck = false;
            var errorProjects = new HashedSet<Project>();
            var regressingProjects = new HashedSet<Project>();

            foreach (Server server in ConfigurationService.Servers)
            {
                foreach (Project project in server.Projects)
                {
                    BuildStatus status = GetProjectStatus(project);
                    if (worstBuildStatus == null || status.Value > worstBuildStatus)
                        worstBuildStatus = status.Value;
                    if (status.Value >= BuildStatusEnum.Failed)
                        errorProjects.Add(project);
                    if (status.IsInProgress)
                        buildInProgress = true;
                    if (status.IsStuck)
                        buildIsStuck = true;
                    if (IsRegressing(project))
                        regressingProjects.Add(project);
                    lastProjectsBuildDetails[project] = project.AllBuildDetails;
                }
            }

            if (worstBuildStatus == null)
                worstBuildStatus = BuildStatusEnum.Unknown;

#if false // tests
            lastBuildStatus++;
            if (lastBuildStatus > BuildStatus.Failed_BuildInProgress)
                lastBuildStatus = 0;
            worstBuildStatus = lastBuildStatus;
            Console.WriteLine("tray:"+lastBuildStatus);
#endif

            BuildStatus buildStatus = new BuildStatus(worstBuildStatus.Value, buildInProgress, buildIsStuck);

            UpdateIcon(buildStatus);
            UpdateBalloonTip(errorProjects, regressingProjects);

            lastBuildStatus = buildStatus;
        }

        private BuildStatus GetProjectStatus(Project project)
        {
            BuildStatus status = project.Status;
            BuildStatus acknowledgedStatus = GetAcknowledgedStatus(project);
            if (acknowledgedStatus != null)
            {
                if (status.Value == acknowledgedStatus.Value)
                    return new BuildStatus(BuildStatusEnum.Successful, false, false);
                else if (status.Value != BuildStatusEnum.Unknown && BuildStatusUtils.IsWorse(acknowledgedStatus, status))
                    ClearAcknowledgedStatus(project);
            }
            return status;
        }

        private bool IsRegressing(Project project)
        {
            AllBuildDetails lastBuildDetails;
            if (lastProjectsBuildDetails.TryGetValue(project, out lastBuildDetails) == false
                || lastBuildDetails == null)
                return false;
            AllBuildDetails newBuildDetails = project.AllBuildDetails;
            if (newBuildDetails == null)
                return false;

            // moving from unknown/aborted to successful should not be considered as a regression
            if (newBuildDetails.Status.Value <= BuildStatusEnum.Successful)
                return false;

            bool res = BuildStatusUtils.IsWorse(newBuildDetails.Status, lastBuildDetails.Status);
            return res;
        }

        private void UpdateBalloonTip(ICollection<Project> errorProjects, ICollection<Project> regressingProjects)
        {
            if (lastBuildStatus != null && lastBuildStatus.Value < BuildStatusEnum.Failed
                && errorProjects != null && errorProjects.Count > 0)
            {
                StringBuilder errorProjectsText = new StringBuilder();
                string prefix = null;
                foreach (Project project in errorProjects)
                {
                    if (prefix != null)
                        errorProjectsText.Append(prefix);
                    BuildDetails buildDetails = project.LastFailedBuild;
                    if (buildDetails == null)
                        logger.Warn("No details for the last failed build of project in error: " + project.Url);
                    ISet<string> users = buildDetails != null ? buildDetails.Users : null;
                    FormatProjectDetails(project.DisplayName, users, errorProjectsText);
                    prefix = "\n";
                }

                notifyIcon.ShowBalloonTip(10000, HudsonTrayTrackerResources.BuildFailed_Caption,
                    errorProjectsText.ToString(), ToolTipIcon.Error);
            }
            else if (regressingProjects != null && regressingProjects.Count > 0)
            {
                StringBuilder regressingProjectsText = new StringBuilder();
                string prefix = null;
                foreach (Project project in regressingProjects)
                {
                    if (prefix != null)
                        regressingProjectsText.Append(prefix);
                    BuildDetails buildDetails = project.AllBuildDetails.LastCompletedBuild;
                    if (buildDetails == null)
                        logger.Warn("No details for the last failed build of project in error: " + project.Url);
                    ISet<string> users = buildDetails != null ? buildDetails.Users : null;
                    FormatProjectDetails(project.DisplayName, users, regressingProjectsText);
                    prefix = "\n";
                }

                notifyIcon.ShowBalloonTip(10000, HudsonTrayTrackerResources.BuildRegressions_Caption,
                    regressingProjectsText.ToString(), ToolTipIcon.Warning);
            }
        }

        private void FormatProjectDetails(string projectName, ISet<string> users, StringBuilder builder)
        {
            builder.Append(projectName);

            if (users != null && users.Count > 0)
            {
                string userString = StringUtils.Join(users, ", ");
                builder.Append(" (").Append(userString).Append(")");
            }
        }

        private void UpdateIcon(BuildStatus buildStatus)
        {
            Icon icon = iconsByKey[buildStatus.Key];
            notifyIcon.Icon = icon;

            // update the main window's icon
            if (ConfigurationService.GeneralSettings.UpdateMainWindowIcon)
                MainForm.Instance.UpdateIcon(icon);
        }

        private void LoadIcons()
        {
            iconsByKey = new Dictionary<string, Icon>();

            foreach (BuildStatusEnum statusValue in Enum.GetValues(typeof(BuildStatusEnum)))
            {
                LoadIcon(statusValue, false, false);
                LoadIcon(statusValue, false, true);
                LoadIcon(statusValue, true, false);
                LoadIcon(statusValue, true, true);
            }
        }

        private void LoadIcon(BuildStatusEnum statusValue, bool isInProgress, bool isStuck)
        {
            BuildStatus status = new BuildStatus(statusValue, isInProgress, isStuck);
            if (iconsByKey.ContainsKey(status.Key))
                return;

            try
            {
                string resourceName = string.Format("Hudson.TrayTracker.Resources.TrayIcons.{0}.ico", status.Key);
                Icon icon = ResourceImageHelper.CreateIconFromResources(resourceName, GetType().Assembly);
                iconsByKey.Add(status.Key, icon);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(HudsonTrayTrackerResources.FailedLoadingIcons_Text,
                    HudsonTrayTrackerResources.FailedLoadingIcons_Caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingHelper.LogError(logger, ex);
                throw new Exception("Failed loading icon: " + statusValue, ex);
            }
        }

        private void notifyIcon_MouseUp(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.Clicks);
        }

        public void AcknowledgeStatus(Project project, BuildStatus currentStatus)
        {
            lock (acknowledgedStatusByProject)
            {
                acknowledgedStatusByProject[project] = currentStatus;
            }
            UpdateNotifier();
        }

        public void ClearAcknowledgedStatus(Project project)
        {
            lock (acknowledgedStatusByProject)
            {
                acknowledgedStatusByProject.Remove(project);
            }
            UpdateNotifier();
        }

        private BuildStatus GetAcknowledgedStatus(Project project)
        {
            BuildStatus status;
            lock (acknowledgedStatusByProject)
            {
                if (acknowledgedStatusByProject.TryGetValue(project, out status) == false)
                    return null;
            }
            return status;
        }

        public bool IsAcknowledged(Project project)
        {
            lock (acknowledgedStatusByProject)
            {
                return acknowledgedStatusByProject.ContainsKey(project);
            }
        }
    }
}
