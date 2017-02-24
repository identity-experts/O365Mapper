using System;
using System.Windows.Forms;
using Hardcodet.Wpf.TaskbarNotification;
using _365Drive.Office365.UI.Globalization;
using _365Drive.Office365.UI.Utility;
using _365Drive.Office365.CommunicationManager;
using _365Drive.Office365.CloudConnector;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace _365Drive.Office365
{
    public class Core
    {
        /// <summary>
        /// Global settings / variables
        /// </summary>
        private readonly TaskbarIcon notifyIcon;

        NotificationManager.NotificationManager NotificationManager;
        ///We need to initialize a timer here which will make sure the sync happens every minute
        DispatcherTimer dispatcherTimer;
        //Declare current dispacher
        Dispatcher currentDispatcher;

        public Core()
        {
            //this.NotificationManager = new Notifications(notifyIcon);
        }

        /// <summary>
        /// Constructor
        /// </summary>5
        /// <param name="notifyIcon">Instace of global notify being used</param>
        public Core(TaskbarIcon notifyIcon)
        {
            LogManager.Verbose("Initializing Notifications, Timer and other elements from core constructor");
            this.notifyIcon = notifyIcon;
            this.NotificationManager = new NotificationManager.NotificationManager(notifyIcon);

            //Initialize the timer
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            currentDispatcher = Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// Initiate the sync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //Code goes here
            await Task.Run(() => tick());
        }

        /// <summary>
        /// Intialize 365Drive, which means make sure the logger, registry, credentials and all other required parameters are upto the mark.
        /// </summary>
        public async void Initialize()
        {

            Communications.notifyIcon = notifyIcon;
            Communications.CurrentState = States.Running;

            //currentDispatcher.Invoke(() =>
            //{
            Communications.updateStatus(Globalization.Initializing);
            //});

            //Verbose
            LogManager.Verbose("Initialization started");

            //Initialize logging
            LogManager.Verbose("Initialize log manager");
            LogManager.Init();

            //Set the dev environment settings (if its dev!)
            LogManager.Verbose("Set the dev environment settings (if its dev!)");
            if (RegistryManager.IsDev)
            {
                LogManager.Verbose("Its a dev environment. Setting dev friendly registry");
                RegistryManager.SetDevEnvironmnet();
            }

            Communications.updateStatus(Globalization.EnsuringCredential);
            LogManager.Verbose("Do we have credentials?");
            //Mare sure credential is present
            if (CredentialManager.ensureCredentials() == CredentialState.Notpresent)
            {
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.CredentialNotPresent);
                });
                LogManager.Verbose("credentials not present");
                NotificationManager.notify(Globalization.credentials, Globalization.NocredMessage, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                Communications.CurrentState = States.UserAction;
                return;
            }
            else
            {
                LogManager.Verbose("Credential present");
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.CredentialsPresent);
                });
            }

            //Make sure the exe is registered as startup
            LogManager.Verbose("Make sure the exe is registered as startup");
            RegistryManager.RegisterExeOnStartup();


            //make sure we have valid license
            LicenseValidationState licenseValidationState = DriveMapper.EnsureLicense(CredentialManager.GetCredential().UserName, CredentialManager.GetCredential().Password);
            if (licenseValidationState == LicenseValidationState.LoginFailed)
            {
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.LoginFailed);
                });
                LogManager.Verbose("credentials not valid or app isnt registered");
                NotificationManager.notify(Globalization.credentials, Globalization.LoginFailed, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
            }
            {
                LogManager.Verbose("Credential present");
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.LicenseFoundOK);
                });
            }

            LogManager.Verbose("setting timer");

            //call tick first time
            currentDispatcher.Invoke(() =>
            {
                //Set the value received from registry
                if (RegistryManager.IsDev)
                    dispatcherTimer.Interval = new TimeSpan(0, 2, 1);
                else
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 15);

                dispatcherTimer.Start();
            });
            //call it now
            await Task.Run(() => tick());
        }


        //Going to be triggered on every specific interval
        async void tick()
        {

            LogManager.Verbose("Trying to get all drive details");
            //get drive details
            LicenseManager.populateDrives();
            currentDispatcher.Invoke(() =>
            {
                Communications.updateStatus(Globalization.DriveDetailsFound);
            });

            //Get fedauth and rtfa cookies
            LogManager.Verbose("getting cookies manager");
            o365cookieManager cookieManager = new o365cookieManager(DriveManager.rootSiteUrl, CredentialManager.GetCredential().UserName, CredentialManager.GetCredential().Password);
            CookieContainer userCookies = cookieManager.getCookieContainer();
            LogManager.Verbose("cookies found");

            string fedAuth = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))[0].Value;
            string rtFA = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))[1].Value;

            //set cookie in IE
            LogManager.Verbose("setting cookies to IE");
            DriveManager.setCookiestoIE(fedAuth, rtFA, DriveManager.rootSiteUrl);

            //set cookie in IE
            LogManager.Verbose("setting cookies to IE for oneDriveHostUrl");
            DriveManager.setCookiestoIE(fedAuth, rtFA, DriveManager.oneDriveHostSiteUrl);

            //map drives
            currentDispatcher.Invoke(() =>
            {
                Communications.updateStatus(Globalization.MappingDrives);
            });

            LogManager.Verbose("Mapping drives");
            DriveManager.mapDrives(userCookies, Globalization.GettingoneDriveUrl, currentDispatcher);

            currentDispatcher.Invoke(() =>
            {
                Communications.updateStatus(Globalization.AllDrivesMapped);
            });
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
