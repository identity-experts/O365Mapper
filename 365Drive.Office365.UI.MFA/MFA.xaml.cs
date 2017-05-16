﻿using FirstFloor.ModernUI.Windows.Controls;
using System.Windows;
using System.Windows.Controls;

namespace _365Drive.Office365.UI.MFA
{
    /// <summary>
    /// Interaction logic for Credential.xaml
    /// </summary>
    public partial class MFA : ModernDialog
    {


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


        private string _smsCode;
        public string smsCode
        {
            get
            {
                return _smsCode;
            }
            set
            {
                _smsCode = value;
            }
        }

        private bool _verify;
        public bool verify
        {
            get
            {
                return _verify;
            }
            set
            {
                _verify = value;
            }
        }


        public MFA()
        {
            InitializeComponent();

            //set the usermessage
            

         
            // define the dialog buttons
            Button customOK = new Button();
            // define the dialog buttons
            this.Buttons = new Button[] { customOK, this.CancelButton };

            customOK.Click += OkButton_Click;
            customOK.Content = "Ok";
            customOK.IsCancel = false;
            customOK.IsDefault = true;

            this.CancelButton.IsCancel = true;
            this.OkButton.IsDefault = true;


            this.CancelButton.Content = "Cancel";
            this.CancelButton.Click += CancelButton_Click; ;

            //Verify now click
            VerifyNow.Click += VerifyNow_Click;
            VerifyNowPA.Click += VerifyNow_Click;
        }

        private void VerifyNow_Click(object sender, RoutedEventArgs e)
        {
            verify = true;
            this.Close();
        }

        public void ShowMFA()
        {

            if (sAuthContext_AuthMethodId.ToLower() == "onewaysms")
            {
                SASAuth.Visibility = Visibility.Visible;
            }
            else if (sAuthContext_AuthMethodId.ToLower() == "twowayvoicemobile" || sAuthContext_AuthMethodId.ToLower() == "twowayvoiceoffice")
            {
                SASAuthCall.Visibility = Visibility.Visible;
            }
            else if (sAuthContext_AuthMethodId.ToLower() == "phoneappnotification")
            {
                SASPhoneAppNotification.Visibility = Visibility.Visible;
            }
            else if (sAuthContext_AuthMethodId.ToLower() == "phoneappotp")
            {
                SASPhoneAppOTP.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// No action, close current form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            verify = false;
            this.Close();
        }


        /// <summary>
        /// Save the credentials to cred store
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {

            if (sAuthContext_AuthMethodId.ToLower() == "onewaysms")
            {
                if (!string.IsNullOrEmpty(this.SMSCode.Text))
                {
                    smsCode = this.SMSCode.Text;
                    this.Close();
                }
                else
                {
                    validationSummary.Text = "Please type SMS code";
                }
            }
            else if (sAuthContext_AuthMethodId.ToLower() == "phoneappotp")
            {
                if (!string.IsNullOrEmpty(this.SASPAOTP.Text))
                {
                    smsCode = this.PAOTP.Text;
                    this.Close();
                }
                else
                {
                    PAvalidationSummary.Text = "Please type SMS code";
                }
            }
            else if (sAuthContext_AuthMethodId.ToLower() == "twowayvoicemobile" || sAuthContext_AuthMethodId.ToLower() == "phoneappnotification")
            {
                verify = true;
                this.Close();
            }
      
            //unmap all drives
        }



    }
}
