using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace _365Drive.Office365.NotificationManager
{
    public class NotificationManager
    {
        private TaskbarIcon notifyIcon;

        public NotificationManager(TaskbarIcon notifyIcon)
        {
            this.notifyIcon = notifyIcon;
        }

        /// <summary>
        /// Open a ballon tooltip icon
        /// </summary>
        /// <param name="strMessage">Message</param>
        public void notify(string tipTitle, string tipMessage, ToolTipIcon tipIcon)
        {
            //this.notifyIcon.ShowBalloonTip(200, tipTitle, tipMessage, ToolTipIcon.Warning);
            FancyBalloon balloon = new FancyBalloon();
            balloon.BalloonText = tipTitle;
            balloon.BalloonMessage = tipMessage;
            //show balloon and close it after 4 seconds
            notifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 4000);
        }

        /// <summary>
        /// Open a ballon tooltip icon
        /// </summary>
        /// <param name="strMessage">Message</param>
        public void notify(string tipTitle, string tipMessage, ToolTipIcon tipIcon, Action callback)
        {
            FancyBalloon balloon = new FancyBalloon();
            balloon.BalloonText = tipTitle;
            balloon.BalloonMessage = tipMessage;
            balloon.Callback = callback;
            //show balloon and close it after 4 seconds
            notifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 4000);
        }
    }
}
