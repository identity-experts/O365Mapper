using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            CredentialManager.UI.Authenticate credForm = new CredentialManager.UI.Authenticate();
            ElementHost.EnableModelessKeyboardInterop(credForm);


            //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
            try
            {
                credForm.ShowDialog();
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
                bool? DialogResult = aboutForm.ShowDialog();
            }
            catch { }
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
                exitForm.ShowDialog();
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
                signOutForm.ShowDialog();
            }
            catch { }
        }
    }
}
