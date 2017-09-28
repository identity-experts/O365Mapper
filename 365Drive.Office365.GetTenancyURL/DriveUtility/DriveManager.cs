using _365Drive.Office365.CommunicationManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using _365Drive.Office365.GetTenancyURL;
using Newtonsoft.Json;

namespace _365Drive.Office365.CloudConnector
{
    public static class DriveManager
    {
        #region to set cookies to ie
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetCookie(string lpszUrlName, string lbszCookieName, string lpszCookieData);
        [DllImport("wininet.dll", SetLastError = true)]
        public static extern bool InternetGetCookie(string lpszUrl, string lpszCookieName, StringBuilder lpszCookieData, ref int lpdwSize);

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetOption(
     int hInternet,
     int dwOption,
     string lpBuffer,
     int dwBufferLength
 );

        [DllImport("kernel32.dll")]
        #endregion
        public static extern uint GetLastError();


        ///Allowed federation types
        /// 
        static FedType?[] AllowedAuthenticationTypes = { FedType.Cloud, FedType.AAD, FedType.ADFS };

        /// <summary>
        /// Allowed single sign on auth
        /// </summary>
        static FedType?[] AllowedSSOAuthenticationTypes = { FedType.ADFS };

        /// <summary>
        /// Collection of all mappable drives
        /// </summary>
        public static List<Drive> mappableDrives { get; set; }

        /// <summary>
        /// goint to be useful at many places
        /// </summary>
        public static string rootSiteUrl { get; set; }

        /// <summary>
        /// goint to be useful at many places
        /// </summary>
        public static string oneDriveHostSiteUrl { get; set; }

        /// <summary>
        /// goint to be useful at many places
        /// </summary>
        public static string AADSSODomainName { get; set; }


        /// <summary>
        /// goint to be useful at many places
        /// </summary>
        public static string ADFSAuthURL { get; set; }

        /// <summary>
        /// goint to be useful at many places
        /// </summary>
        public static FedType? FederationType { get; set; }

