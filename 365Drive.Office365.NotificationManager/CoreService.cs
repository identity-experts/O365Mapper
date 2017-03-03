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
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;
using _365Drive.Office365.NotificationManager;

namespace _365Drive.Office365
{
    public class Core
    {


        /// <summary>
        /// Global settings / variables
        /// </summary>
        private TaskbarIcon notifyIcon;

        //NotificationManager.NotificationManager NotificationManager;
        ///We need to initialize a timer here which will make sure the sync happens every minute
        DispatcherTimer dispatcherTimer;
        DispatcherTimer iconTimer;
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
            try
            {
                if (!Utility.ready())
                    return;
                LogManager.Verbose("Initializing Notifications, Timer and other elements from core constructor");
                this.notifyIcon = notifyIcon;
                //this.NotificationManager = new NotificationManager.NotificationManager(notifyIcon);
                NotificationManager.NotificationManager.notifyIcon = notifyIcon;

                //Initialize the timer
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

                //initialize icon timer
                iconTimer = new DispatcherTimer();

                currentDispatcher = Dispatcher.CurrentDispatcher;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// Initiate the sync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!Utility.ready())
                    return;
                //Code goes here
                await Task.Run(() => tick());
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// Intialize 365Drive, which means make sure the logger, registry, credentials and all other required parameters are upto the mark.
        /// </summary>
        public async void Initialize()
        {
            try
            {
                if (!Utility.ready())
                    return;


                //Checking .NET version
                if (!Utility.checkFx45())
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.Nodotnet45);
                    });
                    LogManager.Verbose(".NET version 4.5 was not installed");
                    NotificationManager.NotificationManager.notify(Globalization.DotNetVersion, Globalization.Nodotnet45, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                    //reset the current state
                    Communications.CurrentState = States.Stopped;
                    return;
                }

