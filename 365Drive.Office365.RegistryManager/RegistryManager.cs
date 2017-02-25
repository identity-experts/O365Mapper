using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        BalloonNotification = 1
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
            add.SetValue(Constants.regStartupAppName, "\"" + AppDomain.CurrentDomain.BaseDirectory + "\"" + Constants.exeName + "\"");
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