        /// <summary>
        /// goint to be useful at many places
        /// </summary>
        public static string FederationProtocol { get; set; }
        /// <summary>
        /// Set authentication type
        /// </summary>
        /// <param name="upn">User principal name</param>
        /// <returns></returns>
        public static FedType RetrieveAuthType(string upn)
        {
            FedType userFedType = FedType.Cloud;

            try
            {
                if (!Utility.ready())
                    return FedType.NA;

                LogManager.Verbose("Fetching authentication type using realm");
                FederationType = FedType.Cloud;

                Task<string> realM = HttpClientHelper.GetAsync(String.Format(StringConstants.UserrealMrequest, upn));
                realM.Wait();

                RealM userRealM = JsonConvert.DeserializeObject<RealM>(realM.Result);

                //only for debug until we find way to differetiate cloud and AAD Connect
                if (upn.ToLower() == "leigh.wood@node-it.com" && RegistryManager.IsDev)
                {
                    FederationType = FedType.Cloud;
                }

                //AAD SSO
                else if (userRealM.is_dsso_enabled != null && userRealM.is_dsso_enabled == true)
                {
                    LogManager.Verbose("AAD SSO auth found");
                    AADSSODomainName = userRealM.DomainName;
                    userFedType = FedType.AAD;
                    FederationType = FedType.AAD;
                }

                //adfs
                else if (userRealM.NameSpaceType.ToLower() == "federated")
                {
                    LogManager.Verbose("ADFS auth found");
                    AADSSODomainName = userRealM.DomainName;
                    userFedType = FedType.ADFS;
                    ADFSAuthURL = userRealM.AuthURL;
                    FederationType = FedType.ADFS;
                    FederationProtocol = userRealM.federation_protocol;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return userFedType;
        }


        public static void clearAll()
        {
            try
            {
                if (!Utility.ready())
                    return;
                LogManager.Verbose("Deleting all entries");

                //clear all drives
                mappableDrives = new List<Drive>();
                rootSiteUrl = string.Empty;
                oneDriveHostSiteUrl = string.Empty;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return;
            }
        }

        /// <summary>
        /// make sure the user federation type is something we do
        /// </summary>
        /// <param name="upn"></param>
        public static bool isAllowedFedType(string upn)
        {
            try
            {
                if (!Utility.ready())
                    return false;
                LogManager.Verbose("Ensuring the authentication type");
                //if we still havent retrieved federation type, do so
                if (FederationType == null)
                {
                    LogManager.Verbose("Could not retrieve authentication type, so getting it");
                    RetrieveAuthType(upn);
                }
                return AllowedAuthenticationTypes.Contains(FederationType);
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return false;
            }

        }


        /// <summary>
        /// make sure the user federation type is something we do
        /// </summary>
        /// <param name="upn"></param>
        public static bool isAllowedSSOFedType(string upn)
        {
            try
            {
                if (!Utility.ready())
                    return false;
                LogManager.Verbose("Ensuring the authentication type");
                //if we still havent retrieved federation type, do so
                if (FederationType == null)
                {
                    LogManager.Verbose("Could not retrieve authentication type, so getting it");
                    RetrieveAuthType(upn);
                }
                return AllowedSSOAuthenticationTypes.Contains(FederationType);
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return false;
            }

        }

        /// <summary>
        /// Add a new drive to mappable drive
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <param name="driveName"></param>
        /// <param name="driveUrl"></param>
        public static void addDrive(string driveLetter, string driveName, string driveUrl)
        {
            try
            {
                if (!Utility.ready())
                    return;

                if (mappableDrives == null)
                    mappableDrives = new List<Drive>();
                if (!mappableDrives.Any(d => d.DriveLetter.ToLower() == driveLetter.ToLower()))
                {
                    LogManager.Verbose("Adding a new drive to mappablearray: " + driveUrl);
                    //Create a new drive class instance and add the mappable drive
                    Drive drive = new Drive();
                    drive.DriveLetter = driveLetter;
                    drive.DriveName = driveName;
                    drive.DriveUrl = driveUrl;
                    drive.Drivetype = driveType.DocLib;
                    //if no state is passed, which means its active
                    drive.Drivestate = driveState.Active;
                    mappableDrives.Add(drive);
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }


        /// <summary>
        /// Add drive with state
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <param name="driveName"></param>
        /// <param name="driveUrl"></param>
        /// <param name="state"></param>
        public static void addDrive(string driveLetter, string driveName, string driveUrl, driveState state)
        {
            try
            {
                if (!Utility.ready())
                    return;
                if (mappableDrives == null)
                    mappableDrives = new List<Drive>();
                if (!mappableDrives.Any(d => d.DriveLetter.ToLower() == driveLetter.ToLower()))
                {
                    LogManager.Verbose("Adding a new drive to mappablearray: " + driveUrl);
                    //Create a new drive class instance and add the mappable drive
                    Drive drive = new Drive();
                    drive.DriveLetter = driveLetter;
                    drive.DriveName = driveName;
                    drive.DriveUrl = driveUrl;
                    drive.Drivestate = state;
                    drive.Drivetype = driveType.DocLib;
                    mappableDrives.Add(drive);
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// Pass all values without asusmptions
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <param name="driveName"></param>
        /// <param name="driveUrl"></param>
        /// <param name="state"></param>
        /// <param name="type"></param>
        public static void addDrive(string driveLetter, string driveName, string driveUrl, driveState state, driveType type)
        {
            try
            {
                if (!Utility.ready())
                    return;

                if (mappableDrives == null)
                    mappableDrives = new List<Drive>();
                if (!mappableDrives.Any(d => d.DriveLetter.ToLower() == driveLetter.ToLower()))
                {
                    LogManager.Verbose("Adding a new drive to mappablearray: " + driveUrl);
                    //Create a new drive class instance and add the mappable drive
                    Drive drive = new Drive();
                    drive.DriveLetter = driveLetter;
                    drive.DriveName = driveName;
                    drive.DriveUrl = driveUrl;
                    drive.Drivestate = state;
                    drive.Drivetype = type;
                    mappableDrives.Add(drive);
                }
                else
                {
                    //update the drive to make sure there are any updates
                    LogManager.Verbose("updating drive to mappablearray: " + driveUrl);
                    //Create a new drive class instance and add the mappable drive
                    mappableDrives.FirstOrDefault(d => d.DriveLetter.ToLower() == driveLetter.ToLower()).DriveLetter = driveLetter;
                    mappableDrives.FirstOrDefault(d => d.DriveLetter.ToLower() == driveLetter.ToLower()).DriveName = driveName;
                    mappableDrives.FirstOrDefault(d => d.DriveLetter.ToLower() == driveLetter.ToLower()).DriveUrl = driveUrl;
                    mappableDrives.FirstOrDefault(d => d.DriveLetter.ToLower() == driveLetter.ToLower()).Drivestate = state;
                    mappableDrives.FirstOrDefault(d => d.DriveLetter.ToLower() == driveLetter.ToLower()).Drivetype = type;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// Add drive with state
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <param name="driveName"></param>
        /// <param name="driveUrl"></param>
        /// <param name="state"></param>
        public static void addDrive(string driveLetter, string driveName, string driveUrl, driveType type)
        {
            try
            {
                if (!Utility.ready())
                    return;

                if (mappableDrives == null)
                    mappableDrives = new List<Drive>();
                if (!mappableDrives.Any(d => d.DriveLetter.ToLower() == driveLetter.ToLower()))
                {
                    LogManager.Verbose("Adding a new drive to mappablearray: " + driveUrl);
                    //Create a new drive class instance and add the mappable drive
                    Drive drive = new Drive();
                    drive.DriveLetter = driveLetter;
                    drive.DriveName = driveName;
                    drive.DriveUrl = driveUrl;
                    drive.Drivestate = driveState.Active;
                    drive.Drivetype = type;
                    mappableDrives.Add(drive);
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }
        /// <summary>
        /// Set the cookies in IE to keep them persistant and used by drive mapper
        /// </summary>
        /// <param name="cookies"></param>
        public static void setCookiestoIE(string fedAuth, string rtFA, string strUrl)
        {
            try
            {
                if (!Utility.ready())
                    return;
                //as many times this method is called WITHOUT URL (Probably user is signing out before the tenancy name is get, it puts unnecessary errors in event. Lets handle it!
                Uri host;
                if (!String.IsNullOrEmpty(strUrl) && Uri.TryCreate(strUrl, UriKind.Absolute, out host))
                {
                    LogManager.Verbose("setting fedAuth and rtFA cookie in IE");
                    ///Setting fedAuth
                    bool FedAuthcookiesetresult = InternetSetCookie("https://" + new Uri(strUrl).Authority, "FedAuth", fedAuth + ";" + "Expires = " + DateTime.Now.AddDays(10).ToString("R"));
                    LogManager.Verbose("fedAuth cookie setIE result: " + FedAuthcookiesetresult);

                    ///setting rtFA
                    bool rtFAcookiesetresult = InternetSetCookie("https://" + new Uri(strUrl).Authority, "rtFa", rtFA + ";" + "Expires = " + DateTime.Now.AddDays(10).ToString("R"));
                    LogManager.Verbose("rtFA cookie setIE result: " + rtFAcookiesetresult);

                    if (!FedAuthcookiesetresult)
                    {

                        uint lastError = GetLastError(); //this will return 87  for www.nonexistent.com
                        LogManager.Verbose("cookie setIE failed with error uint code: " + lastError.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }


        //IPUT
        /// <summary>
        /// Set the cookies in IE to keep them persistant and used by drive mapper
        /// </summary>
        /// <param name="cookies"></param>
        public static void setIPUTCookiestoIE(string buid, string estsauthpersistent)
        {
            try
            {
                if (!Utility.ready())
                    return;

                LogManager.Verbose("setting fedAuth and rtFA cookie in IE");

                //setting rtFA
                bool rtFAcookiesetresult = InternetSetCookie("https://" + new Uri("https://login.microsoftonline.com").Authority, "ESTSAUTHPERSISTENT", estsauthpersistent + ";" + "Expires = " + DateTime.Now.AddDays(10).ToString("R"));
                LogManager.Verbose("ESTSAUTHPERSISTENT cookie setIE result: " + rtFAcookiesetresult);


                //Setting fedAuth
                bool FedAuthcookiesetresult = InternetSetCookie("https://" + new Uri("https://login.microsoftonline.com").Authority, "buid", buid + ";" + "Expires = " + DateTime.Now.AddDays(10).ToString("R"));
                LogManager.Verbose("buid cookie setIE result: " + FedAuthcookiesetresult);


                if (!FedAuthcookiesetresult)
                {

                    uint lastError = GetLastError(); //this will return 87  for www.nonexistent.com
                    LogManager.Verbose("cookie setIE failed with error uint code: " + lastError.ToString());
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }


        /// <summary>
        /// Set the cookies in IE to keep them persistant and used by drive mapper
        /// </summary>
        /// <param name="cookies"></param>
        public static void deleteCookiesFromIE(string fedAuth, string rtFA, string strUrl)
        {
            try
            {
                if (!Utility.ready())
                    return;


                //as many times this method is called WITHOUT URL (Probably user is signing out before the tenancy name is get, it puts unnecessary errors in event. Lets handle it!
                Uri host;
                if (!String.IsNullOrEmpty(strUrl) && Uri.TryCreate(strUrl, UriKind.Absolute, out host))
                {

                    LogManager.Verbose("setting fedAuth and rtFA cookie in IE");
                    ///Setting fedAuth
                    bool FedAuthcookiesetresult = InternetSetCookie("https://" + new Uri(strUrl).Authority, "FedAuth", fedAuth + ";" + "Expires = " + DateTime.Now.AddDays(-10).ToString("R"));
                    LogManager.Verbose("fedAuth cookie setIE result: " + FedAuthcookiesetresult);

                    ///setting rtFA
                    bool rtFAcookiesetresult = InternetSetCookie("https://" + new Uri(strUrl).Authority, "rtFa", rtFA + ";" + "Expires = " + DateTime.Now.AddDays(-10).ToString("R"));

                    //delete cookies
                    InternetSetOption(0, 42, null, 0);

                    LogManager.Verbose("rtFA cookie setIE result: " + rtFAcookiesetresult);

                    if (!FedAuthcookiesetresult)
                    {

                        uint lastError = GetLastError(); //this will return 87  for www.nonexistent.com
                        LogManager.Verbose("cookie setIE failed with error uint code: " + lastError.ToString());
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
        /// map SharePoint Drives
        /// </summary>
        public static void mapDrives(CookieContainer userCookies, string gettingOneDriveUrlText, Dispatcher currentDispatcher)
        {
            try
            {
                if (!Utility.ready())
                    return;

                //{0}=Letter,{1}=Path,{2}=Name
                string singleDrivePowershellCommand = "Try{{If ((Test-Path " + "{0}" + ")) {{(New-Object -Com WScript.Network).RemoveNetworkDrive(\"" + "{0}" + "\",$true,$true);Remove-PSDrive " + "{0}" + " -Force;}};(New-Object -ComObject WScript.Network).MapNetworkDrive(\"" + "{0}" + "\", \"" + "{1}" + "\", $true, \"" + "\", \"" + "\"); (New-Object -ComObject Shell.Application).NameSpace(\"" + "{0}" + "\").Self.Name = \"" + "{2}" + "\";$RegKey = \"HKCU:\\software\\microsoft\\windows\\currentversion\\explorer\\mountpoints2\\{3}\";$RegKey2 = \"HKCU:\\software\\microsoft\\windows\\currentversion\\explorer\\mountpoints2\\{4}\";Set-ItemProperty -Path $RegKey2 -Name _LabelFromReg -Value \"{2}\";Set-ItemProperty -Path $RegKey -Name _LabelFromReg -Value \"{2}\";}}Catch{{}}";
                string singleRepairDrivePowershellCommand = "Try{{(New-Object -ComObject Shell.Application).NameSpace(\"" + "{0}" + "\").Self.Name = \"" + "{1}" + "\";$RegKey = \"HKCU:\\software\\microsoft\\windows\\currentversion\\explorer\\mountpoints2\\{2}\";$RegKey2 = \"HKCU:\\software\\microsoft\\windows\\currentversion\\explorer\\mountpoints2\\{3}\";Set-ItemProperty -Path $RegKey2 -Name _LabelFromReg -Value \"{1}\";Set-ItemProperty -Path $RegKey -Name _LabelFromReg -Value \"{1}\";}}Catch{{}}";
                string singleDriveunmapPowershellCommand = "Try{{If ((Test-Path " + "{0}" + ")) {{(New-Object -Com WScript.Network).RemoveNetworkDrive(\"" + "{0}" + "\",$true,$true);Remove-PSDrive " + "{0}" + " -Force;}};}}Catch{{}}";
                //string restartExplorer = ";Stop-Process -processName: Explorer;";
                string restartExplorer = ";";

                StringBuilder powerShell = new StringBuilder();
                bool anyDelete = false;

                //loop through each drive and make a complete powershell command
                foreach (Drive d in mappableDrives)
                {
                    LogManager.Verbose("Ensuring user has access to:" + d.DriveUrl);
                    string webDavPath;
                    if (!string.IsNullOrEmpty(d.DriveUrl) && d.Drivetype != driveType.OneDrive)
                        webDavPath = d.DriveUrl.Replace("http://", "\\\\").Replace("https://", "\\\\").Replace(new Uri(d.DriveUrl).Host, new Uri(d.DriveUrl).Host + (new Uri(d.DriveUrl).Scheme == Uri.UriSchemeHttps ? "@SSL\\DavWWWRoot" : "\\DavWWWRoot")).Replace("/", "\\");
                    else
                        webDavPath = string.Empty;

                    if (!isDriveExists(d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + @":\", webDavPath) && d.Drivestate == driveState.Active)
                    {
                        bool userHasAccess = true;
                        if (d.Drivestate != driveState.Deleted && d.Drivetype != driveType.OneDrive)
                        {
                            userHasAccess = DriveMapper.userHasAccess(new Uri(d.DriveUrl), userCookies);
                            //If user doesnt have access, notify the user
                            try
                            {
                                if (!userHasAccess)
                                {
                                    //Notify user about no access
                                    CommunicationManager.Communications.queueNotification("Access Denied", "Unable to map drive " + d.DriveLetter + ". You do not have permission to '" + d.DriveName + "'");
                                }
                            }
                            catch
                            {
                                //No need to act if it doesn't work
                            }
                        }
                        if (d.Drivestate == driveState.Deleted || d.Drivetype == driveType.OneDrive || userHasAccess)
                        {
                            LogManager.Verbose("Its found that user has access OR drive is to be removed, continueing with mapping drive:" + d.DriveUrl);
                            string psCommand = string.Empty;

                            //checking for OneDrive
                            if (d.Drivetype == driveType.OneDrive)
                            {
                                //map drives
                                currentDispatcher.Invoke(() =>
                                {
                                    Communications.updateStatus(gettingOneDriveUrlText);
                                });

                                ///getting user drive details
                                d.DriveUrl = DriveMapper.getOneDriveUrl(rootSiteUrl, userCookies);
                            }

                            //If the drive is NOT changed by company admin, which means still active. Continue as we do.
                            if (d.Drivestate == driveState.Active)
                            {
                                //we have to do it again because incase of OneDrive it is blank above 
                                webDavPath = d.DriveUrl.Replace("http://", "\\\\").Replace("https://", "\\\\").Replace(new Uri(d.DriveUrl).Host, new Uri(d.DriveUrl).Host + (new Uri(d.DriveUrl).Scheme == Uri.UriSchemeHttps ? "@SSL\\DavWWWRoot" : "\\DavWWWRoot")).Replace("/", "\\");

                                LogManager.Verbose("WebDav Path:" + webDavPath);
                                string regKey = webDavPath.Replace(@"\", "#").EndsWith("#") ? webDavPath.Replace(@"\", "#").TrimEnd('#') : webDavPath.Replace(@"\", "#");
                                string webDavPath2 = webDavPath.Replace(@"\", "#").Replace("#DavWWWRoot", "").EndsWith("#") ? webDavPath.Replace(@"\", "#").Replace("#DavWWWRoot", "").TrimEnd('#') : webDavPath.Replace(@"\", "#").Replace("#DavWWWRoot", "");

                                string strDriveLetter = d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + ":";

                                psCommand = String.Format(singleDrivePowershellCommand, strDriveLetter, webDavPath, d.DriveName, regKey, webDavPath2);
                                LogManager.Verbose("PS: " + String.Format(singleDrivePowershellCommand, d.DriveLetter, webDavPath, d.DriveName, regKey, webDavPath2));
                            }

                            //If the drive is changed by company admin and marked as to be removed, which means it needs to be unmapped
                            else if (d.Drivestate == driveState.Deleted)
                            {
                                string strDriveLetter = d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + ":";
                                psCommand = String.Format(singleDriveunmapPowershellCommand, strDriveLetter);
                                LogManager.Verbose("PS: " + String.Format(singleDriveunmapPowershellCommand, d.DriveLetter));
                            }
                            powerShell.Append(psCommand);
                        }
                    }
                    else
                    {
                        LogManager.Verbose(d.DriveLetter + " already exist, hence skipping the create drive but repairing...");

                        //Repair names (Some times it is buggy so lets repair them)
                        //Skip deleted ones
                        //       if (d.Drivestate != driveState.Deleted)
                        //     {
                        LogManager.Verbose("Its found that user has access OR drive is to be removed, continueing with mapping drive:" + d.DriveUrl);
                        string psCommand = string.Empty;

                        //If the drive is NOT changed by company admin, which means still active. Continue as we do.
                        if (d.Drivestate == driveState.Active)
                        {
                            //Make sure if its OneDrive, we have the URL set
                            //checking for OneDrive
                            if (d.Drivetype == driveType.OneDrive && String.IsNullOrEmpty(d.DriveUrl))
                            {
                                //map drives
                                currentDispatcher.Invoke(() =>
                                {
                                    Communications.updateStatus(gettingOneDriveUrlText);
                                });

                                ///getting user drive details
                                d.DriveUrl = DriveMapper.getOneDriveUrl(rootSiteUrl, userCookies);
                            }

                            //we have to do it again because incase of OneDrive it is blank above 
                            webDavPath = d.DriveUrl.Replace("http://", "\\\\").Replace("https://", "\\\\").Replace(new Uri(d.DriveUrl).Host, new Uri(d.DriveUrl).Host + (new Uri(d.DriveUrl).Scheme == Uri.UriSchemeHttps ? "@SSL\\DavWWWRoot" : "\\DavWWWRoot")).Replace("/", "\\");

                            LogManager.Verbose("WebDav Path:" + webDavPath);
                            string regKey = webDavPath.Replace(@"\", "#").EndsWith("#") ? webDavPath.Replace(@"\", "#").TrimEnd('#') : webDavPath.Replace(@"\", "#");
                            string webDavPath2 = webDavPath.Replace(@"\", "#").Replace("#DavWWWRoot", "").EndsWith("#") ? webDavPath.Replace(@"\", "#").Replace("#DavWWWRoot", "").TrimEnd('#') : webDavPath.Replace(@"\", "#").Replace("#DavWWWRoot", "");

                            string strDriveLetter = d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + ":";

                            psCommand = String.Format(singleRepairDrivePowershellCommand, strDriveLetter, d.DriveName, regKey, webDavPath2);
                            LogManager.Verbose("PS: " + String.Format(singleRepairDrivePowershellCommand, d.DriveLetter, d.DriveName, regKey, webDavPath2));
                        }
                        //If the drive is changed by company admin and marked as to be removed, which means it needs to be unmapped
                        else if (d.Drivestate == driveState.Deleted)
                        {
                            if (isDriveExists(d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + @":\", webDavPath))
                            {
                                string strDriveLetter = d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + ":";
                                psCommand = String.Format(singleDriveunmapPowershellCommand, strDriveLetter);
                                LogManager.Verbose("PS: " + String.Format(singleDriveunmapPowershellCommand, d.DriveLetter));
                                anyDelete = true;
                            }
                        }
                        powerShell.Append(psCommand);
                        //}
                    }
                }

                //if we have any delete to be made, restart the explorer in the end as sometime due to explorer bug, the drive which is deleted shown as disconnected or cross mark..
                if (anyDelete)
                {
                    powerShell.Append(restartExplorer);
                }

                LogManager.Verbose("Full command: " + powerShell.ToString());

                //invoke powershell ALL in
                RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();
                using (Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration))
                {
                    runspace.Open();
                    RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);
                    string psCommand = powerShell.ToString();
                    scriptInvoker.Invoke(psCommand);
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }



        /// <summary>
        /// check whether the drive already exist, if yes dont touch it
        /// </summary>
        /// <param name="driveLetterWithColonAndSlash"></param>
        /// <returns></returns>
        static bool isDriveExists(string driveLetterWithColonAndSlash, string webDavPath)
        {
            try
            {
                if (!Utility.ready())
                    return false;
                DriveInfo di = DriveInfo.GetDrives().Where(x => x.Name == driveLetterWithColonAndSlash).FirstOrDefault();
                if (di != null)
                {
                    try
                    {
                        string strPath = FindUNCPaths(di);
                        if (webDavPath == string.Empty || strPath.TrimEnd('\\').ToLower() == webDavPath.TrimEnd('\\').ToLower())
                            return true;
                        else
                            return false;
                    }
                    catch { return true; }
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return false;
            }
        }

        public static string FindUNCPaths(DriveInfo di)
        {
            try
            {
                if (!Utility.ready())
                    return string.Empty;
                DriveInfo[] dis = DriveInfo.GetDrives();
                //foreach (DriveInfo di in dis)
                //{
                //    if (di.DriveType == DriveType.Network)
                //    {
                DirectoryInfo dir = di.RootDirectory;
                // "x:"
                //MessageBox.Show(GetUNCPath(dir.FullName.Substring(0, 2)));
                return GetUNCPath(dir.FullName.Substring(0, 2));
                //    }
                //}
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return string.Empty;
            }
        }

        public static string GetUNCPath(string path)
        {
            try
            {
                if (!Utility.ready())
                    return string.Empty;
                if (path.StartsWith(@"\\"))
                {
                    return path;
                }

                ManagementObject mo = new ManagementObject();
                mo.Path = new ManagementPath(String.Format("Win32_LogicalDisk='{0}'", path));

                // DriveType 4 = Network Drive
                if (Convert.ToUInt32(mo["DriveType"]) == 4)
                {
                    return Convert.ToString(mo["ProviderName"]);
                }
                else
                {
                    return path;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Incase of exit, we need to remove cookies so noone else can use it
        /// </summary>
        public static void clearCookies()
        {
            try
            {
                if (!Utility.ready())
                    return;

                DriveManager.deleteCookiesFromIE(string.Empty, string.Empty, DriveManager.rootSiteUrl);
                DriveManager.deleteCookiesFromIE(string.Empty, string.Empty, DriveManager.oneDriveHostSiteUrl);
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }

        /// <summary>
        /// Unmap all drives upon exit
        /// </summary>
        public static void unmapAllDrives()
        {
            try
            {
                if (!Utility.ready())
                    return;

                LogManager.Verbose("Unmapping all drives");
                //{0}=Letter,{1}=Path,{2}=Name
                string singleDriveunmapPowershellCommand = "Try{{If ((Test-Path " + "{0}" + ")) {{(New-Object -Com WScript.Network).RemoveNetworkDrive(\"" + "{0}" + "\",$true,$true);Remove-PSDrive " + "{0}" + " -Force;}};}}Catch{{}}";
                //string restartExplorer = ";Stop-Process -processName: Explorer;";
                StringBuilder powerShell = new StringBuilder();

                //loop through each drive and make a complete powershell command
                foreach (Drive d in mappableDrives)
                {

                    string strDriveLetter = d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + ":";
                    string psCommand = String.Format(singleDriveunmapPowershellCommand, strDriveLetter);
                    powerShell.Append(psCommand);
                    LogManager.Verbose("PS: " + String.Format(singleDriveunmapPowershellCommand, d.DriveLetter));
                }

                LogManager.Verbose("Full command: " + powerShell.ToString());

                //refresh the desktop
                //powerShell.Append(restartExplorer);

                //invoke powershell ALL in
                RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();
                using (Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration))
                {
                    runspace.Open();
                    RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);
                    scriptInvoker.Invoke(powerShell.ToString());
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
        }
    }


    /// <summary>
    /// Class for mappable Drive
    /// </summary>
    public class Drive
    {
        public string DriveLetter { get; set; }
        public string DriveName { get; set; }
        public string DriveUrl { get; set; }
        public driveState Drivestate { get; set; }
        public driveType Drivetype { get; set; }
    }

    public enum driveState
    {
        Active = 0,
        Deleted = 1
    }

    public enum driveType
    {
        OneDrive = 0,
        SharePoint = 1,
        DocLib = 2
    }
}
