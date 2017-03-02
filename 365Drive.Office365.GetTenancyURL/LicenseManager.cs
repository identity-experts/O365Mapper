using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.CloudConnector
{
    public static class LicenseManager
    {
        public static LicenseValidationState isLicenseValid(string tenancyName)
        {
            LicenseValidationState licenseState = LicenseValidationState.Ok;
            try
            {
                if (!Utility.ready())
                    return LicenseValidationState.CouldNotVerify;

                //Call to license management API goes here.
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
        public static void populateDrives()
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
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }


        }
    }
}
