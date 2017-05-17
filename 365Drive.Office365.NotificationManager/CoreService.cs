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
using _365Drive.Office365.GetTenancyURL;

namespace _365Drive.Office365
{
    public class Core
    {

        /// <summary>
        /// Instance of current class, so the ticker can be initiated from signin form
        /// </summary>
        public static Core coreInstance;

        /// <summary>
        /// Global settings / variables
        /// </summary>
        private TaskbarIcon notifyIcon;

        //NotificationManager.NotificationManager NotificationManager;
        ///We need to initialize a timer here which will make sure the sync happens every minute
        DispatcherTimer dispatcherTimer;
        DispatcherTimer iconTimer;
        DispatcherTimer notificationTimer;
        //Declare current dispacher
        Dispatcher currentDispatcher;

        /// <summary>
        /// to stop multiple thread and drive mapping name issues, we will make sure the thread will be called only once
        /// </summary>
        static bool busy = false;
        static int busyCounter = 0;

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
                //If the internet is not there, its exiting from here without notification which is creating confusion
                //if (!Utility.ready())
                //    return;
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

                //initiate notification timer
                notificationTimer = new DispatcherTimer();
                notificationTimer.Tick += NotificationTimer_Tick;

                //set the static variable to this
                coreInstance = this;

            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }


        /// <summary>
        /// attempt all the pending notification queues
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotificationTimer_Tick(object sender, EventArgs e)
        {
            Notification nextNotification = Communications.getNextNotificationQueueItem();
            if (nextNotification != null)
            {
                currentDispatcher.Invoke(() =>
                {
                    NotificationManager.NotificationManager.notify(nextNotification.Heading, nextNotification.Message, ToolTipIcon.Warning);
                });
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

                //make sure other treads are not running to avoid race conditions
                //We will wait 6 times which means 6 minutes. If until 6 minutes we still find its busy, which means something is fishy and ignore busy
                if (!busy || busyCounter == 5)
                {
                    busyCounter = 0;
                    //Code goes here
                    await Task.Run(() => tick());
                }
                else
                {
                    busyCounter++;
                    LogManager.Verbose("Already other thread is running");
                }
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
                //If the internet is not there, its exiting from here without notification which is creating confusion
                //if (!Utility.ready())
                //    return;


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
                        dispatcherTimer.Interval = new TimeSpan(0, 2, 15);
                    else
                        dispatcherTimer.Interval = new TimeSpan(0, 1, 0);

                    dispatcherTimer.Start();

                    //start the notification timer too
                    notificationTimer.Interval = new TimeSpan(0, 0, 2);
                    notificationTimer.Start();
                });

                //Make sure the network is connected (always)
                LogManager.Verbose("Registering network manager event to make sure we are always connected to internet");
                NetworkChange.NetworkAvailabilityChanged += EnsureInternetAccess;

                //Make sure the system powermode is NOT resume
                LogManager.Verbose("Registering power mode change event to make sure the system is not in resume mode");
                SystemEvents.PowerModeChanged += EnsurePowerMode;

                //call tick now
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
            //set busy to true so other thread doesnt access this code
            busy = true;

