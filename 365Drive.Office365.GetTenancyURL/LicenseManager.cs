using _365Drive.Office365.GetTenancyURL;
using _365Drive.Office365.GetTenancyURL.LicenseHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;

namespace _365Drive.Office365.CloudConnector
{

    public class DriveMappingDetailsResult
    {
        public bool success { get; set; }
        public List<DriveMappingDetail> data { get; set; }
    }

    public class DriveMappingDetail
    {
        public string drive_deleted { get; set; }
        public string drive_label { get; set; }
        public string drive_letter { get; set; }
        public string drive_url { get; set; }
        public string office365_tenancy_name { get; set; }
        public string subscription_id { get; set; }
        public string user_id { get; set; }
    }



    public class LicenseUserMappingResult
    {
        public bool success { get; set; }
        public LicenseUserMappingResultData data { get; set; }

    }

    public class LicenseUserMappingResultData
    {
        public string email { get; set; }
        public string key { get; set; }
        public bool alreadyactivated { get; set; }
        public bool tenancynotexist { get; set; }
        public bool licenseexcceded { get; set; }
        public string partner_about { get; set; }
        public string partner_name { get; set; }
        public string partner_logo { get; set; }
    }

    public class LicenseCheckResult
    {
        public string status_check { get; set; }
        public string activated { get; set; }
        public string code { get; set; }
        public string error { get; set; }
    }

    /// <summary>
    /// Example : //{"activated":true,"instance":"6EAD-7486-3357-CFAA-F33C-8FA1-1C46-1CEC","activation_extra":59,"message":"4 out of 5 activations remaining","timestamp":1490160837}
    /// </summary>
    public class ActivationResult
    {
        public string activated { get; set; }
        public string message { get; set; }

        //incase of error
        public string error { get; set; }
        public string code { get; set; }
        public string debug { get; set; }
    }


    public static class LicenseManager
    {

        #region partner specific properties
        public static string partnerName { get; set; }
        public static string partnerLogo { get; set; }

        public static BitmapImage partnerLogoBM
        {
            get
            {
                //if (!string.IsNullOrEmpty(LicenseManager.partnerLogo))
                //{
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(LicenseManager.partnerLogo, UriKind.Absolute);
                bitmap.EndInit();
                return bitmap;
                //}
            }
        }

