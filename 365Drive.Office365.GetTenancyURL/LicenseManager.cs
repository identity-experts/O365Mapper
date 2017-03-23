using _365Drive.Office365.GetTenancyURL;
using _365Drive.Office365.GetTenancyURL.LicenseHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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
        public bool activated { get; set; }
        public string message { get; set; }

        //incase of error
        public string error { get; set; }
        public string code { get; set; }
        public string debug { get; set; }
    }


    public static class LicenseManager
    {
        public static string lastActivationMessage { get; set; }

        public static LicenseValidationState isLicenseValid(string tenancyName, string userName, CookieContainer userCookies)
        {


            LicenseValidationState licenseState = LicenseValidationState.Ok;
            try
            {
                if (!Utility.ready())
                    return LicenseValidationState.CouldNotVerify;

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
                    }
                }

                ///Get the license key, email and status of license for given user
                string licensingusermappingUrl = String.Format(Constants.licenseuserMappingUrl, Constants.licensingBaseDomain, Constants.ieUserMappingApiCode, tenancyName, userName);
                LogManager.Verbose("Get the details about license : " + licensingusermappingUrl);

                //poll end cookie container
                CookieContainer userMappingCookieContainer = new CookieContainer();
                string fedAuth = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))[0].Value;
                string rtFA = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))[1].Value;
                userMappingCookieContainer.Add(new Uri("http://" + new Uri(Constants.licensingBaseDomain).Authority), new Cookie("FedAuth", encode(fedAuth)));
                userMappingCookieContainer.Add(new Uri("http://" + new Uri(Constants.licensingBaseDomain).Authority), new Cookie("rtFA", encode(rtFA)));


                //get the initial license details
                Task<string> call1Result = HttpClientHelper.GetAsync(licensingusermappingUrl, userMappingCookieContainer);
                call1Result.Wait();

                string licenseUserMappingResult = call1Result.Result;

                LicenseUserMappingResult userLicenseMapresult = JsonConvert.DeserializeObject<LicenseUserMappingResult>(licenseUserMappingResult);

                //if we get the key, proceed or check for why its failed
                if (userLicenseMapresult.success)
                {
                    string uniqueMachineID = ThumbPrint.Value();
                    string licenseStatusUrl = String.Format(Constants.activationUrl, Constants.licensingBaseDomain, Constants.activationApiCode, encode(userLicenseMapresult.data.email), userLicenseMapresult.data.key, Constants.statusRequestName, encode(Constants.ie365MapperProductName), uniqueMachineID, encode(userName), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    //check the license status 
                    LogManager.Verbose("Checking license status url (html encoded): " + licenseStatusUrl);

                    //get the initial license details
                    Task<string> licensestatusCheckresult = HttpClientHelper.GetAsync(licenseStatusUrl);
                    licensestatusCheckresult.Wait();

                    LicenseCheckResult licenseCheckResult = JsonConvert.DeserializeObject<LicenseCheckResult>(licensestatusCheckresult.Result);
                    if (licenseCheckResult.status_check == "inactive")
                    {
                        string licenseActivationUrl = String.Format(Constants.activationUrl, Constants.licensingBaseDomain, Constants.activationApiCode, encode(userLicenseMapresult.data.email), userLicenseMapresult.data.key, Constants.activationRequestName, encode(Constants.ie365MapperProductName), uniqueMachineID, encode(userName), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                        //check the license status 
                        LogManager.Verbose("Activating license status url (html encoded): " + licenseActivationUrl);

                        //get the initial license details
                        Task<string> licenseActivationCheckresult = HttpClientHelper.GetAsync(licenseActivationUrl);
                        licenseActivationCheckresult.Wait();

                        //activation result 
                        ActivationResult activationResult = JsonConvert.DeserializeObject<ActivationResult>(licenseActivationCheckresult.Result);
                        if (activationResult.activated)
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
            }
            return licenseState;
        }

        /// <summary>
        /// Encode the given string
        /// </summary>
        /// <param name="encodeString"></param>
        /// <returns></returns>
        static string encode(string encodeString)
        {
            return WebUtility.UrlEncode(encodeString);
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
                        }

                    }
                }
                //Prio 2:
                //Prio 3:
                else
                {
                    ///Get the license key, email and status of license for given user

                    string tenancyName = RegistryManager.Get(RegistryKeys.TenancyName);
                    string licensingusermappingUrl = String.Format(Constants.retrieveDriveMappingsUrl, Constants.licensingBaseDomain, Constants.ieDriveDetailsApiCode, tenancyName);
                    LogManager.Verbose("Get the details about license : " + licensingusermappingUrl);

                    //poll end cookie container
                    CookieContainer userMappingCookieContainer = new CookieContainer();
                    string fedAuth = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))[0].Value;
                    string rtFA = userCookies.GetCookies(new Uri(DriveManager.rootSiteUrl))[1].Value;
                    userMappingCookieContainer.Add(new Uri("http://" + new Uri(Constants.licensingBaseDomain).Authority), new Cookie("FedAuth", encode(fedAuth)));
                    userMappingCookieContainer.Add(new Uri("http://" + new Uri(Constants.licensingBaseDomain).Authority), new Cookie("rtFA", encode(rtFA)));


                    //get the initial license details
                    Task<string> call1Result = HttpClientHelper.GetAsync(licensingusermappingUrl, userMappingCookieContainer);
                    call1Result.Wait();

                    string licenseUserMappingResult = call1Result.Result;

                    DriveMappingDetailsResult userLicenseMapresult = JsonConvert.DeserializeObject<DriveMappingDetailsResult>(licenseUserMappingResult);
                    if (userLicenseMapresult.data.Count > 0)
                    {
                        foreach (DriveMappingDetail drive in userLicenseMapresult.data)
                        {
                            string driveLetter = drive.drive_letter;
                            string driveUrl = drive.drive_url;
                            string driveName = drive.drive_label;
                            driveState driveState = driveState.Active;
                            if (drive.drive_deleted == "1")
                                driveState = driveState.Deleted;
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
                    }

                    //Last: Get value from users' profile and Priority 3: get from app value
                    else
                    {
                        //Call to Azure AD, user profile and registry to fetch and populate drive

                        //for now hardcoding
                        string rootSitedocLibUrl = DriveManager.rootSiteUrl.EndsWith("/") ? DriveManager.rootSiteUrl + "Shared Documents" : DriveManager.rootSiteUrl + "/Shared Documents";

                        //Add this to mappable drives
                        LogManager.Verbose("Adding default site to mappable drive. Url: " + rootSitedocLibUrl);
                        DriveManager.addDrive("S", "SharePoint", rootSitedocLibUrl);
                        DriveManager.addDrive("O", "OneDrive", string.Empty, driveType.OneDrive);

                        //ONLY FOR TESTING HARDCODED VALUE
                        //DriveManager.addDrive("M", "Map2", "https://gloiretech.sharepoint.com/Map2");
                    }
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }


        }
    }
}
