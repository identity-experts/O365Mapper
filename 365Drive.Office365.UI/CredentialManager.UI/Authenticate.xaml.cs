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
using System.Windows.Threading;

namespace _365Drive.Office365.UI.CredentialManager.UI
{
    /// <summary>
    /// Interaction logic for Credential.xaml
    /// </summary>
    public partial class Authenticate : ModernDialog
    {
        string currentUser;



        /// <summary>
        /// if its SSO, we dont need password column
        /// </summary>
        void EnableDisableSSO()
        {

            var bc = new BrushConverter();
            if (ckSSO.IsChecked == true)
            {
                password.IsEnabled = false;

                password.Background = (Brush)bc.ConvertFrom("#3e3e42");
                //password.BorderBrush = (Brush)bc.ConvertFrom("#3e3e42");
                lblPassword.Foreground = (Brush)bc.ConvertFrom("#3e3e42");
            }
            else
            {
                password.IsEnabled = true;
                password.Background = (Brush)bc.ConvertFrom("#808080");
                //password.BorderBrush = (Brush)bc.ConvertFrom("#3e3e42");
                lblPassword.Foreground = (Brush)bc.ConvertFrom("#c1c1c1");
            }
        }

        public Authenticate()
        {
            InitializeComponent();
            Button customOK = new Button();
            // define the dialog buttons
            this.Buttons = new Button[] { customOK, this.CancelButton };

            customOK.Click += OkButton_Click;
            this.OkButton.Click += OkButton_Click;

            customOK.Content = "Save";
            customOK.IsCancel = false;
            customOK.IsDefault = true;

            this.CancelButton.IsCancel = true;
            this.OkButton.IsDefault = true;

            //ShowPass.MouseDown += ShowPass_MouseDown;
            //ShowPass.PreviewMouseDown += ShowPass_MouseDown;
            //ShowPass.MouseUp += ShowPass_MouseUp;
            //ShowPass.PreviewMouseUp += ShowPass_MouseUp;


            // ShowPass.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(ShowPass_MouseDown), true);
            //AddHandler(FrameworkElement.MouseDownEvent, new MouseButtonEventHandler(ShowPass_MouseUp), true);

            //set existing 
            setExistingCredentials();

            //ckSSO.Checked += ckSSO_Checked;

            //Bring to front
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            if (LicenseManager.isitPartnerManaged)
            {
                //change logo
                logo.Source = LicenseManager.partnerLogoBM;
            }



            this.Activate();
        }


        private void ShowPass_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //throw new NotImplementedException();
            //password.Visibility = System.Windows.Visibility.Visible;
            //MyTextBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ShowPass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DispatcherTimer _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);

            _timer.Tick += new EventHandler(delegate (object s, EventArgs a)
            {
                password.Visibility = System.Windows.Visibility.Visible;
                MyTextBox.Visibility = System.Windows.Visibility.Collapsed;
                _timer.Stop();
            });

            //throw new NotImplementedException();
            password.Visibility = System.Windows.Visibility.Collapsed;
            MyTextBox.Visibility = System.Windows.Visibility.Visible;
            MyTextBox.Text = password.Password;
            MyTextBox.IsEnabled = false;
            MyTextBox.Focus();

            _timer.Start();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            password.Visibility = System.Windows.Visibility.Visible;
            MyTextBox.Visibility = System.Windows.Visibility.Collapsed;
            MyTextBox.Text = password.Password;
            password.Focus();
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
                SignInprogress.Visibility = Visibility.Visible;

                string newuser = (this.userName.Text + ";!" + this.password.Password).ToLower();

                if (currentUser != newuser)
                {
                    //To clear things, we fist need to signout existing user
                    SignOut.appSignOut();
                }

                LogManager.Verbose("Setting username password");
                _365Drive.Office365.CredentialManager.SetCredentials(userName.Text, password.Password);
                if (currentUser != newuser)
                {
                    //Reset SSO
                    if (ckSSO.IsChecked == true)
                    {
                        RegistryManager.Set(RegistryKeys.AutoSSO, "1");
                    }
                    else
                    {
                        RegistryManager.Set(RegistryKeys.AutoSSO, "0");
                    }

                    //set the password changed first time for the matter of MFA
                    _365Drive.Office365.CloudConnector.LicenseManager.hasPasswordChangedOrFirstTime = true;



                    //Notify the user that we will attempt the credentials shortly
                    CommunicationManager.Communications.queueNotification(Globalization.Globalization.SignInPageheader, Globalization.Globalization.CredentialsReceived);
                }

