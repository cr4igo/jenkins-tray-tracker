using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using JenkinsTrayTracker.BusinessComponents;
using JenkinsTrayTracker.Entities;
using JenkinsTrayTracker.Utils.BackgroundProcessing;
using DevExpress.XtraBars;
using Spring.Context.Support;

namespace JenkinsTrayTracker.UI
{
    public partial class SettingsForm : DevExpress.XtraEditors.XtraForm
    {
        public static SettingsForm Instance
        {
            get
            {
                SettingsForm instance = (SettingsForm)ContextRegistry.GetContext().GetObject("SettingsForm");
                return instance;
            }
        }

        public ConfigurationService ConfigurationService { get; set; }
        public HudsonService HudsonService { get; set; }

        public SettingsForm()
        {
            InitializeComponent();
        }

        public static void ShowDialogOrFocus()
        {
            if (Instance.Visible)
                Instance.Focus();
            else
                Instance.ShowDialog();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            tabControl.SelectedTabPageIndex = 0;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            refreshSpinEdit.Value = ConfigurationService.GeneralSettings.RefreshIntervalInSeconds;
            updateMainWindowIconCheckEdit.Checked = ConfigurationService.GeneralSettings.UpdateMainWindowIcon;
            integrateWithClaimPluginCheckEdit.Checked = ConfigurationService.GeneralSettings.IntegrateWithClaimPlugin;
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            int refreshInterval = (int)refreshSpinEdit.Value;
            ConfigurationService.SetRefreshIntervalInSeconds(refreshInterval);
            ConfigurationService.SetUpdateMainWindowIcon(updateMainWindowIconCheckEdit.Checked);
            ConfigurationService.SetIntegrateWithClaimPlugin(integrateWithClaimPluginCheckEdit.Checked);
        }
    }
}