                Communications.notifyIcon = notifyIcon;
                Communications.CurrentState = States.Running;
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.Initializing);
                });
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

                //Make sure the exe is registered as startup
                if (!RegistryManager.IsDev)
                {
                    LogManager.Verbose("Make sure the exe is registered as startup");
                    RegistryManager.RegisterExeOnStartup();
                }
                //timer settings
                LogManager.Verbose("setting timer");

                //call tick first time
                currentDispatcher.Invoke(() =>
                {
                    //Set the value received from registry
                    if (RegistryManager.IsDev)
                        dispatcherTimer.Interval = new TimeSpan(0, 10, 0);
                    else
                        dispatcherTimer.Interval = new TimeSpan(0, 0, 15);

                    dispatcherTimer.Start();
                });

                //Make sure the network is connected (always)
                LogManager.Verbose("Registering network manager event to make sure we are always connected to internet");
                NetworkChange.NetworkAvailabilityChanged += EnsureInternetAccess;

                //Make sure the system powermode is NOT resume
                LogManager.Verbose("Registering power mode change event to make sure the system is not in resume mode");
                SystemEvents.PowerModeChanged += EnsurePowerMode;

                //call it now
                await Task.Run(() => tick());
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }


        /// <summary>
        /// Make sure we are connected to internet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnsurePowerMode(object sender, PowerModeChangedEventArgs e)
        {
            try
            {
                PowerCheck(e);
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }


        /// <summary>
        /// When power comes back to resume mode, restart the services. 
        /// </summary>
        bool PowerCheck(PowerModeChangedEventArgs e)
        {
            try
            {
                LogManager.Verbose("Checking Internet access");
                if (!Utility.ensurePowerMode(e))
                {
                    //Lets give 30 seconds of breathing time to let the credential manager load 
                    Thread.Sleep(new TimeSpan(0, 0, 30));
                    LogManager.Verbose("coming back to resume");
                    Initialize();
                    //NotificationManager.NotificationManager.notify(Globalization.InternetConnection, Globalization.NoInternet, ToolTipIcon.Warning);
                    return false;
                }
                else
                {
                    LogManager.Verbose("Powermode sleeping");
                    return true;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return true;
        }
        //Going to be triggered on every specific interval
        async void tick()
        {
            try
            {
                //starting animation
                Animation.animatedIcontimer = iconTimer;
                Animation.notifyIcon = notifyIcon;

                ///start the inprogress
                Animation.Animate(AnimationTheme.Inprogress);

                #region Ensuring Internet
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.EnsuringInternet);
                });
                if (!InternetCheck())
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.NoInternet);
                    });
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Warning);
                    return;
                }
                #endregion

                #region Ensuring webClient service running or not
                LogManager.Verbose("Making sure webclient is running or not");
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.EnsuringwebClient);
                });
                if (!Utility.webClientServiceRunning())
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.webClientNotRunning);
                    });
                    LogManager.Verbose("WebClient is NOT running");
                    NotificationManager.NotificationManager.notify(Globalization.webClient, Globalization.webClientNotRunning, ToolTipIcon.Warning);
                    Communications.CurrentState = States.Stopped;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Warning);
                    return;
                }
                #endregion

                #region Ensuring credentials
                //Mare sure credential is present
                LogManager.Verbose("Checking, do we have credentials?");
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.EnsuringCredential);
                });
                if (CredentialManager.ensureCredentials() == CredentialState.Notpresent)
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.CredentialNotPresent);
                    });
                    LogManager.Verbose("credentials not present");
                    NotificationManager.NotificationManager.notify(Globalization.credentials, Globalization.NocredMessage, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                    Communications.CurrentState = States.UserAction;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Warning);
                    return;
                }
                else
                {
                    LogManager.Verbose("Credential present");
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.CredentialsPresent);
                    });
                    //reset the current state
                    Communications.CurrentState = States.Running;
                }
                #endregion

                //Mare sure the user authentication type is supported
                LogManager.Verbose("Checking, does the user authentication type is supported");
                if (!DriveManager.isAllowedFedType(CredentialManager.GetCredential().UserName))
                {
                    LogManager.Verbose("authentication type is NOT supported");
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.AuthenticationTypeNotSupported);
                    });
                    NotificationManager.NotificationManager.notify(Globalization.Federation, Globalization.AuthenticationTypeNotSupported, ToolTipIcon.Warning);
                    Communications.CurrentState = States.Stopped;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Error);
                    return;
                }

                #region Ensuring License
                //make sure we have valid license
                LicenseValidationState licenseValidationState = DriveMapper.EnsureLicense(CredentialManager.GetCredential().UserName, CredentialManager.GetCredential().Password);
                if (licenseValidationState == LicenseValidationState.CouldNotVerify)
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.LicenseValidationFailed);
                    });
                    LogManager.Verbose("License could not be verified");
                    NotificationManager.NotificationManager.notify(Globalization.LicenseValidationFailed, Globalization.LicenseValidationFailed, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                    //reset the current state
                    Communications.CurrentState = States.Stopped;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Error);
                    return;
                }
                else if (licenseValidationState == LicenseValidationState.LoginFailed)
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.LoginFailed);
                    });
                    LogManager.Verbose("credentials not valid or app isnt registered");
                    NotificationManager.NotificationManager.notify(Globalization.credentials, Globalization.LoginFailed, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                    //reset the current state
                    Communications.CurrentState = States.UserAction;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Warning);
                    return;
                }
                else if (licenseValidationState == LicenseValidationState.Expired || licenseValidationState == LicenseValidationState.Exceeded)
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.LicenseExpired);
                    });
                    LogManager.Verbose("License has been expired or exceeded limit");
                    NotificationManager.NotificationManager.notify(Globalization.credentials, Globalization.LicenseExpired, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                    //reset the current state
                    Communications.CurrentState = States.Hold;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Error);
                    return;
                }
                else
                {
                    LogManager.Verbose("License present");
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.LicenseFoundOK);
                    });
                    //set it to running
                    Communications.CurrentState = States.Running;
                }
                #endregion

                #region Getting mappable drive details
                LogManager.Verbose("Trying to get all drive details");
                //get drive details
                LicenseManager.populateDrives();
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.DriveDetailsFound);
                });
                #endregion

                #region Getting auth cookies and setting them to IE
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
                #endregion

                #region mapping drives
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
                #endregion


                Animation.Stop();
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// Make sure we are connected to internet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnsureInternetAccess(object sender, NetworkAvailabilityEventArgs e)
        {
            InternetCheck();
        }


        /// <summary>
        /// Make sure internet connected
        /// </summary>
        bool InternetCheck()
        {
            LogManager.Verbose("Checking Internet access");
            if (!Utility.ensureInternet())
            {
                LogManager.Verbose("No internet found");
                NotificationManager.NotificationManager.notify(Globalization.InternetConnection, Globalization.NoInternet, ToolTipIcon.Warning);
                return false;
            }
            else
            {
                LogManager.Verbose("Internet is OK");
                return true;
            }
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
