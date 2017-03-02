using _365Drive.Office365.UI.Globalization;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace _365Drive.Office365.NotificationManager
{
    public static class NotificationManager
    {
        public static TaskbarIcon notifyIcon;

        //public NotificationManager(TaskbarIcon notifyIcon)
        //{
        //    this.notifyIcon = notifyIcon;
        //}

        public static List<string> AlreadyNotified
        {
            get; set;
        }

        /// <summary>
        /// Open a ballon tooltip icon
        /// </summary>
        /// <param name="strMessage">Message</param>
        public static void notify(string tipTitle, string tipMessage, ToolTipIcon tipIcon)
        {
            try
            {
                if (AlreadyNotified == null)
                    AlreadyNotified = new List<string>();

                if (!AlreadyNotified.Contains(tipMessage))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        //this.notifyIcon.ShowBalloonTip(200, tipTitle, tipMessage, ToolTipIcon.Warning);
                        FancyBalloon balloon = new FancyBalloon();
                        balloon.BalloonText = tipTitle;
                        balloon.BalloonMessage = tipMessage;
                        //show balloon and close it after 4 seconds
                        notifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 4000);

                        //Add to already notified
                        AlreadyNotified.Add(tipMessage);
                    });
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// Open a ballon tooltip icon
        /// </summary>
        /// <param name="strMessage">Message</param>
        public static void notify(string tipTitle, string tipMessage, ToolTipIcon tipIcon, Action callback)
        {
            try
            {
                if (AlreadyNotified == null)
                    AlreadyNotified = new List<string>();

                if (!AlreadyNotified.Contains(tipMessage))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {

                        FancyBalloon balloon = new FancyBalloon();
                        balloon.BalloonText = tipTitle;
                        balloon.BalloonMessage = tipMessage;
                        balloon.Callback = callback;
                        //show balloon and close it after 4 seconds
                        notifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 4000);

                        //add it to already notified array to avoil re-notifications and irretations
                        AlreadyNotified.Add(tipMessage);
                    });

                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }





    }
}
