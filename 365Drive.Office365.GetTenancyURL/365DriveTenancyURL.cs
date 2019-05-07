using _365Drive.Office365.GetTenancyURL;
using _365Drive.Office365.GetTenancyURL.CookieManager;
using CsQuery;
using FirstFloor.ModernUI.Windows.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Text.RegularExpressions;


namespace _365Drive.Office365.CloudConnector
{
    public static class _365DriveTenancyURL
    {




        /// <summary>
        /// If the MFA is checked as remember, we will not ask for next time
        /// </summary>
        static bool rememberMFA { get; set; }

        /// <summary>
        /// User has prompted to remind later
        /// </summary>
        static bool remindLater { get; set; }

        /// <summary>
        /// Get the tenancy name from username password combination
        /// </summary>
        /// <param name="upn">username</param>
        /// <param name="password">password</param>
        /// <returns></returns>
        public static string Get365TenancyName(string upn, string password)
        {
            string TenancyName = string.Empty;


            //Checking first registry
            if (!String.IsNullOrEmpty(RegistryManager.Get(RegistryKeys.TenancyName)) && !String.IsNullOrEmpty(RegistryManager.Get(RegistryKeys.RootSiteUrl)) && !String.IsNullOrEmpty(RegistryManager.Get(RegistryKeys.MySiteUrl)))
            {
                DriveManager.rootSiteUrl = RegistryManager.Get(RegistryKeys.RootSiteUrl);
                DriveManager.oneDriveHostSiteUrl = RegistryManager.Get(RegistryKeys.MySiteUrl);
                return RegistryManager.Get(RegistryKeys.TenancyName);
            }
            //check for exceptions first!
            else if (!string.IsNullOrEmpty(exceptionDomain(upn)))
            {
                TenancyName = exceptionDomain(upn);
            }
            else
            {
                TenancyName = null;
            }

            //Set the tennacy name at registry for next time use
            if (!string.IsNullOrEmpty(TenancyName) && TenancyName != "0")
                RegistryManager.Set(RegistryKeys.TenancyName, TenancyName);

            return TenancyName;
        }

        /// <summary>
        /// For companies like microsoft, we will not ask them to grant permission to app
        /// </summary>
        /// <param name="upn"></param>
        /// <returns></returns>
        public static string exceptionDomain(string upn)
        {
            string domainName = string.Empty;
            MailAddress address = new MailAddress(upn);
            string host = address.Host.ToString().ToLower(); // host contains yahoo.com

            if (Constants.exception_domains.Contains(host))
            {
                if (host == "microsoft.com")
                {
                    string tenancyUniqueName = "microsoft";

                    //Set onedrive host
                    DriveManager.oneDriveHostSiteUrl = "https://" + tenancyUniqueName + "-my.sharepoint.com";
                    RegistryManager.Set(RegistryKeys.MySiteUrl, DriveManager.oneDriveHostSiteUrl);


                    //as this is going to be needed at many places, we will save it 
                    DriveManager.rootSiteUrl = "https://" + tenancyUniqueName + ".sharepoint.com";
                    RegistryManager.Set(RegistryKeys.RootSiteUrl, DriveManager.rootSiteUrl);

                    domainName = "sharepoint" + StringConstants.rootUrltobeReplacedWith;
                }
            }
            return domainName;
        }

        
    




        private static void AboutForm_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Application curApp = System.Windows.Application.Current;
            Window mainWindow = curApp.MainWindow;
            ((ModernDialog)sender).Left = mainWindow.Left + (mainWindow.Width - ((ModernDialog)sender).ActualWidth) / 2;
            ((ModernDialog)sender).Top = mainWindow.Top + (mainWindow.Height - ((ModernDialog)sender).ActualHeight) / 2;
        }

