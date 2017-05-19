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
        public static List<Notification> notificationQueue;

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
        /// Take the notification request
        /// </summary>
        /// <param name="heading"></param>
        /// <param name="message"></param>
        public static void queueNotification(string heading, string message)
        {
            try
            {
                if (!Utility.ready())
                    return;

                Notification newNotification = new Notification();
                newNotification.Heading = heading;
                newNotification.Message = message;
                newNotification.Notified = false;

                if (notificationQueue == null)
                    notificationQueue = new List<Notification>();

                //add to queue
                notificationQueue.Add(newNotification);

            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// fetch the next notification queue
        /// </summary>
        /// <returns></returns>
        public static Notification getNextNotificationQueueItem()
        {
            try
            {
                if (!Utility.ready())
                    return null;


                if (notificationQueue != null && notificationQueue.Count(n => !n.Notified) > 0)
                {
                    //first retrieve the next item
                    Notification nextNotification = notificationQueue.FirstOrDefault(n => !n.Notified);

                    //remove from queue
                    notificationQueue.Remove(nextNotification);

                    //return 
                    return nextNotification;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return null;
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
