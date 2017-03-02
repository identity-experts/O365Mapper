using _365Drive.Office365.MutexManager;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace _365Drive.Office365.NotificationManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            if (!SingleInstance.Start())
            {      //exit
                Application.Current.Shutdown(); ;
                return;
            }
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                var applicationContext = new CustomApplicationContext();
                applicationContext.InitContext(notifyIcon);
                System.Windows.Forms.Application.Run(applicationContext);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex.Message);
            }
            SingleInstance.Stop();

            AppDomain.CurrentDomain.UnhandledException += LogManager.CurrentDomainOnUnhandledException;
        }


        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }
    }
}
