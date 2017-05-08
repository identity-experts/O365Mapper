using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Threading;

namespace _365Drive.Office365.NotificationManager
{
    public static class Animation
    {
        //notify icon
        public static TaskbarIcon notifyIcon;

        /// <summary>
        /// timer for Icon
        /// </summary>
        public static DispatcherTimer animatedIcontimer;
        static int animationImageCounter = 1;


        /// <summary>
        /// Animate as per the theme 
        /// </summary>
        /// <param name="theme"></param>
        public static void Animate(AnimationTheme theme)
        {
            switch (theme)
            {
                case (AnimationTheme.Inprogress):
                    {
                        animatedIcontimer.Tick += animateInProgress;
                        animatedIcontimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
                        animatedIcontimer.Start();
                        break;
                    }
                case (AnimationTheme.Warning):
                    {
                        animatedIcontimer.Tick += Waiting;
                        animatedIcontimer.Interval = new TimeSpan(0, 0, 0, 0, 700);
                        animatedIcontimer.Start();
                        break;
                    }
                case (AnimationTheme.Error):
                    {
                        notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.Hang;
                        break;
                    }
            }
        }

        private static void Waiting(object sender, EventArgs e)
        {
            if (notifyIcon != null)
            {
                if (animationImageCounter > 3)
                {
                    animationImageCounter = 0;
                }

                switch (animationImageCounter)
                {
                    //case (1):
                    //    {
                    //        notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources._365Drive;
                    //        break;
                    //    }
                    case (1):
                        {
                            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.Wait1;
                            break;
                        }
                    case (2):
                        {
                            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.Wait3;
                            break;
                        }
                    case (3):
                        {
                            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.Wait2;
                            break;
                        }
                }
                animationImageCounter++;
            }
        }

        /// <summary>
        /// stop the icon
        /// </summary>
        public static void Stop()
        {
            //Clear tickers
            var eventField = animatedIcontimer.GetType().GetField("Tick", BindingFlags.NonPublic | BindingFlags.Instance);
            var eventDelegate = (Delegate)eventField.GetValue(animatedIcontimer);
            if (eventDelegate != null)
            {
                var invocatationList = eventDelegate.GetInvocationList();

                foreach (var handler in invocatationList)
                    animatedIcontimer.Tick -= ((EventHandler)handler);

                //stop the timer
                animatedIcontimer.Stop();
            }

            ///reset the icon
            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources._365Drive;

        }

        public static void animateInProgress(object sender, EventArgs e)
        {
            if (notifyIcon != null)
            {
                if (animationImageCounter > 4)
                {
                    animationImageCounter = 0;
                }

                string imageName = Constants.animationIconNamePrefix + (animationImageCounter).ToString() + ".ico";

                switch (animationImageCounter)
                {
                    case (1):
                        {
                            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.IE_ProgressAnimation1;
                            break;
                        }
                    case (2):
                        {
                            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.IE_ProgressAnimation2;
                            break;
                        }
                    case (3):
                        {
                            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.IE_ProgressAnimation3;
                            break;
                        }
                    case (4):
                        {
                            notifyIcon.Icon = _365Drive.Office365.NotificationManager.Properties.Resources.IE_ProgressAnimation4;
                            break;
                        }
                }
                animationImageCounter++;
                //ImageSource source = BitmapFrame.Create(Path);
                //BitmapImage logo = new BitmapImage();
                //logo.BeginInit();
                //logo.UriSource = Path;
                //logo.EndInit();
                ////notifyIcon.Icon = toIcon(btm);
                //notifyIcon.IconSource = logo;
            }
        }

        //public static Icon toIcon(Bitmap b)
        //{
        //    Bitmap cb = (Bitmap)b.Clone();
        //    cb.MakeTransparent(Color.White);
        //    System.IntPtr p = cb.GetHicon();
        //    Icon ico = Icon.FromHandle(p);
        //    return ico;
        //}
    }
    public enum AnimationTheme
    {
        Inprogress, Error, Warning
    }
}