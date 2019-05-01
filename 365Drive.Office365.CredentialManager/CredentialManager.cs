using CredentialManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365
{


    public enum CredentialState
    {
        Notpresent = 0,
        Present = 1,
        ServerNotConnectable = 2
    }

    public class ADDomainManager
    {
        public static bool IsInDomain()
        {
            Win32.NetJoinStatus status = Win32.NetJoinStatus.NetSetupUnknownStatus;
            IntPtr pDomain = IntPtr.Zero;
            int result = Win32.NetGetJoinInformation(null, out pDomain, out status);
            if (pDomain != IntPtr.Zero)
            {
                Win32.NetApiBufferFree(pDomain);
            }
            if (result == Win32.ErrorSuccess)
            {
                return status == Win32.NetJoinStatus.NetSetupDomainName;
            }
            else
            {
                throw new Exception("Domain Info Get Failed", new Win32Exception());
            }
        }
    }

    internal class Win32
    {
        public const int ErrorSuccess = 0;

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

        [DllImport("Netapi32.dll")]
        public static extern int NetApiBufferFree(IntPtr Buffer);

        public enum NetJoinStatus
        {
            NetSetupUnknownStatus = 0,
            NetSetupUnjoined,
            NetSetupWorkgroupName,
            NetSetupDomainName
        }

    }

    public class CredentialManager
    {



        public const string _credentialStoreName = "365drive.credential";

        /// <summary>
        /// the counter for SSO retries
        /// </summary>
        public static int ssoCounter = 0;

        // is it SSO
        public static bool isItSSOTry()
        {
            bool isAutoSSOEnabled = false;
            try
            {
                string autoSSO = RegistryManager.Get(RegistryKeys.AutoSSO);
                if (autoSSO == "1")
                {
                    isAutoSSOEnabled = true;
                }
                else
                {
                    isAutoSSOEnabled = false;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return isAutoSSOEnabled;
        }

        /// <summary>
        /// since autoSSO failed, we will ahve to make sure it doesnts keep trying all times so lets set registry to indicate that its failed :(
        /// </summary>
        /// <returns>returns whether the sso was ON or not, this will hep </returns>
        public static bool disableAutoSSO()
        {
            bool isAutoSSOEnabled = false;
            try
            {
                string autoSSO = RegistryManager.Get(RegistryKeys.AutoSSO);
                if (autoSSO == "1")
                {
                    isAutoSSOEnabled = true;

                    ///We need to undo EVERTHING and set AutoSSO to 0 which means dont try SSO here now for this user..
                    RegistryManager.Set(RegistryKeys.AutoSSO, "0");
                    //RegistryManager.Set(RegistryKeys.SSO, "0");
                    RemoveCredentials();
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return isAutoSSOEnabled;
        }



        /// <summary>
        /// reset the SSO counter to make a new beginning!
        /// </summary>
        public static void ResetSSOCounter()
        {
            ssoCounter = 0;
        }

        /// <summary>
        /// Make sure if the credential exist, set them
        /// </summary>
        /// <returns></returns>
        public static CredentialState ensureCredentials()
        {
            CredentialState state = CredentialState.Notpresent;
            try
            {
                Cred currentCreds = GetCredential();

                // if there is already a username password, no need to overwrite it. Lets respect user pref..
                if (currentCreds != null && !String.IsNullOrEmpty(currentCreds.UserName) && !string.IsNullOrEmpty(currentCreds.Password)&& !string.IsNullOrEmpty(RegistryManager.Get(RegistryKeys.RootSiteUrl)))
                {
                    
                    return CredentialState.Present;
                }

                //Lets check auto sso first (smartSSO)
                string autoSSO = RegistryManager.Get(RegistryKeys.AutoSSO);
                if (string.IsNullOrEmpty(autoSSO) && Convert.ToString(autoSSO) != "0")
                {
                    bool isMachineDomainJoined = ADDomainManager.IsInDomain();
                    if (isMachineDomainJoined)
                    {

                        //set the credentials as current user UPN
                        string UPN = System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName;
                        SetCredentials(UPN, string.Empty);

                        //found out that machine is domain joined so lets attempt auto sso - fingers crossed
                        LogManager.Info("Attempting auto SSO");
                        //RegistryManager.Set(RegistryKeys.SSO, "1");
                        RegistryManager.Set(RegistryKeys.AutoSSO, "1");
                    }
                }


                //If there is an SSO setting, we need to fetch it and ignore the credential check
                LogManager.Info("Ensuring SSO");
                //string isSSO = RegistryManager.Get(RegistryKeys.SSO);

                //lets make inform our engine that SSO failed
                bool blwasAutoSSOOn = CredentialManager.isItSSOTry();
                bool blRetryAgain = false;
                if (blwasAutoSSOOn)
                {
                    int iPendingRetries = CredentialManager.SSOPendingRetries();
                    blRetryAgain = iPendingRetries > 0;
                }


                LogManager.Info("SSO retry ON: " + blRetryAgain.ToString());
                //If its 1, ignore rest of credential checks
                if (blwasAutoSSOOn && blRetryAgain)
                {
                    state = CredentialState.Present;
                    if (currentCreds == null || String.IsNullOrEmpty(currentCreds.UserName))
                    {
                        //set the credentials as current user UPN
                        string UPN = System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName;
                        SetCredentials(UPN, string.Empty);
                    }
                }
                else if (blwasAutoSSOOn && !blRetryAgain)
                {
                    state = CredentialState.Notpresent;
                }
                else if (GetCredential() == null)
                {
                    state = CredentialState.Notpresent;
                }
                else
                {
                    state = CredentialState.Present;
                }
            }
            catch (Exception ex)
            {

                // probably the SSO server is not connetable. Lets switch back to credentials
                disableAutoSSO();
                state = CredentialState.ServerNotConnectable;

                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return state;
        }


        /// <summary>
        /// If its first time attempt (VERY first - i.e. we cant find the last license check details)
        /// </summary>
        public static int SSOPendingRetries()
        {
            string lastLicenseChecked = RegistryManager.Get(RegistryKeys.LastLicenseChecked);
            int retryPendings = 0;
            //if its blank / null which means its a first attempt
            if (string.IsNullOrEmpty(lastLicenseChecked))
            {
                retryPendings = Constants.SSOFirstTimeRetry - CredentialManager.ssoCounter;
            }
            else
            {
                retryPendings = Constants.SSORetry - CredentialManager.ssoCounter;
            }

            return retryPendings;
        }

        public static Cred GetCredential()
        {
            var cm = new Credential { Target = _credentialStoreName };
            try
            {
                cm = new Credential { Target = _credentialStoreName };
                if (!cm.Load())
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            //Cred is just a class with two string properties for user and pass
            // Send the password in encode format to handle & and ? which are considered differently in URL
            return new Cred(cm.Username, WebUtility.UrlEncode(cm.Password));
        }

        public static bool SetCredentials(
              string username, string password)
        {
            bool blSetCredentialstate = false;

            try
            {
                blSetCredentialstate = new Credential
                {
                    Target = _credentialStoreName,
                    Username = username,
                    Password = password,
                    PersistanceType = PersistanceType.LocalComputer
                }.Save();
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return blSetCredentialstate;
        }


        /// <summary>
        /// Deletes all saved credentials
        /// </summary>
        /// <returns></returns>
        public static bool RemoveCredentials()
        {
            bool blCredDeleted = false;

            try
            {
                blCredDeleted = new Credential { Target = _credentialStoreName }.Delete();
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return blCredDeleted;
        }
    }


    public class Cred
    {

        public string UserName { get; set; }
        public string Password { get; set; }

        public Cred(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;

        }
    }
}
