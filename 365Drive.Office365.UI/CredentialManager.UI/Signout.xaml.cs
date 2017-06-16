using _365Drive.Office365.CloudConnector;
using _365Drive.Office365.GetTenancyURL.CookieManager;
using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _365Drive.Office365.UI.CredentialManager.UI
{
    /// <summary>
    /// Interaction logic for Credential.xaml
    /// </summary>
    public partial class SignOut : ModernDialog
    {
        public SignOut()
        {
            InitializeComponent();

            // define the dialog buttons
            this.Buttons = new Button[] { this.OkButton, this.CancelButton };

            this.OkButton.Content = Globalization.Globalization.SignOut;
            this.OkButton.Click += OkButton_Click;

            this.CancelButton.Content = Globalization.Globalization.Cancel;
            this.CancelButton.Click += CancelButton_Click;

            if (LicenseManager.isitPartnerManaged)
            {
                //change logo
                logo.Source = LicenseManager.partnerLogoBM;
            }

            //var menu = (ContextMenu)Resources["Vegetables"];
        }


        /// <summary>
        /// No action, close current form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Save the credentials to cred store
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SignInprogress.Visibility = Visibility.Visible;
            //signout from application
            appSignOut();
            SignInprogress.Visibility = Visibility.Hidden;

            this.Close();
        }


        public static void appSignOut()
        {
            //unmap all drives
            DriveManager.unmapAllDrives();

            //Clear all cookies
            DriveManager.clearCookies();

            //reset the fedType
            DriveManager.FederationType = null;

            //remove all drive from array
            DriveManager.clearAll();

            //clear partner details
            LicenseManager.partnerName = string.Empty;
            LicenseManager.partnerLogo = string.Empty;
            LicenseManager.partnerAbout = string.Empty;


            //Delete credentials
            _365Drive.Office365.CredentialManager.RemoveCredentials();

            //Delete registrries
            RegistryManager.DeleteAllRegistry();

            //clear cookie cache
            AADConnectCookieManager.signout();
            o365cookieManager.signout();
            ADFSAuth.signout();

            //clear MFA cache
            _365Drive.Office365.UI.MFA.ReminderStates.lastRemiderState = null;

            //clear license states
            LicenseManager.lastLicenseChecked = null;
            LicenseManager.lastDriveFetched = null;
            LicenseManager.lastLicenseState = null;

            ////exit
            //Application.Current.Shutdown();
        }


    }
}
