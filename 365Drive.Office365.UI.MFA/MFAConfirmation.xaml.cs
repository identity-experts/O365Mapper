using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace _365Drive.Office365.UI.MFA
{
    /// <summary>
    /// Interaction logic for Credential.xaml
    /// </summary>
    public partial class MFAConfirmation : ModernDialog
    {

        private RemindLater? _remindLater;


        public BitmapImage partnerLogo
        {
            set
            {
                try
                {
                    this.logo.Source = value;
                }
                catch { }
            }
        }

        private string _sAuthContext_AuthMethodId;
        public string sAuthContext_AuthMethodId
        {
            get
            {
                return _sAuthContext_AuthMethodId;
            }
            set
            {
                _sAuthContext_AuthMethodId = value;
            }
        }

        public RemindLater? RemindLater
        {
            get
            {
                return _remindLater;
            }

            set
            {
                _remindLater = value;
            }
        }

        public bool proceed;


        public MFAConfirmation()
        {
            InitializeComponent();

            //set the usermessage

            //if the user has already asked to remind later, no need to ask again
            if (!ReminderStates.mfaConfirmationTimeNow)
            {
                this.RemindLater = ((RemindLater)(ReminderStates.lastRemiderState));
                proceed = false;
                this.Close();
                return;
            }


            // define the dialog buttons
            Button customOK = new Button();
            Button customClose = new Button();
            // define the dialog buttons
            this.Buttons = new Button[] { customOK, customClose };

            customOK.Click += OkButton_Click;
            customOK.Content = "Yes";
            customOK.IsCancel = false;
            customOK.IsDefault = true;

            customClose.IsCancel = false;
            //this.OkButton.IsDefault = true;


            customClose.Content = "Later";
            customClose.Click += CancelButton_Click;
            //this.CancelButton.IsCancel = true;
            //this.CancelButton.IsDefault = true;

            this.Closing += MFAConfirmation_Closing;
        }


        /// <summary>
        /// incase user tried to close directly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MFAConfirmation_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (proceed != true)
            {
                if (this._remindLater == null)
                {
                    this._remindLater = UI.MFA.RemindLater.oneHour;
                    proceed = false;
                }
            }
        }


        /// <summary>
        /// if the user prefer to remind later, below should happen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void remindLater_click(object sender, RoutedEventArgs e)
        {
            string buttonName = (sender as Button).Name.ToString();

            ///get value in local variable
            switch (buttonName)
            {
                case "remindIn1Hour":
                    this._remindLater = _365Drive.Office365.UI.MFA.RemindLater.oneHour;
                    ReminderStates.lastRemiderState = _365Drive.Office365.UI.MFA.RemindLater.oneHour;
                    ReminderStates.lastAsked = DateTime.Now;
                    break;
                case "remindIn2Hour":
                    this._remindLater = _365Drive.Office365.UI.MFA.RemindLater.twoHour;
                    ReminderStates.lastRemiderState = _365Drive.Office365.UI.MFA.RemindLater.twoHour;
                    ReminderStates.lastAsked = DateTime.Now;
                    break;
                case "remindIn5Hour":
                    this._remindLater = _365Drive.Office365.UI.MFA.RemindLater.fiveHour;
                    ReminderStates.lastRemiderState = _365Drive.Office365.UI.MFA.RemindLater.fiveHour;
                    ReminderStates.lastAsked = DateTime.Now;
                    break;
                case "remindIn24Hour":
                    this._remindLater = _365Drive.Office365.UI.MFA.RemindLater.twentyFourHour;
                    ReminderStates.lastRemiderState = _365Drive.Office365.UI.MFA.RemindLater.twentyFourHour;
                    ReminderStates.lastAsked = DateTime.Now;
                    break;
                default:
                    this._remindLater = _365Drive.Office365.UI.MFA.RemindLater.oneHour;
                    ReminderStates.lastRemiderState = _365Drive.Office365.UI.MFA.RemindLater.oneHour;
                    ReminderStates.lastAsked = DateTime.Now;
                    break;
            }


            //proceed and close
            proceed = false;
            this.Close();
        }

        /// <summary>
        /// No action, close current form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //remind later is clicked!
            LaterTime.Visibility = Visibility.Visible;
            Confirmation.Visibility = Visibility.Hidden;

            //hide buttons
            var buttons = this.Buttons;
            foreach(Button btn in buttons)
            {
                btn.Visibility = Visibility.Hidden;
            }

            //proceed = false;
            //this.Close();
        }


        /// <summary>
        /// Save the credentials to cred store
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            proceed = true;
            this.Close();
            //unmap all drives
        }



    }
}
