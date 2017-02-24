using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Reflection;
using _365Drive.Office365;
using _365Drive.Office365.NotificationManager;
using Hardcodet.Wpf.TaskbarNotification;
using System.Threading.Tasks;

namespace _365Drive.Office365.NotificationManager
{

    /// <summary>
    /// Framework for running application as a tray app.
    /// </summary>
    /// <remarks>
    /// Tray app code adapted from "Creating Applications with NotifyIcon in Windows Forms", Jessica Fosler,
    /// http://windowsclient.net/articles/notifyiconapplications.aspx
    /// </remarks>
    public class CustomApplicationContext : ApplicationContext
    {
        // Icon graphic from http://prothemedesign.com/circular-icons/
        private static readonly string IconFileName = "365Mapper.ico";
        private static readonly string DefaultTooltip = "Map office 365 drives as windows drive";
      //  private readonly Core coreService;

        /// <summary>
		/// This class should be created and passed into Application.Run( ... )
		/// </summary>
		public async void InitContext(TaskbarIcon notifyIcon) 
		{
            Core coreService = new Core(notifyIcon);
            coreService.Initialize();    
		}

      

      
    }
}
