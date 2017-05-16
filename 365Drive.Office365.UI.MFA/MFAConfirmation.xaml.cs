using FirstFloor.ModernUI.Windows.Controls;
using System.Windows;
using System.Windows.Controls;

namespace _365Drive.Office365.UI.MFA
{
    /// <summary>
    /// Interaction logic for Credential.xaml
    /// </summary>
    public partial class MFAConfirmation : ModernDialog
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


        public bool proceed;
       

        public MFAConfirmation()
        {
            InitializeComponent();

            //set the usermessage
            

         
            // define the dialog buttons
            Button customOK = new Button();
            // define the dialog buttons
            this.Buttons = new Button[] { customOK, this.CancelButton };

            customOK.Click += OkButton_Click;
            customOK.Content = "Yes";
            customOK.IsCancel = false;
            customOK.IsDefault = true;

            this.CancelButton.IsCancel = true;
            this.OkButton.IsDefault = true;


            this.CancelButton.Content = "Later";
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
            proceed = false;
            this.Close();
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
