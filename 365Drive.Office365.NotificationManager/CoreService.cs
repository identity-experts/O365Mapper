﻿using System;
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
using _365Drive.Office365.UpdateManager;
using _365Drive.Office365.UI.About;
using System.ComponentModel;

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
                //initialize icon timer
                iconTimer = new DispatcherTimer();

                //starting animation
                Animation.animatedIcontimer = iconTimer;
                Animation.notifyIcon = notifyIcon;

                ///start the inprogress
                Animation.Animate(AnimationTheme.Inprogress);

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


                //Write version number to registry
                WriteVersionToRegistry();

                string dontAskForUpdates = RegistryManager.Get(RegistryKeys.DontAskForUpdates);
                if (dontAskForUpdates != "1")
                {
                    //check for updates
                    CheckForUpdates();
                }

                //lets delete cookies before we start our game to keep things simple
                //in many cases, the internet explore is setting the FEDAUTH cookie which is conflicting with 3M cookie. Lets ensure everything is clearned before we set cookie
                clearCookies();

                //call tick now
                await Task.Run(() => tick());
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        void clearCookies()
        {
            //we need a piece here so lets not throw error and cause any issues
            try
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args)
                {
                    DriveManager.clearCookies();
                });
                bw.RunWorkerAsync();
            }
            catch { }
        }


        /// <summary>
        /// write version number to registry
        /// </summary>
        void WriteVersionToRegistry()
        {
            try
            {
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                //put the version number to registry 
                RegistryManager.Set(RegistryKeys.Version, currentVersion);
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        void CheckForUpdates()
        {
            try
            {
                //check for updates
                VersionResponse version = Versions.LatestVersion();

                //get current version
                if (!string.IsNullOrEmpty(version.data.version) && !string.IsNullOrEmpty(version.data.x64) && !string.IsNullOrEmpty(version.data.x86))
                {
                    string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    if (Versions.compareVersion(currentVersion, version.data.version))
                    {
                        Updates updatePrompt = new Updates();
                        updatePrompt.Show();
                    }
                }
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
            //if incase SSO fails, the execution will restart form here
            start:

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
                CredentialState credState = CredentialManager.ensureCredentials();
                if (credState == CredentialState.Notpresent)
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
                else if (credState == CredentialState.ServerNotConnectable)
                {
                    currentDispatcher.Invoke(() =>
                    {
                        Communications.updateStatus(Globalization.CouldNotRetrieveUPN);
                    });
                    LogManager.Verbose("credentials not present");
                    Animation.Stop();
                    Animation.Animate(AnimationTheme.Warning);
                    NotificationManager.NotificationManager.notify(Globalization.CouldNotRetrieveUPN, Globalization.NocredMessage, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication, true);
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
                    // set the site urls in Drive Manager for further use in the code
                    DriveManager.SetSharePointUrls();

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

                #region Ensuring SSO authentication type
                //Mare sure the user authentication type is supported
                LogManager.Verbose("Checking, if we have SSO and the auth type is allowed for SSO");
                if (!DriveManager.isAllowedSSOFedType(CredentialManager.GetCredential().UserName))
                {
                    LogManager.Verbose("trying SSO in non-sso type methods");

                    //lets disable all what we have done 
                    bool isAutoSSOEnabled = CredentialManager.disableAutoSSO();

                    //restart
                    if (isAutoSSOEnabled)
                        goto start;
                }
                #endregion

                #region getting cookies (used to setting in IE and authenticating for licensing)
                //Get fedauth and rtfa cookies
               LogManager.Verbose("getting cookies manager");
                GlobalCookieManager cookieManager = new GlobalCookieManager(DriveManager.rootSiteUrl, CredentialManager.GetCredential().UserName, CredentialManager.GetCredential().Password);
                CookieContainer userCookies = cookieManager.getCookieContainer();

                if (userCookies == null)
                {
                    if (!_365Drive.Office365.UI.MFA.ReminderStates.mfaConfirmationTimeNow)
                    {
                        currentDispatcher.Invoke(() =>
                        {
                            Communications.updateStatus(Globalization.RemindMeLaterStatus);
                        });
                        LogManager.Verbose("User opted for remind later (MFA)");
                        NotificationManager.NotificationManager.notify(Globalization.SignInPageheader, Globalization.RemindMeLaterNotification, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                        //reset the current state
                        Communications.CurrentState = States.UserAction;
                        Animation.Stop();
                        Animation.Animate(AnimationTheme.Warning);
                        busy = false;
                        currentDispatcher.Invoke(() =>
                        {
                            EnableMFAMenuitem();

                        });

                        return;
                    }
                    else
                    {


                        //lets make inform our engine that SSO failed
                        bool blwasAutoSSOOn = CredentialManager.isItSSOTry();
                        bool blRetryAgain = false;
                        if (blwasAutoSSOOn)
                        {
                            SSOFailed();
                            int iPendingRetries = CredentialManager.SSOPendingRetries();
                            blRetryAgain = iPendingRetries > 0;
                        }

                        DriveManager.StopClearCache = true;
                        //lets make sure if we have autoSSO ON, lets make it off
                        //bool blwasAutoSSOOn = CredentialManager.disableAutoSSO();

                        currentDispatcher.Invoke(() =>
                        {
                            if (blwasAutoSSOOn && blRetryAgain)
                                Communications.updateStatus(Globalization.InformAutoSSORetry);
                            else if (blwasAutoSSOOn && !blRetryAgain)
                            {
                                Communications.updateStatus(Globalization.InformAutoSSOFailed);
                            }
                            else
                                Communications.updateStatus(Globalization.LoginFailed);
                        });
                        LogManager.Verbose("credentials not valid or app isnt registered");
                        if (!blwasAutoSSOOn && !blRetryAgain)
                            NotificationManager.NotificationManager.notify(Globalization.credentials, Globalization.LoginFailed, ToolTipIcon.Warning, CommunicationCallBacks.AskAuthentication);
                        //reset the current state
                        Communications.CurrentState = States.UserAction;
                        Animation.Stop();
                        if (!blwasAutoSSOOn && !blRetryAgain)
                            Animation.Animate(AnimationTheme.Warning);
                        busy = false;
                        return;
                    }
                }
                LogManager.Verbose("cookies found");
                string fedAuth = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["fedauth"].Value;
                string rtFA = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["rtfa"].Value;
                #endregion
                
                #region setting cookies to IE (already retrieved above)


                //set cookie in IE
                LogManager.Verbose("setting cookies to IE");
                DriveManager.setCookiestoIE(fedAuth, rtFA, DriveManager.rootSiteUrl);

                //set cookie in IE
                LogManager.Verbose("setting cookies to IE for oneDriveHostUrl");
                DriveManager.setCookiestoIE(fedAuth, rtFA, DriveManager.oneDriveHostSiteUrl);
                #endregion

                #region IPUT SPECIFIC
                DriveManager.setCookiestoIE(fedAuth, rtFA, "https://sharepoint.com");
                try
                {
                    if (userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["buid"] != null && userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["estsauthpersistent"] != null)
                    {
                        string buid = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["buid"].Value;
                        string estsauthpersistent = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["estsauthpersistent"].Value;
                        try
                        {
                            DriveManager.setIPUTCookiestoIE(buid, estsauthpersistent);
                        }
                        catch
                        {
                            // do nothing, this is edge case
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Verbose("Error in setting build and estsauth");
                    LogManager.Error(ex.Message);
                    LogManager.Error(ex.StackTrace);
                }

                #endregion

                #region Getting mappable drive details
                LogManager.Verbose("Trying to get all drive details");
                //get drive details
                CloudConnector.LicenseManager.populateDrives(userCookies);
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

                //LogManager.Exception(method, ex);
                LogManager.Exception(method + " UNEXPECTED ERROR FROM CORE :(", ex);
            }
        }




        /// <summary>
        /// SSO failed which means increase SSO counter and do all other tasks
        /// </summary>
        static void SSOFailed()
        {
            CredentialManager.ssoCounter++;
        }

        /// <summary>
        /// If user prompts for later in MFA, enable menu item now
        /// </summary>
        public static void EnableMFAMenuitem()
        {
            System.Windows.Controls.ContextMenu ctxMenu = (System.Windows.Controls.ContextMenu)System.Windows.Application.Current.FindResource("SysTrayMenu");
            System.Windows.Controls.ItemCollection items = ctxMenu.Items;

            //foreach (var item in items)
            //{
            //    if (item.GetType() == typeof(System.Windows.Controls.MenuItem))
            //    {
            //        // do your work with the item 
            //        if (((System.Windows.Controls.MenuItem)item).Name == "MFA")
            //        {

            //  Add to main menu
            System.Windows.Controls.MenuItem promptMFA = new System.Windows.Controls.MenuItem();
            promptMFA.Name = "MFA";
            promptMFA.Header = "Prompt MFA";
            promptMFA.Click += PromptMFA_Click;
            ((System.Windows.Controls.ContextMenu)System.Windows.Application.Current.FindResource("SysTrayMenu")).Items.Add(promptMFA);
            //        }
            //    }
            //}
        }


        /// <summary>
        /// Clear MFA Cache
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PromptMFA_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //throw new NotImplementedException();
            CommunicationCallBacks.ClearMFACache();

            //finally again disable MFA
            DisableMFAMenuitem();
        }


        /// <summary>
        /// If user prompts for later in MFA, enable menu item now
        /// </summary>
        public static void DisableMFAMenuitem()
        {
            System.Windows.Controls.ContextMenu ctxMenu = (System.Windows.Controls.ContextMenu)System.Windows.Application.Current.FindResource("SysTrayMenu");
            System.Windows.Controls.ItemCollection items = ctxMenu.Items;
            System.Windows.Controls.MenuItem itemtobeRemoved = null;
            foreach (var item in items)
            {
                if (item.GetType() == typeof(System.Windows.Controls.MenuItem))
                {
                    // do your work with the item 
                    if (((System.Windows.Controls.MenuItem)item).Name == "MFA")
                    {
                        itemtobeRemoved = ((System.Windows.Controls.MenuItem)item);
                    }
                }
            }
            if (itemtobeRemoved != null)
                ctxMenu.Items.Remove(itemtobeRemoved);
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
