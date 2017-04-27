using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Common.Logging;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;
using JenkinsTrayTracker.UI;
using JenkinsTrayTracker.Utils;
using JenkinsTrayTracker.Utils.Logging;
using Spring.Context.Support;

namespace JenkinsTrayTracker
{
    static class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.ThreadException += new ThreadExceptionEventHandler(ThreadExceptionHandler.Application_ThreadException);

                // skinning         
                SkinManager.EnableFormSkins();
                OfficeSkins.Register();
                UserLookAndFeel.Default.ActiveLookAndFeel.SkinName = "Office 2010 Silver";

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ApplicationExit += new EventHandler(Application_Exit);
                Application_Prepare();

                // Spring
                ContextRegistry.GetContext();
                TrayNotifier.Instance.UpdateNotifier();

                ApplicationContext appContext = new ApplicationContext();
                Application.Run(appContext);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(logger, ex);
                MessageBox.Show(ex.ToString(), "Program exception handler");
            }
        }

        private static void Application_Prepare()
        {
            logger.Info("Log4net ready.");
            logger.Info(Assembly.GetExecutingAssembly().GetName().Name
                + " v" + Assembly.GetExecutingAssembly().GetName().Version);
            logger.Info(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location));
        }

        static void Application_Exit(object sender, EventArgs e)
        {
            try
            {
                TrayNotifier.Instance.Dispose();
            }
            catch (Exception ex)
            {
                logger.Error("Failed disposing tray notifier", ex);
            }

            logger.Info(Assembly.GetExecutingAssembly().GetName().Name
                + " v" + Assembly.GetExecutingAssembly().GetName().Version + " Exit");
        }
    }
}
