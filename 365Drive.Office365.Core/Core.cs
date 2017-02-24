using System;
using System.Windows.Forms;

namespace _365Drive.Office365
{
    public class Core
    {
        /// <summary>
        /// Global settings / variables
        /// </summary>
        private readonly NotifyIcon notifyIcon;
        Notifications NotificationManager;

        /// <summary>
        /// Constructor
        /// </summary>5
        /// <param name="notifyIcon">Instace of global notify being used</param>
        public Core(NotifyIcon notifyIcon)
        {
            this.notifyIcon = notifyIcon;
            this.NotificationManager = new Notifications(notifyIcon);
        }


        /// <summary>
        /// Intialize 365Drive, which means make sure the logger, registry, credentials and all other required parameters are upto the mark.
        /// </summary>
        public void Initialize()
        {
            //Set the dev environment settings (if its dev!)
            RegistryManager.SetDevEnvironmnet();

            //Mare sure credential is present
            if (CredentialManager.ensureCredentials() == CredentialState.Notpresent)
            {
                NotificationManager.notify("Credentials", "Please provide credentials to proceed", ToolTipIcon.Warning);
            }

            //Set the default text
            NotificationManager.setDefaults();

            //Initialize notification manager

            //Make sure the exe is registered as startup
            RegistryManager.RegisterExeOnStartup();

            //Initialize logging
            LogManager.Init();

        }

        public bool IsDecorated { get; private set; }


        public void BuildContextMenu(ContextMenuStrip contextMenuStrip)
        {
            contextMenuStrip.Items.Clear();
            //contextMenuStrip.Items.AddRange(
            //    projectDict.Keys.OrderBy(project => project).Select(project => BuildSubMenu(project)).ToArray());
            contextMenuStrip.Items.AddRange(
                new ToolStripItem[] {
                   new ToolStripSeparator(),
                    ToolStripMenuItemWithHandler("&Signin", signIn_Click),
                    ToolStripMenuItemWithHandler("$Signout", signOut_Click)
                });
        }

        private void signIn_Click(object sender, EventArgs e)
        {

        }
        private void signOut_Click(object sender, EventArgs e)
        {

        }
        public ToolStripMenuItem ToolStripMenuItemWithHandler(string displayText, EventHandler eventHandler)
        {
            return ToolStripMenuItemWithHandler(displayText, 0, 0, eventHandler);
        }

        private ToolStripMenuItem ToolStripMenuItemWithHandler(
           string displayText, int enabledCount, int disabledCount, EventHandler eventHandler)
        {
            var item = new ToolStripMenuItem(displayText);
            if (eventHandler != null) { item.Click += eventHandler; }

            //item.Image = (enabledCount > 0 && disabledCount > 0) ? Properties.Resources.signal_yellow
            //             : (enabledCount > 0) ? Properties.Resources.signal_green
            //             : (disabledCount > 0) ? Properties.Resources.signal_red
            //             : null;
            item.ToolTipText = (enabledCount > 0 && disabledCount > 0) ?
                                                 string.Format("{0} enabled, {1} disabled", enabledCount, disabledCount)
                         : (enabledCount > 0) ? string.Format("{0} enabled", enabledCount)
                         : (disabledCount > 0) ? string.Format("{0} disabled", disabledCount)
                         : "";
            return item;
        }

    }
}
