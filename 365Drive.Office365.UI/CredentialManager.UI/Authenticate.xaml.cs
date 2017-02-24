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
    public partial class Authenticate : ModernDialog
    {
        public Authenticate()
        {
            InitializeComponent();

            // define the dialog buttons
            this.Buttons = new Button[] { this.OkButton, this.CancelButton };

            this.OkButton.Click += OkButton_Click;
            this.OkButton.IsCancel = true;
            this.OkButton.IsDefault = true;

            //set existing 
            setExistingCredentials();
        }


        /// <summary>
        /// Save the credentials to cred store
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateEmailandPassword(true))
            {
                LogManager.Verbose("Setting username password");
                _365Drive.Office365.CredentialManager.SetCredentials(userName.Text, password.Password);
                this.Close();
            }
        }

        /// <summary>
        /// Fetch current credentials 
        /// </summary>
        void setExistingCredentials()
        {
            Cred currentCreds = _365Drive.Office365.CredentialManager.GetCredential();
            if (currentCreds != null)
            {
                this.userName.Text = currentCreds.UserName;
                this.password.Password = currentCreds.Password;
            }
        }

        /// <summary>
        /// Save the credential to registry and restart the timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateEmailandPassword(true))
            {
                LogManager.Verbose("Setting username password");
                _365Drive.Office365.CredentialManager.SetCredentials(userName.Text, password.Password);
                this.Close();
            }
        }


        /// <summary>
        /// Email and password validation
        /// </summary>
        /// <returns>boolean indicating whether validation passed or failed</returns>
        bool ValidateEmailandPassword(bool bottonclick)
        {
            LogManager.Verbose("validating username and password");
            //email validation
            bool result = ValidatorExtensions.IsValidEmailAddress(userName.Text);

            //email validation failed
            if (!result)
            {
                validationSummary.Content = Globalization.Globalization.emailvalidation;
            }
            else
            {
                validationSummary.Content = string.Empty;
            }

            if (bottonclick)
            {
                //password validation
                if (string.IsNullOrEmpty(password.Password))
                {
                    result = false;
                    validationSummary.Content += Globalization.Globalization.passwordcannotbeblank;
                }
                else
                {
                    validationSummary.Content = string.Empty;
                }
            }
            LogManager.Verbose("credential validation result: " + result.ToString());
            return result;
        }

        /// <summary>
        /// Make sure its valid email
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void userName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEmailandPassword(false);
        }


        /// <summary>
        /// validate password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void password_TextInput(object sender, TextCompositionEventArgs e)
        {
            ValidateEmailandPassword(false);
        }
    }
}
