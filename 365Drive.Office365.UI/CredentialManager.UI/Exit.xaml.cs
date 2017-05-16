using _365Drive.Office365.CloudConnector;
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
    public partial class Exit : ModernDialog
    {
        public Exit()
        {
            InitializeComponent();

            // define the dialog buttons
            this.Buttons = new Button[] { this.OkButton, this.CancelButton };

            this.OkButton.Content = Globalization.Globalization.Exit;
            this.OkButton.Click += OkButton_Click;
            //this.OkButton.IsCancel = false;
            //this.OkButton.IsDefault = false;

            this.CancelButton.Content = Globalization.Globalization.Cancel;
            this.CancelButton.Click += CancelButton_Click;
            //this.CancelButton.IsCancel = true;
            //this.CancelButton.IsDefault = true;
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
            //unmap all drives
            DriveManager.unmapAllDrives();

            //Clear all cookies
            DriveManager.clearCookies();

            //exit
            Application.Current.Shutdown();
        }

      

    }
}
