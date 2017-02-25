using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.CommunicationManager
{
    public static class Communications
    {
        public static TaskbarIcon notifyIcon;
        /// <summary>
        /// Update the notify icon status
        /// </summary>
        public static void updateStatus(string message)
        {
            try
            {
                if (!Utility.ready())
                    return;

                if (notifyIcon != null)
                    notifyIcon.ToolTipText = message;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }


        /// <summary>
        /// Get / set current state
        /// </summary>
        public static States CurrentState
        {
            get; set;
        }
    }

    public enum States
    {
        Running = 0,
        Hold = 1,
        Stopped = 2,
        UserAction = 3
    }
}
