using _365Drive.Office365.CloudConnector;
using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;

namespace _365Drive.Office365.UI.Utility
{
    public static class CommunicationCallBacks
    {
        /// <summary>
        /// Callback method to be called along with communication event
        /// </summary>
        /// <param name="callback">Method to be called</param>
        public static void AskAuthentication()
        {
            //Make sure the auth form is NOT already open
            bool isWindowOpen = false;

            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is CredentialManager.UI.Authenticate)
                {
                    isWindowOpen = true;
                    w.Activate();
                }
            }

            if (!isWindowOpen)
            {
                CredentialManager.UI.Authenticate credForm = new CredentialManager.UI.Authenticate();
                ElementHost.EnableModelessKeyboardInterop(credForm);


                //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
                try
                {

                    credForm.Loaded += AboutForm_Loaded;
                    credForm.ShowDialog();
                }
                catch { }
            }
        }

        /// <summary>
        /// Callback method to be called along with communication event
        /// </summary>
        /// <param name="callback">Method to be called</param>
        public static void OpenWebClient()
        {
            _365Drive.Office365.UI.WebClientSupport.WebClientSupport wcSupport = new _365Drive.Office365.UI.WebClientSupport.WebClientSupport();
            ElementHost.EnableModelessKeyboardInterop(wcSupport);


            //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
            try
            {
                wcSupport.Loaded += AboutForm_Loaded;
                wcSupport.ShowDialog();
            }
            catch { }
        }

        /// <summary>
        /// Display about form
        /// </summary>
        public static void About()
        {
            About.About aboutForm = new About.About();
            ElementHost.EnableModelessKeyboardInterop(aboutForm);
            //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
            try
            {
                //aboutForm.Loca = FormStartPosition.CenterScreen
                aboutForm.Loaded += AboutForm_Loaded;
                bool? DialogResult = aboutForm.ShowDialog();
            }
            catch { }
        }

        private static void AboutForm_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Application curApp = System.Windows.Application.Current;
            Window mainWindow = curApp.MainWindow;
            ((ModernDialog)sender).Left = mainWindow.Left + (mainWindow.Width - ((ModernDialog)sender).ActualWidth) / 2;
            ((ModernDialog)sender).Top = mainWindow.Top + (mainWindow.Height - ((ModernDialog)sender).ActualHeight) / 2;
        }

        /// <summary>
        /// Display about form
        /// </summary>
        public static void Exit()
        {
            CredentialManager.UI.Exit exitForm = new CredentialManager.UI.Exit();
            ElementHost.EnableModelessKeyboardInterop(exitForm);
            //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
            try
            {
                exitForm.Loaded += AboutForm_Loaded;
                exitForm.ShowDialog();
            }
            catch { }
        }


        /// <summary>
        /// Clear local variable values about last drive settings fetched and last license checked
        /// </summary>
        public static void RefreshSettings()
        {
            try
            {
                LicenseManager.lastLicenseChecked = null;
                LicenseManager.lastDriveFetched = null;
                LicenseManager.lastLicenseState = null;
            }
            catch { }
        }
        /// <summary>
        /// Display about form
        /// </summary>
        public static void SignOut()
        {
            CredentialManager.UI.SignOut signOutForm = new CredentialManager.UI.SignOut();
            ElementHost.EnableModelessKeyboardInterop(signOutForm);
            //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
            try
            {
                signOutForm.Loaded += AboutForm_Loaded;
                signOutForm.ShowDialog();
            }
            catch { }
        }
    }
}
