using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Reflection;
using _365Drive.Office365;
using _365Drive.Office365.NotificationManager;

namespace _365Drive.Office365.Launcher
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
        private readonly Core coreService;

        /// <summary>
		/// This class should be created and passed into Application.Run( ... )
		/// </summary>
		public CustomApplicationContext() 
		{
			InitializeContext();
            coreService = new Core(notifyIcon);
            coreService.Initialize();

            //if (!/*hostManager*/.IsDecorated) { ShowIntroForm(); }
		}

        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            coreService.Initialize();

            ///Add all CORE menu options by below method
            coreService.BuildContextMenu(notifyIcon.ContextMenuStrip);
            notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            //Whatever is reminder, like Exit, about and other stuffs will be added here
            notifyIcon.ContextMenuStrip.Items.Add(coreService.ToolStripMenuItemWithHandler("&Help/About", showHelpItem_Click));
            notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            notifyIcon.ContextMenuStrip.Items.Add(coreService.ToolStripMenuItemWithHandler("&Exit", exitItem_Click));
        }

        # region the child forms

        //private DetailsForm detailsForm;
        private System.Windows.Window introForm;

        private void ShowIntroForm()
        {
            if (introForm == null)
            {
                introForm = new About();
                introForm.Closed += mainForm_Closed; // avoid reshowing a disposed form
                ElementHost.EnableModelessKeyboardInterop(introForm);
                introForm.Show();
            }
            else { introForm.Activate(); }
        }

        
        private void notifyIcon_DoubleClick(object sender, EventArgs e) { ShowIntroForm();    }

        // From http://stackoverflow.com/questions/2208690/invoke-notifyicons-context-menu
        private void notifyIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon, null);
            }
        }


        // attach to context menu items
        private void showHelpItem_Click(object sender, EventArgs e)     { ShowIntroForm();    }
        //private void showDetailsItem_Click(object sender, EventArgs e)  { /*ShowDetailsForm*/();  }

        // null out the forms so we know to create a new one.
        //private void detailsForm_Closed(object sender, EventArgs e)     { detailsForm = null; }
        private void mainForm_Closed(object sender, EventArgs e)        { introForm = null;   }

        # endregion the child forms

        # region generic code framework

        private System.ComponentModel.IContainer components;	// a list of components to dispose when the context is disposed
        private NotifyIcon notifyIcon;				            // the icon that sits in the system tray

        private void InitializeContext()
        {
            NotificationManager.App app = new NotificationManager.App();
            app.Run();
            //components = new System.ComponentModel.Container();
            //notifyIcon = new NotifyIcon(components)
            //                 {
            //                     ContextMenuStrip = new ContextMenuStrip(),
            //                     Icon = new Icon(IconFileName),
            //                     Text = DefaultTooltip,
            //                     Visible = true
            //                 };
            //notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
            //notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            //notifyIcon.MouseUp += notifyIcon_MouseUp;
        }

        /// <summary>
		/// When the application context is disposed, dispose things like the notify icon.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && components != null) { components.Dispose(); }
		}

		/// <summary>
		/// When the exit menu item is clicked, make a call to terminate the ApplicationContext.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void exitItem_Click(object sender, EventArgs e) 
		{
			ExitThread();
		}

        /// <summary>
        /// If we are presently showing a form, clean it up.
        /// </summary>
        protected override void ExitThreadCore()
        {
            // before we exit, let forms clean themselves up.
            if (introForm != null) { introForm.Close(); }
            //if (detailsForm != null) { detailsForm.Close(); }

            notifyIcon.Visible = false; // should remove lingering tray icon
            base.ExitThreadCore();
        }

        # endregion generic code framework

    }
}
