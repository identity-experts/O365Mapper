using System.Windows.Forms;

namespace _365Drive.Office365
{
    public class Notifications
    {

        private readonly NotifyIcon notifyIcon;

        public Notifications()
        {

        }

        /// <summary>
        /// Set the default hover text of tooltip manager
        /// </summary>
        public void setDefaults()
        {
            notifyIcon.Text = "365Drive";
        }

        /// <summary>
        /// Intialize the notification manager
        /// </summary>
        /// <param name="notifyIcon">the global notification master</param>
        public Notifications(NotifyIcon notifyIcon)
        {
            this.notifyIcon = notifyIcon;
        }


        /// <summary>
        /// Open a ballon tooltip icon
        /// </summary>
        /// <param name="strMessage">Message</param>
        public void notify(string tipTitle, string tipMessage, ToolTipIcon tipIcon)
        {

            this.notifyIcon.ShowBalloonTip(200, tipTitle, tipMessage, ToolTipIcon.Warning);
        }

    }
}