        /// <summary>
        /// extract the config json from given html
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static LoginConfig renderConfig(string html)
        {
            try
            {
                //regex to parse the output
                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex(@"<!\[CDATA\[\s*(?:.(?<!\]\]>)\s*)*\]\]>", RegexOptions.None);
                string configVariableVal = @"\{(.|\s)*\}";

                bool isMatch = regex.IsMatch(html);
                LoginConfig config = null;
                if (isMatch)
                {
                    Match match = regex.Match(html);
                    string HTMLtext = match.Groups[0].Value;
                    Match m = Regex.Match(HTMLtext, configVariableVal);
                    if (m.Success)
                    {
                        string configJson = m.Value;
                        config = JsonConvert.DeserializeObject<LoginConfig>(configJson);
                        if (string.IsNullOrEmpty(config.canary) && string.IsNullOrEmpty(config.sCtx) && string.IsNullOrEmpty(config.sFT))
                        {
                            return null;
                        }
                    }
                }
                return config;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// extract the config json from given html for desktop SSO
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static dSSOConfig renderdSSOConfig(string html)
        {
            try
            {
                //regex to parse the output
                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex(@"<!\[CDATA\[\s*(?:.(?<!\]\]>)\s*)*\]\]>", RegexOptions.None);
                string configVariableVal = @"\{(.|\s)*\}";

                bool isMatch = regex.IsMatch(html);
                dSSOConfig config = null;
                if (isMatch)
                {
                    Match match = regex.Match(html);
                    string HTMLtext = match.Groups[0].Value;
                    Match m = Regex.Match(HTMLtext, configVariableVal);
                    if (m.Success)
                    {
                        string configJson = m.Value;
                        config = JsonConvert.DeserializeObject<dSSOConfig>(configJson);

                    }
                }
                return config;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// extract the config json from given html
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static MFAConfig renderMFAConfig(string html)
        {
            try
            {
                //regex to parse the output
                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex(@"<!\[CDATA\[\s*(?:.(?<!\]\]>)\s*)*\]\]>", RegexOptions.None);
                string configVariableVal = @"\{(.|\s)*\}";

                bool isMatch = regex.IsMatch(html);
                MFAConfig config = null;
                if (isMatch)
                {
                    Match match = regex.Match(html);
                    string HTMLtext = match.Groups[0].Value;
                    Match m = Regex.Match(HTMLtext, configVariableVal);
                    if (m.Success)
                    {
                        string configJson = m.Value;
                        config = JsonConvert.DeserializeObject<MFAConfig>(configJson);

                    }
                }
                return config;
            }
            catch
            { return null; }
        }

        /// <summary>
        /// Display about form
        /// </summary>
        public static string PromptMFA(string authMethod)
        {
            string smsCode = string.Empty;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                //Make sure the auth form is NOT already open
                bool isWindowOpen = false;

                foreach (Window w in System.Windows.Application.Current.Windows)
                {
                    if (w is _365Drive.Office365.UI.MFA.MFA)
                    {
                        isWindowOpen = true;
                        w.Activate();
                    }
                }

                if (!isWindowOpen)
                {

                    //your code
                    _365Drive.Office365.UI.MFA.MFA mfaForm = new _365Drive.Office365.UI.MFA.MFA();
                    mfaForm.sAuthContext_AuthMethodId = authMethod;

                    ElementHost.EnableModelessKeyboardInterop(mfaForm);
                    //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
                    try
                    {
                        mfaForm.Loaded += AboutForm_Loaded;
                        if (LicenseManager.isitPartnerManaged)
                        {
                            //change logo
                            mfaForm.partnerLogo = LicenseManager.partnerLogoBM;
                        }
                        mfaForm.ShowMFA();
                        bool? dialogResult = mfaForm.ShowDialog();


                        if (authMethod.ToLower() == "onewaysms" || authMethod.ToLower() == "phoneappotp")
                        {
                            rememberMFA = mfaForm.rememberMFA;
                            smsCode = mfaForm.smsCode;
                        }
                        else if (authMethod.ToLower() == "twowayvoicemobile" || authMethod.ToLower() == "phoneappnotification" || authMethod.ToLower() == "twowayvoiceoffice")
                        {
                            rememberMFA = mfaForm.rememberMFA;
                            smsCode = mfaForm.verify.ToString();
                        }

                    }
                    catch { }
                }
            });

            return smsCode;
        }


        public static bool MFAUserConsent()
        {
            if (LicenseManager.MFAConcentRequired)
            {
                bool userConsent = false;
                Application.Current.Dispatcher.Invoke((Action)delegate
                {

                    //Make sure the auth form is NOT already open
                    bool isWindowOpen = false;

                    foreach (Window w in System.Windows.Application.Current.Windows)
                    {
                        if (w is _365Drive.Office365.UI.MFA.MFAConfirmation)
                        {
                            isWindowOpen = true;
                            w.Activate();
                        }
                    }

                    if (!isWindowOpen)
                    {

                        _365Drive.Office365.UI.MFA.MFAConfirmation mfaForm = new _365Drive.Office365.UI.MFA.MFAConfirmation();

                        ElementHost.EnableModelessKeyboardInterop(mfaForm);
                        //getting DialogResult can be set only after Window is created and shown as dialog error. Will check later.
                        try
                        {
                            mfaForm.Loaded += AboutForm_Loaded;
                            if (LicenseManager.isitPartnerManaged)
                            {
                                //change logo
                                mfaForm.partnerLogo = LicenseManager.partnerLogoBM;
                            }

                            bool? dialogResult = mfaForm.ShowDialog();
                            userConsent = mfaForm.proceed;
                            if (mfaForm.RemindLater != null)
                                remindLater = true;

                            if (userConsent)
                            {
                                LicenseManager.lastConsentGranted = DateTime.Now;
                                LicenseManager.MFAConsent = true;
                            }
                        }
                        catch { }
                    }
                });
                return userConsent;
            }
            else
            {
                LicenseManager.MFAConsent = true;
                return true;
            }
        }

        /// <summary>
        /// Get tnancy url for aad connect tenancy type
        /// </summary>
        /// <returns></returns>
     


        public static string RemoveQueryStringByKey(string url, string key)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);

            // this removes the key if exists
            newQueryString.Remove(key);

            // this gets the page path from root without QueryString
            string pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            return newQueryString.Count > 0
                ? String.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
                : pagePathWithoutQueryString;
        }

        

