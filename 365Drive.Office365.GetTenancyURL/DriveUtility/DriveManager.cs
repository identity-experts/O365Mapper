using _365Drive.Office365.CommunicationManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace _365Drive.Office365.CloudConnector
{
    public static class DriveManager
    {
        #region to set cookies to ie
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetCookie(string lpszUrlName, string lbszCookieName, string lpszCookieData);
        [DllImport("wininet.dll", SetLastError = true)]
        public static extern bool InternetGetCookie(string lpszUrl, string lpszCookieName, StringBuilder lpszCookieData, ref int lpdwSize);

        [DllImport("kernel32.dll")]
        #endregion
        public static extern uint GetLastError();

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
        /// Add a new drive to mappable drive
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <param name="driveName"></param>
        /// <param name="driveUrl"></param>
        public static void addDrive(string driveLetter, string driveName, string driveUrl)
        {
            if (mappableDrives == null)
                mappableDrives = new List<Drive>();
            LogManager.Verbose("Adding a new drive to mappablearray: " + driveUrl);
            //Create a new drive class instance and add the mappable drive
            Drive drive = new Drive();
            drive.DriveLetter = driveLetter;
            drive.DriveName = driveName;
            drive.DriveUrl = driveUrl;
            drive.Drivetype = driveType.SharePoint;
            //if no state is passed, which means its active
            drive.Drivestate = driveState.Active;
            mappableDrives.Add(drive);
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
            if (mappableDrives == null)
                mappableDrives = new List<Drive>();
            LogManager.Verbose("Adding a new drive to mappablearray: " + driveUrl);
            //Create a new drive class instance and add the mappable drive
            Drive drive = new Drive();
            drive.DriveLetter = driveLetter;
            drive.DriveName = driveName;
            drive.DriveUrl = driveUrl;
            drive.Drivestate = state;
            drive.Drivetype = driveType.SharePoint;
            mappableDrives.Add(drive);
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
            if (mappableDrives == null)
                mappableDrives = new List<Drive>();
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
                string singleDrivePowershellCommand = "Try{{If ((Test-Path " + "{0}" + ")) {{(New-Object -Com WScript.Network).RemoveNetworkDrive(\"" + "{0}" + "\",$true,$true);Remove-PSDrive " + "{0}" + " -Force;}};(New-Object -ComObject WScript.Network).MapNetworkDrive(\"" + "{0}" + "\", \"" + "{1}" + "\", $true, \"" + "\", \"" + "\"); (New-Object -ComObject Shell.Application).NameSpace(\"" + "{0}" + "\").Self.Name = \"" + "{2}" + "\";$RegKey = \"HKCU:\\software\\microsoft\\windows\\currentversion\\explorer\\mountpoints2\\{3}\";Set-ItemProperty -Path $RegKey -Name _LabelFromReg -Value \"{2}\";}}Catch{{}}";
                string singleDriveunmapPowershellCommand = "Try{{If ((Test-Path " + "{0}" + ")) {{(New-Object -Com WScript.Network).RemoveNetworkDrive(\"" + "{0}" + "\",$true,$true);Remove-PSDrive " + "{0}" + " -Force;}};}}Catch{{}}";
                StringBuilder powerShell = new StringBuilder();

                //loop through each drive and make a complete powershell command
                foreach (Drive d in mappableDrives)
                {
                    LogManager.Verbose("Ensuring user has access to:" + d.DriveUrl);
                    if (d.Drivestate == driveState.Deleted || d.Drivetype == driveType.OneDrive || DriveMapper.userHasAccess(new Uri(d.DriveUrl), userCookies))
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
                            string webDavPath = d.DriveUrl.Replace("http://", "\\\\").Replace("https://", "\\\\").Replace(new Uri(d.DriveUrl).Host, new Uri(d.DriveUrl).Host + (new Uri(d.DriveUrl).Scheme == Uri.UriSchemeHttps ? "@SSL\\DavWWWRoot" : "\\DavWWWRoot")).Replace("/", "\\");
                            LogManager.Verbose("WebDav Path:" + webDavPath);
                            string regKey = webDavPath.Replace(@"\", "#");
                            string strDriveLetter = d.DriveLetter.EndsWith(":") ? d.DriveLetter : d.DriveLetter + ":";
                            psCommand = String.Format(singleDrivePowershellCommand, strDriveLetter, webDavPath, d.DriveName, regKey);
                            LogManager.Verbose("PS: " + String.Format(singleDrivePowershellCommand, d.DriveLetter, webDavPath, d.DriveName, regKey));
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

                LogManager.Verbose("Full command: " + powerShell.ToString());

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


        /// <summary>
        /// Incase of exit, we need to remove cookies so noone else can use it
        /// </summary>
        public static void clearCookies()
        {
            try
            {
                if (!Utility.ready())
                    return;

                DriveManager.setCookiestoIE(string.Empty, string.Empty, DriveManager.rootSiteUrl);
                DriveManager.setCookiestoIE(string.Empty, string.Empty, DriveManager.oneDriveHostSiteUrl);
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
        SharePoint = 1
    }
}
