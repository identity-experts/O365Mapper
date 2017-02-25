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

        public static void populateDrives()
        {
            try
            {
                if (!Utility.ready())
                    return;
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
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }


        }
    }
}