        /// <summary>
        /// checks a fixed value in response. This is NOT the best way but for now, that sounds the way
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        static bool isItmodernAuth(string response)
        {
            if (response.Contains("isPollingRequired: true"))
            {
                return true;
            }
            return false;
        }



        /// <summary>
        /// Get the strong auth
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string getStrongAuthConstant(string response)
        {
            int indexofCanary = response.IndexOf("Constants.StrongAuth = ") + 23;
            int endIndex = response.IndexOf("};", indexofCanary + 1) + 1;
            string StrongAuthConstant = response.Substring(indexofCanary, endIndex - indexofCanary).Trim();
            return StrongAuthConstant;
        }

        /// <summary>
        /// Get the strong auth
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string getStrongAuthContext(string response)
        {
            int indexofCanary = response.IndexOf("Constants.StrongAuth.Context=") + 29;
            int endIndex = response.IndexOf("};", indexofCanary + 1) + 1;
            string StrongAuthConstant = response.Substring(indexofCanary, endIndex - indexofCanary);
            return StrongAuthConstant;
        }

        /// <summary>
        /// Get the session context for AAD
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string getapiCanary(string response)
        {
            int indexofCanary = response.IndexOf("\"apiCanary\":") + 13;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string Canary = response.Substring(indexofCanary, endIndex - indexofCanary);
            return Canary;
        }

        public static string getCanary2(string response)
        {
            int indexofCanary = response.IndexOf("\"canary\":") + 10;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string Canary = response.Substring(indexofCanary, endIndex - indexofCanary);
            return Canary;
        }



        public static string getDesktopSsoConfig(string response)
        {
            int indexofCanary = response.IndexOf("iwaEndpointUrlFormat: \"") + 23;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string Canary = response.Substring(indexofCanary, endIndex - indexofCanary);
            return Canary;
        }
        public static string getClientRequest(string response)
        {
            int indexofCanary = response.IndexOf("\"correlationId\":") + 17;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }

        public static string getHPGact(string response)
        {
            int indexofCanary = response.IndexOf("\"hpgact\":") + 9;
            int endIndex = response.IndexOf(",", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }


        public static string getpolingRequired(string response)
        {
            int indexofCanary = response.IndexOf("isPollingRequired: ") + 19;
            int endIndex = response.IndexOf(",", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }

        public static string getHPGId(string response)
        {
            int indexofCanary = response.IndexOf("\"hpgid\":") + 8;
            int endIndex = response.IndexOf(",", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }
    }
}