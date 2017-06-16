using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _365Drive.Office365.NotificationManager.Commands
{
    public class ContextMenuHelper
    {
        /// <summary>
        /// If user has opted for MFA prompt later, below method will add a menu option to let user to clear that cache setting and prompt mfa now
        /// </summary>
        public void AddMFAMenu()
        {
             Application.Current.FindResource("YOUR-RESOURCE-KEY");
        }
    }
}
