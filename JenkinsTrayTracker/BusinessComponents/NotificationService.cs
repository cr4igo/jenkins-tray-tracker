using System;
using System.ComponentModel;
using JenkinsTrayTracker.Entities;

namespace JenkinsTrayTracker.BusinessComponents
{
    public class NotificationService : Component
    {
        private AllServersStatus allServersStatus = new AllServersStatus();

        public ConfigurationService ConfigurationService { get; set; }
        public ProjectsUpdateService UpdateService { private get; set; }

        public void Initialize()
        {
            ConfigurationService.ConfigurationUpdated += Execute;
            UpdateService.ProjectsUpdated += Execute;
            Disposed += delegate
            {
                ConfigurationService.ConfigurationUpdated -= Execute;
                UpdateService.ProjectsUpdated -= Execute;
            };

            //Execute();
        }

        public void Execute()
        {
            allServersStatus.Update(ConfigurationService.Servers);

            string fileToPlay = null;
            if (allServersStatus.StillFailingProjects.Count > 0)
                fileToPlay = ConfigurationService.NotificationSettings.StillFailingSoundPath;
            else if (allServersStatus.FailingProjects.Count > 0)
                fileToPlay = ConfigurationService.NotificationSettings.FailedSoundPath;
            else if (allServersStatus.FixedProjects.Count > 0)
                fileToPlay = ConfigurationService.NotificationSettings.FixedSoundPath;
            else if (allServersStatus.SucceedingProjects.Count > 0)
                fileToPlay = ConfigurationService.NotificationSettings.SucceededSoundPath;
            if (fileToPlay != null)
                SoundPlayer.PlayFile(fileToPlay);
        }

        private bool TreatAsFailure(BuildStatusEnum status)
        {
            return status == BuildStatusEnum.Failed
                || (status == BuildStatusEnum.Unstable && ConfigurationService.NotificationSettings.TreatUnstableAsFailed);
        }
    }
}