            try
            {
                //starting animation
                Animation.animatedIcontimer = iconTimer;
                Animation.notifyIcon = notifyIcon;

                //first stop animation to clear all previous steps
                Animation.Stop();

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
                    busy = false;
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
                    NotificationManager.NotificationManager.notify(Globalization.webClient, Globalization.webClientNotRunning, ToolTipIcon.Warning, CommunicationCallBacks.OpenWebClient);
                    Communications.CurrentState = States.Stopped;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Warning);
                    busy = false;
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
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Warning);
                    NotificationManager.NotificationManager.notify(Globalization.credentials, Globalization.NocredMessage, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication, true);
                    Communications.CurrentState = States.UserAction;
                    busy = false;
                    //if (!alreadyNotified)
                    //{
                    //    currentDispatcher.Invoke(() =>
                    //    {
                    //        CommunicationCallBacks.AskAuthentication();
                    //    });
                    //}
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

                #region Ensuring authentication type
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
                    busy = false;
                    return;
                }
                #endregion

                #region get and set tenancy name and urls
                //as this is must in next calls, lets fetch the tenancy name here
                LicenseValidationState tenancyNameState = DriveMapper.retrieveTenancyName(CredentialManager.GetCredential().UserName, CredentialManager.GetCredential().Password);
                if (tenancyNameState == LicenseValidationState.LoginFailed)
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
                    busy = false;
                    return;
                }
                else if (tenancyNameState == LicenseValidationState.CouldNotVerify)
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
                    busy = false;
                    return;
                }
                #endregion;



                #region getting cookies (used to setting in IE and authenticating for licensing)
                //Get fedauth and rtfa cookies
                LogManager.Verbose("getting cookies manager");
                GlobalCookieManager cookieManager = new GlobalCookieManager(DriveManager.rootSiteUrl, CredentialManager.GetCredential().UserName, CredentialManager.GetCredential().Password);
                CookieContainer userCookies = cookieManager.getCookieContainer();

                if (userCookies == null)
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
                    busy = false;
                    return;
                }
                LogManager.Verbose("cookies found");
                string fedAuth = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["fedauth"].Value;
                string rtFA = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["rtfa"].Value;
                #endregion

                #region Ensuring License
                //make sure we have valid license
                LicenseValidationState licenseValidationState = DriveMapper.EnsureLicense(CredentialManager.GetCredential().UserName, CredentialManager.GetCredential().Password, userCookies);
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
                    busy = false;
                    return;
                }
                else if (licenseValidationState == LicenseValidationState.ActivationFailed)
                {
                    string notificationMessage = string.Format(Globalization.LicenseActivationFailed, LicenseManager.lastActivationMessage);
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(notificationMessage);
                    });
                    LogManager.Verbose("License could not be verified");
                    NotificationManager.NotificationManager.notify(Globalization.LicenseValidationFailed, notificationMessage, ToolTipIcon.Error);
                    //reset the current state
                    Communications.CurrentState = States.Stopped;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Error);
                    busy = false;
                    return;
                }
                else if (licenseValidationState == LicenseValidationState.ActivatedFirstTime)
                {
                    LogManager.Verbose("License first time activated");
                    currentDispatcher.Invoke(() =>
                    {
                        //Communications.updateStatus(Globalization.LicenseActivatedFirstTime + ". " + LicenseManager.lastActivationMessage);
                    });
                    //NotificationManager.NotificationManager.notify(Globalization.License, Globalization.LicenseActivatedFirstTime + ". " + LicenseManager.lastActivationMessage, ToolTipIcon.Info);
                    //set it to running
                    Communications.CurrentState = States.Running;
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
                    busy = false;
                    return;
                }
                else if (licenseValidationState == LicenseValidationState.Expired || licenseValidationState == LicenseValidationState.Exceeded)
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.LicenseExpired);
                    });
                    LogManager.Verbose("License has been expired or exceeded limit");
                    NotificationManager.NotificationManager.notify(Globalization.License, Globalization.LicenseExpired, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                    //reset the current state
                    Communications.CurrentState = States.Hold;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Error);
                    busy = false;
                    return;
                }
                else if (licenseValidationState == LicenseValidationState.TenancyNotExist)
                {
                    string TenancyNotRegistered = String.Format(Globalization.TenancyNotRegistered, Constants.licensingBaseDomain);
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(TenancyNotRegistered);
                    });
                    LogManager.Verbose("Tenancy has not been signed up with us");
                    NotificationManager.NotificationManager.notify(Globalization.LicenseValidationFailedHeading, TenancyNotRegistered, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                    //reset the current state
                    Communications.CurrentState = States.Hold;
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Error);
                    busy = false;
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


                #region setting cookies to IE (already retrieved above)
                //set cookie in IE
                LogManager.Verbose("setting cookies to IE");
                DriveManager.setCookiestoIE(fedAuth, rtFA, DriveManager.rootSiteUrl);

                //set cookie in IE
                LogManager.Verbose("setting cookies to IE for oneDriveHostUrl");
                DriveManager.setCookiestoIE(fedAuth, rtFA, DriveManager.oneDriveHostSiteUrl);
                #endregion

                #region Getting mappable drive details
                LogManager.Verbose("Trying to get all drive details");
                //get drive details
                LicenseManager.populateDrives(userCookies);
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.DriveDetailsFound);
                });
                #endregion

                #region mapping drives
                //map drives
                currentDispatcher.Invoke(() =>
                {
                    Communications.updateStatus(Globalization.MappingDrives);
                });

                LogManager.Verbose("Mapping drives");

                //if the user has passed any document library INSIDE their OneDrive site collection, we need to pass onedrive cookies
                //userCookies.Add(new Cookie("FedAuth", fedAuth, "/", DriveManager.oneDriveHostSiteUrl));
                //userCookies.Add(new Cookie("rtFa", rtFA, "/", DriveManager.oneDriveHostSiteUrl));

                userCookies.Add(new Uri("https://" + new Uri(DriveManager.oneDriveHostSiteUrl).Authority), new Cookie("FedAuth", fedAuth));
                userCookies.Add(new Uri("https://" + new Uri(DriveManager.oneDriveHostSiteUrl).Authority), new Cookie("rtFA", rtFA));

                DriveManager.mapDrives(userCookies, Globalization.GettingoneDriveUrl, currentDispatcher);

                currentDispatcher.Invoke(() =>
                {
                    var totalMappedDrives = DriveManager.mappableDrives.FindAll(d => d.Drivestate != driveState.Deleted).Count.ToString();
                    Communications.updateStatus(string.Format(Globalization.AllDrivesMapped, totalMappedDrives));
                });
                #endregion


                Animation.Stop();
                busy = false;
            }
            catch (Exception ex)
            {
                Animation.Stop();
                busy = false;
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