                //reset the SSO Counter to make a new beginning
                _365Drive.Office365.CredentialManager.ResetSSOCounter();

                SignInprogress.Visibility = Visibility.Hidden;
                this.Close();
            }
            else
            {
                //this.DialogResult = false;
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

            var creds = _365Drive.Office365.CredentialManager.GetCredential();

            if (creds != null && !String.IsNullOrEmpty(creds.UserName))
            {
                Task<bool> run = Task.Run(() => DriveManager.isAllowedSSOFedType(creds.UserName));
                run.Wait();

                if (run.Result)
                {
                    ckSSO.Visibility = Visibility.Visible;
                    helpIcon.Visibility = Visibility.Visible;
                    //tick / untick SSO checkbox
                    string autoSSO = RegistryManager.Get(RegistryKeys.AutoSSO);
                    if (!string.IsNullOrEmpty(autoSSO) && Convert.ToString(autoSSO) != "0")
                    {
                        ckSSO.IsChecked = true;
                    }
                    //enable / disable password as per SSO selection
                    EnableDisableSSO();

                }
                else
                {
                    ckSSO.Visibility = Visibility.Hidden;
                    helpIcon.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                ckSSO.Visibility = Visibility.Hidden;
                helpIcon.Visibility = Visibility.Hidden;
            }




            //set current username and password to local variable
            currentUser = (this.userName.Text + ";!" + this.password.Password).ToLower();
        }

        ///// <summary>
        ///// Save the credential to registry and restart the timer
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ValidateEmailandPassword(true))
        //    {
        //        string newuser = (this.userName.Text + ";!" + this.password.Password).ToLower(); 
        //        if(currentUser != newuser)
        //        {
        //            //set the password changed first time for the matter of MFA
        //            _365Drive.Office365.CloudConnector.LicenseManager.hasPasswordChangedOrFirstTime = true;

        //            //Notify the user that we will attempt the credentials shortly
        //            CommunicationManager.Communications.queueNotification(Globalization.Globalization.credentials, Globalization.Globalization.CredentialsReceived);
        //        }

        //        LogManager.Verbose("Setting username password");
        //        _365Drive.Office365.CredentialManager.SetCredentials(userName.Text, password.Password);
        //        this.Close();

        //        //restart the ticker

        //    }
        //}


        /// <summary>
        /// Email and password validation
        /// </summary>
        /// <returns>boolean indicating whether validation passed or failed</returns>
        bool ValidateEmailandPassword(bool bottonclick)
        {
            LogManager.Verbose("validating username and password");
            string autoSSO = RegistryManager.Get(RegistryKeys.AutoSSO);

            //email validation
            bool result = ValidatorExtensions.IsValidEmailAddress(userName.Text);
            if (string.IsNullOrEmpty(userName.Text))
                result = false;

            //email validation failed
            if (!result)
            {
                validationSummary.Text = Globalization.Globalization.emailvalidation;
            }
            else
            {
                validationSummary.Text = string.Empty;
            }

            if (bottonclick)
            {
                //incase of SSO, password will be blank so dont validate it.
                //if (!(string.IsNullOrEmpty(autoSSO) && Convert.ToString(autoSSO) != "0"))
                if (ckSSO.IsChecked != true)
                {
                    //password validation
                    if (string.IsNullOrEmpty(password.Password))
                    {
                        result = false;
                        validationSummary.Text += Environment.NewLine + Globalization.Globalization.passwordcannotbeblank;
                    }
                    else
                    {
                        //validationSummary.Content += string.Empty;
                    }
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
            //This looks irretating as the user is still typing username, hence no need to validate during that time.
            //ValidateEmailandPassword(false);
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


        /// <summary>
        /// If its enabled, we dont need password!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ckSSO_Checked(object sender, RoutedEventArgs e)
        {
            //enable / disable password as per SSO selection
            EnableDisableSSO();
        }
    }
}