        public static string partnerAbout { get; set; }
        public static string partnerAboutPlainText
        {
            get { return Regex.Replace(partnerAbout, "<.*?>", String.Empty); }
        }
        public static bool isitPartnerManaged
        {
            get
            {
                if (string.IsNullOrEmpty(partnerName))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        #endregion
        /// <summary>
        /// This bool will be used for making sure whether we need to directly prompt for MFA or not
        /// </summary>
        public static bool hasPasswordChangedOrFirstTime { get; set; }

        /// <summary>
        /// THis will be true when user will say YES for MFA. We will keep it true for both Tenancy Name and auth cookies
        /// </summary>
        public static bool MFAConsent { get; set; }

        public static string lastActivationMessage { get; set; }


        /// <summary>
        /// Last checked time
        /// </summary>
        public static DateTime? lastLicenseChecked { get; set; }


        /// <summary>
        /// Last concent Asked
        /// </summary>
        public static DateTime? lastConsentGranted { get; set; }

        public static bool licenseCheckTimeNow
        {
            get
            {
                if (lastLicenseChecked == null)
                {
                    return true;
                }
                else
                {
                    TimeSpan diff = DateTime.Now - Convert.ToDateTime(lastLicenseChecked);
                    double hours = diff.TotalHours;
                    if (hours < Constants.localLicenseCheckLimit)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }



        /// <summary>
        /// Make sure whether we need to askfor user consent
        /// </summary>
        public static bool MFAConcentRequired
        {
            get
            {
                bool consentRequired = true;

                bool consentTimedOut = false;
                if (lastConsentGranted != null)
                {
                    TimeSpan diff = DateTime.Now - Convert.ToDateTime(lastConsentGranted);
                    double hours = diff.TotalHours;
                    if (hours >= Constants.ConsentGrantLimit)
                    {
                        consentTimedOut = true;
                    }
                }
                else
                {
                    consentTimedOut = true;
                }
                //if this is first time user typed in credential, they would deffo expect the MFA so no need for user consent. And finally set it off.
                if (hasPasswordChangedOrFirstTime)
                {
                    consentRequired = false;
                    hasPasswordChangedOrFirstTime = false;
                }
                else if(!consentTimedOut)
                {
                    consentRequired = false;
                }

                //if during tenancy name fetching user has already said yes, there is NO need to ask for it again in cookies get. However set it off next time.
                if (MFAConsent)
                {
                    consentRequired = false;
                    MFAConsent = false;
                }
                return consentRequired;
            }
        }



        public static LicenseValidationState? lastLicenseState
        {
            get; set;
        }

        /// <summary>
        /// Last checked time
        /// </summary>
        public static DateTime? lastDriveFetched { get; set; }

        public static bool driveFetchTimeNow
        {
            get
            {
                if (lastDriveFetched == null)
                {
                    return true;
                }
                else
                {
                    TimeSpan diff = DateTime.Now - Convert.ToDateTime(lastDriveFetched);
                    double hours = diff.TotalHours;
                    if (hours < Constants.localDriveFetchLimit)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }


        public static LicenseValidationState isLicenseValid(string tenancyName, string userName, CookieContainer userCookies)
        {


            LicenseValidationState licenseState = LicenseValidationState.Ok;
            try
            {
                if (!Utility.ready())
                    return LicenseValidationState.CouldNotVerify;

                ///Get the license key, email and status of license for given user
                string licensingusermappingUrl = String.Format(Constants.licenseuserMappingUrl, Constants.licensingBaseDomain, Constants.ieUserMappingApiCode, tenancyName, userName);
                LogManager.Verbose("Get the details about license : " + licensingusermappingUrl);

                //poll end cookie container
                CookieContainer userMappingCookieContainer = new CookieContainer();
                string fedAuth = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["fedAuth"].Value;
                string rtFA = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))["rtfa"].Value;
                userMappingCookieContainer.Add(new Uri("http://" + new Uri(Constants.licensingBaseDomain).Authority), new Cookie("FedAuth", encode(fedAuth)));
                userMappingCookieContainer.Add(new Uri("http://" + new Uri(Constants.licensingBaseDomain).Authority), new Cookie("rtFA", encode(rtFA)));


                //get the initial license details
                Task<string> call1Result = HttpClientHelper.GetAsync(licensingusermappingUrl, userMappingCookieContainer);
                call1Result.Wait();

                string licenseUserMappingResult = call1Result.Result;


                LicenseUserMappingResult userLicenseMapresult = JsonConvert.DeserializeObject<LicenseUserMappingResult>(licenseUserMappingResult);

                ///set the last checked time 
                lastLicenseChecked = DateTime.Now;

                //if we get the key, proceed or check for why its failed
                if (userLicenseMapresult.success)
                {
                    //check partner information
                    if (userLicenseMapresult.data.partner_name != null && !string.IsNullOrEmpty(userLicenseMapresult.data.partner_name.Trim()))
                    {
                        partnerName = userLicenseMapresult.data.partner_name;
                        partnerLogo = userLicenseMapresult.data.partner_logo;
                        partnerAbout = userLicenseMapresult.data.partner_about;

                        //set to registry ONLY for notifications
                        RegistryManager.Set(RegistryKeys.PartnerLogo, userLicenseMapresult.data.partner_logo);
                    }

                    string uniqueMachineID = ThumbPrint.Value();
                    string licenseStatusUrl = String.Format(Constants.statusCheckUrl, Constants.licensingBaseDomain, Constants.activationApiCode, encode(userLicenseMapresult.data.email), userLicenseMapresult.data.key, Constants.statusRequestName, encode(Constants.ie365MapperProductName), uniqueMachineID, encode(userName), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    //check the license status 
                    LogManager.Verbose("Checking license status url (html encoded): " + licenseStatusUrl);

                    //get the initial license details
                    Task<string> licensestatusCheckresult = HttpClientHelper.GetAsync(licenseStatusUrl);
                    licensestatusCheckresult.Wait();

                    LicenseCheckResult licenseCheckResult = JsonConvert.DeserializeObject<LicenseCheckResult>(licensestatusCheckresult.Result);

                    if (licenseCheckResult.status_check == "inactive")
                    {
                        string licenseActivationUrl = String.Format(Constants.activationUrl, Constants.licensingBaseDomain, Constants.activationApiCode, encode(userLicenseMapresult.data.email), userLicenseMapresult.data.key, Constants.activationRequestName, encode(Constants.ie365MapperProductName), uniqueMachineID, encode(userName), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), tenancyName);
                        //check the license status 
                        LogManager.Verbose("Activating license status url (html encoded): " + licenseActivationUrl);

                        //get the initial license details
                        Task<string> licenseActivationCheckresult = HttpClientHelper.GetAsync(licenseActivationUrl);
                        licenseActivationCheckresult.Wait();

                        //activation result 
                        ActivationResult activationResult = JsonConvert.DeserializeObject<ActivationResult>(licenseActivationCheckresult.Result);
                        if (activationResult.activated == "true")
                        {
                            lastActivationMessage = activationResult.message;
                            RegistryManager.Set(RegistryKeys.LastLicenseChecked, DateTime.Now.Ticks.ToString());
                            return LicenseValidationState.ActivatedFirstTime;
                        }
                        else
                        {
                            LogManager.Verbose("Activation failed: " + activationResult.code + ":" + activationResult.debug + ", " + activationResult.error);
                            lastActivationMessage = activationResult.code + ":" + activationResult.error;
                            return LicenseValidationState.ActivationFailed;
                        }
                    }
                    else if (licenseCheckResult.activated == "inactive" && !string.IsNullOrEmpty(licenseCheckResult.error))
                    {
                        lastActivationMessage = licenseCheckResult.code + ":" + licenseCheckResult.error;
                        return LicenseValidationState.ActivationFailed;
                    }
                    else
                    {
                        //Already activated
                        RegistryManager.Set(RegistryKeys.LastLicenseChecked, DateTime.Now.Ticks.ToString());
                        return LicenseValidationState.Ok;
                    }
                }
                else
                {
                    if (userLicenseMapresult.data.tenancynotexist)
                    {
                        return LicenseValidationState.TenancyNotExist;
                    }
                    else if (userLicenseMapresult.data.licenseexcceded)
                    {
                        return LicenseValidationState.Exceeded;
                    }
                    else
                    {
                        return LicenseValidationState.LoginFailed;
                    }
                }

                //finally rturn ok
                licenseState = LicenseValidationState.Ok;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);

                //Only check after every fixed hours
                var lastLicensechecked = RegistryManager.Get(RegistryKeys.LastLicenseChecked);
                if (lastLicensechecked != null)
                {
                    long lastChecked = 0;
                    if (long.TryParse(lastLicensechecked, out lastChecked))
                    {
                        DateTime LicenseChecked = new DateTime(lastChecked);
                        TimeSpan diff = DateTime.Now - LicenseChecked;
                        double hours = diff.TotalHours;
                        if (hours < Constants.licenseCheckInterval)
                            return LicenseValidationState.Ok;
                        else
                            return LicenseValidationState.CouldNotVerify;
                    }
                }
            }
            return licenseState;
        }

        /// <summary>
        /// Encode the given string
        /// </summary>
        /// <param name="encodeString"></param>
        /// <returns></returns>
        public static string encode(string encodeString)
        {
            return WebUtility.UrlEncode(encodeString);
        }


        /// <summary>
        /// decode given string
        /// </summary>
        /// <param name="decodeString"></param>
        /// <returns></returns>
        public static string decode(string decodeString)
        {
            return WebUtility.UrlDecode(decodeString);
        }

        /// <summary>
        /// as per the input given by user, identify what is it
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static driveType getDriveType(string input)
        {
            string Input = input.Replace("[", string.Empty).Replace("]", string.Empty).ToLower();
            if (Constants.oneDriveIdentifier.Contains(Input))
            {
                return driveType.OneDrive;
            }
            else if (Constants.spDefaultDriveIdentifier.Contains(Input))
            {
                return driveType.SharePoint;
            }
            else
            {
                return driveType.DocLib;
            }

        }

        /// <summary>
        /// get the state of drive
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static driveState getDriveState(string input)
        {
            string Input = input.Replace("[", string.Empty).Replace("]", string.Empty).ToLower();
            if (Constants.deleteDriveIdentifier.Contains(Input))
            {
                return driveState.Deleted;
            }
            else
            {
                return driveState.Active;
            }

        }

        /// <summary>
        /// three possibilities to be checked. First is regisry, second is user's profile and third is tenancy level, finally default two drives
        /// </summary>
        public static void populateDrives(CookieContainer userCookies)
        {
            try
            {
                if (!Utility.ready())
                    return;

                //Priority 1: We need to check wheter there is any value in mappings registry key
                var regMapValues = RegistryManager.GetMappingDetails();
                if (regMapValues != null && regMapValues.Count > 0)
                {
                    foreach (string strkey in regMapValues.Keys)
                    {
                        //string[] values = regMapValues.GetValues(strkey);
                        string mappingvalue = regMapValues[strkey];
                        if (mappingvalue != null)
                        {
                            string[] mappingdetail = mappingvalue.Split(';');
                            if (mappingdetail.Length >= 2)
                            {
                                string driveLetter = strkey;
                                string driveUrl = mappingdetail[0];
                                string driveName = mappingdetail[1];
                                driveState driveState = driveState.Active;

                                if (mappingdetail.Length > 2)
                                {
                                    string deleteFlag = mappingdetail[1];
                                    driveState = getDriveState(mappingdetail[2]);
                                }

                                driveType driveType = getDriveType(driveUrl);
                                if (driveType == driveType.DocLib)
                                {
                                    Uri uriResult;
                                    bool isValidUrl = Uri.TryCreate(driveUrl, UriKind.Absolute, out uriResult)
                                        && uriResult.Scheme == Uri.UriSchemeHttps;
                                    if (isValidUrl)
                                    {
                                        DriveManager.addDrive(driveLetter, driveName, uriResult.ToString(), driveState, driveType);
                                    }
                                }
                                else if (driveType == driveType.OneDrive)
                                {
                                    DriveManager.addDrive(driveLetter, driveName, string.Empty, driveState, driveType);
                                }
                                else if (driveType == driveType.SharePoint)
                                {
                                    string rootSitedocLibUrl = DriveManager.rootSiteUrl.EndsWith("/") ? DriveManager.rootSiteUrl + "Shared Documents" : DriveManager.rootSiteUrl + "/Shared Documents";
                                    DriveManager.addDrive(driveLetter, driveName, rootSitedocLibUrl, driveState, driveType);
                                }
                            }
                            else
                            {
                                //notify user about problem
                                CommunicationManager.Communications.queueNotification("Drive Configuration", "Unable to get drive settings for drive '" + strkey + "'. Settings are not properly configured.");
                            }
                        }

                    }
                    if (DriveManager.mappableDrives == null || DriveManager.mappableDrives.Count == 0)
                    {
                        addDefaultDrives();
                    }
                }
                //Prio 2:
                //Prio 3: from portal
                //Last: Get value from users' profile and Priority 3: get from app value
                else
                {

                    string strUser = _365Drive.Office365.CredentialManager.GetCredential().UserName;

                    if (!string.IsNullOrEmpty(_365DriveTenancyURL.exceptionDomain(strUser)))
                    {
                        if (_365DriveTenancyURL.exceptionDomain(strUser).ToLower() == "sharepoint.onmicrosoft.com")
                        {
                            LogManager.Verbose("Adding default microsoft drives (hard coded)");
                            DriveManager.addDrive("I", "India Learning", "https://microsoft.sharepoint.com/sites/infopedia/indialearning/Documents");
                            DriveManager.addDrive("T", "ITWeb", "https://microsoft.sharepoint.com/sites/itweb/Documents");
                            DriveManager.addDrive("M", "MSW", "https://microsoft.sharepoint.com/sites/msw/documents");
                            DriveManager.addDrive("N", "Dining", "https://microsoft.sharepoint.com/sites/refweb/na/Redmond/dining/Documents");
                            DriveManager.addDrive("H", "Sharepoint Hosting Options", "https://microsoft.sharepoint.com/sites/SharePoint/Documents");

                            DriveManager.addDrive("O", "OneDrive for Business", string.Empty, driveType.OneDrive);

                        }
                        else
                        {
                            addDefaultDrives();
                        }
                    }
                    else
                    {
                        //Call to Azure AD, user profile and registry to fetch and populate drive
                        addDefaultDrives();
                    }
                    lastDriveFetched = DateTime.Now;
                }
              
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }


        }


        public static void addDefaultDrives()
        {
            //for now hardcoding
            string rootSitedocLibUrl = DriveManager.rootSiteUrl.EndsWith("/") ? DriveManager.rootSiteUrl + "Shared Documents" : DriveManager.rootSiteUrl + "/Shared Documents";

            //Add this to mappable drives
            LogManager.Verbose("Adding default site to mappable drive. Url: " + rootSitedocLibUrl);
            DriveManager.addDrive("S", "SharePoint", rootSitedocLibUrl);
            DriveManager.addDrive("O", "OneDrive for Business", string.Empty, driveType.OneDrive);

            //ONLY FOR TESTING HARDCODED VALUE
            //DriveManager.addDrive("M", "Map2", "https://gloiretech.sharepoint.com/Map2");
        }
    }
}
