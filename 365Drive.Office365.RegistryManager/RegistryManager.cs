using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365
{

    /// <summary>
    /// Following are the registry KEYs which will be used for saving configuration values for 365Drive
    /// </summary>
    public enum RegistryKeys : int
    {
        Verbose = 0,
        BalloonNotification = 1,
        TenancyName = 2,
        RootSiteUrl = 3,
        MySiteUrl = 4,
        LastLicenseChecked = 5,
        PartnerLogo = 6,
        SSO = 7,
        AutoSSO = 8,
        DontAskForUpdates = 9
    }


    public static class RegistryManager
    {

        /// <summary>
        /// Boolean returning true if the the executing exe is running inside debug folders
        /// </summary>
        public static bool IsDev
        {
            get
            {
                string EXEpath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (EXEpath.ToLower().Contains(@"\debug\"))
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Get the key value from registry
        /// </summary>
        /// <param name="key"></param>
        public static string Get(RegistryKeys key)
        {
            string value = string.Empty;
            RegistryKey myKey = Registry.CurrentUser.OpenSubKey(Constants.registryRoot, RegistryKeyPermissionCheck.ReadSubTree);
            if (myKey != null)
            {
                value = Convert.ToString(myKey.GetValue((string)key.ToString()));
            }
            else
            {
                return null;
            }
            return value;
        }

        /// <summary>
        /// Get the key value from registry
        /// </summary>
        /// <param name="key"></param>
        public static string Delete(RegistryKeys key)
        {
            string value = string.Empty;
            RegistryKey myKey = Registry.CurrentUser.OpenSubKey(Constants.registryRoot, RegistryKeyPermissionCheck.ReadSubTree);
            if (myKey != null)
            {
                myKey.SetValue((string)key.ToString(), string.Empty);
            }
            else
            {
                return null;
            }
            return value;
        }

        /// <summary>
        /// Get the key value from registry
        /// </summary>
        /// <param name="key"></param>
        public static NameValueCollection GetMappingDetails()
        {
            string value = string.Empty;
            NameValueCollection mappings = new NameValueCollection();
            RegistryKey myKey = Registry.CurrentUser.OpenSubKey(Constants.registryRoot + @"\" + Constants.mappingregKey, RegistryKeyPermissionCheck.ReadSubTree);
            if (myKey != null)
            {
                foreach (string subKeyName in myKey.GetValueNames())
                {
                    if (subKeyName.Length == 1)
                    {
                        string val = myKey.GetValue(subKeyName).ToString();
                        mappings.Add(subKeyName, val);
                    }
                }
            }
            else
            {
                return null;
            }
            return mappings;
        }

        /// <summary>
        /// Set the value to registry
        /// </summary>
        /// <param name="key">enum for key</param>
        /// <param name="value">the value to set</param>
        public static bool Set(RegistryKeys key, string value)
        {
            ///Since we are setting the values, its quite possible that the registry is NOT touched at all (first time) so we need to make sure
            /// we create the key set

            RegistryKey myKey = Registry.CurrentUser.OpenSubKey(Constants.registryRoot, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (myKey == null)
            {
                Registry.CurrentUser.CreateSubKey(Constants.registryRoot);
                myKey = Registry.CurrentUser.OpenSubKey(Constants.registryRoot, RegistryKeyPermissionCheck.ReadWriteSubTree);
            }
            try
            {
                myKey.SetValue((string)key.ToString(), value);
            }
            catch (Exception ex) { return false; }
            return true;
        }


        /// <summary>
        /// Register current exe on startup
        /// </summary>
        public static void RegisterExeOnStartup()
        {

            ///Get the exe folder path
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            RegistryKey add = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            add.SetValue(Constants.regStartupAppName, "\"" + AppDomain.CurrentDomain.BaseDirectory + Constants.exeName + "\"");
        }

        /// <summary>
        /// incase if its Dev environment, we can use below method to set hardcoded values.
        /// </summary>
        public static void DeleteAllRegistry()
        {

            Set(RegistryKeys.Verbose, string.Empty);
            Set(RegistryKeys.BalloonNotification, string.Empty);
            Set(RegistryKeys.TenancyName, string.Empty);
            Set(RegistryKeys.RootSiteUrl, string.Empty);
            Set(RegistryKeys.MySiteUrl, string.Empty);
            Set(RegistryKeys.LastLicenseChecked, string.Empty);
            Set(RegistryKeys.PartnerLogo, string.Empty);
            Set(RegistryKeys.SSO, string.Empty);
            Set(RegistryKeys.AutoSSO, string.Empty);
        }

        /// <summary>
        /// Admin settings (One time)
        /// </summary>
        public static void ConfigRegistry()
        {

            try
            {
                //set file size limit
                RegistryKey fileUploadLimitKey = Registry.LocalMachine.OpenSubKey(Constants.fileUploadLimitKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                fileUploadLimitKey.SetValue("FileSizeLimitInBytes", unchecked((int)0xffffffff), RegistryValueKind.DWord);

                //set file size limit
                RegistryKey enableLinkedConnectionsKey = Registry.LocalMachine.OpenSubKey(Constants.enableLinkedConnectionsKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                enableLinkedConnectionsKey.SetValue("EnableLinkedConnections", unchecked((int)0x00000001), RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                //ignore
            }

        }

        /// <summary>
        /// incase if its Dev environment, we can use below method to set hardcoded values.
        /// </summary>
        public static void SetDevEnvironmnet()
        {

            if (IsDev)
            {
                Set(RegistryKeys.Verbose, "1");
                Set(RegistryKeys.BalloonNotification, "1");
            }

        }
    }
}
