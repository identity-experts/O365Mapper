using _365Drive.Office365.CloudConnector;
using _365Drive.Office365.GetTenancyURL.CookieManager;
using FirstFloor.ModernUI.Windows.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Forms.Integration;

namespace _365Drive.Office365.GetTenancyURL
{
    public class GlobalCookieManager
    {
        #region Properties

        readonly string _username;
        readonly string _password;
        readonly bool _useRtfa;
        readonly Uri _host;

        CookieContainer _cachedCookieContainer = null;

        /// <summary>
        /// If the MFA is checked as remember, we will not ask for next time
        /// </summary>
        static bool rememberMFA { get; set; }

        #endregion

        public GlobalCookieManager(string host, string username, string password)
            : this(new Uri(host), username, password)
        {

        }
        public GlobalCookieManager(Uri host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            _useRtfa = true;
        }

        public CookieContainer getCookieContainer()
        {
            CookieContainer userCookies = new CookieContainer();

            if (DriveManager.FederationType == FedType.AAD)
            {
                AADConnectCookieManager cookieManager = new AADConnectCookieManager(_host.ToString(), _username, _password);
                userCookies = cookieManager.getCookieContainer();
            }
            else if (DriveManager.FederationType == FedType.Cloud)
            {
                o365cookieManager cookieManager = new o365cookieManager(_host.ToString(), _username, _password);
                userCookies = cookieManager.getCookieContainer();
            }

            else if (DriveManager.FederationType == FedType.ADFS)
            {
                ADFSAuth cookieManager = new ADFSAuth(new Uri(_host.ToString()), _username, _password, false);
                userCookies = cookieManager.getCookieContainer();
            }
            if (userCookies == null || userCookies.Count == 0)
            {
                return null;
            }

            return userCookies;
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

                        //your code

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
                        }
                        catch { }
                    }
                });
                if(userConsent)
                {
                    LicenseManager.lastConsentGranted = DateTime.Now;
                }
                return userConsent;
            }
            else
            {
                return true;
            }
        }

        public static string retrieveCodeFromMFA(string responseContent, CookieContainer authorizeCookies)
        {

            MFAConfig mfaConfig = _365DriveTenancyURL.renderMFAConfig(responseContent);


            string flowToken = mfaConfig.sFT;
            string ctx = mfaConfig.sCtx;
            string canary = mfaConfig.canary;
            string beginAuthUrl = mfaConfig.urlBeginAuth;
            string endAuthUrl = mfaConfig.urlEndAuth;
            string processAuthUrl = mfaConfig.urlPost;
            string authMethodId = mfaConfig.defaultAuthMethod;

            //Task<string> msLoginPostResponsestrongAuth = HttpClientHelper.PostAsync(StringConstants.AzureActivateUserStep2, MSloginpostData, "application/x-www-form-urlencoded", authorizeCookies, MSloginpostHeader);
            //msLoginPostResponsestrongAuth.Wait();

            //Task<string> responseContent = msLoginPostResponse.Result.Content.ReadAsStringAsync();
            //string strongAuthConstant = getStrongAuthConstant(responseContent);
            //string strongAuthContext = getStrongAuthContext(responseContent);
            //string canary = getCanary2(responseContent);
            //LogManager.Info("String Auth Constant: " + strongAuthConstant);
            //LogManager.Info("strongAuthContext: " + strongAuthContext);
            //LogManager.Info("canary: " + canary);

            //ensure the strong auth required or not
            if (!string.IsNullOrEmpty(ctx))
            {
                LogManager.Info("string auth not empty");

                //StrongAuthConstantResponse sAuthConstantResponse = JsonConvert.DeserializeObject<StrongAuthConstantResponse>(strongAuthConstant);
                //StrongAuthContextResponse sAuthContextResponse = JsonConvert.DeserializeObject<StrongAuthContextResponse>(strongAuthContext);

                //LogManager.Info("result" + Convert.ToString(sAuthContextResponse.Success)); 

                //Check whether MFA is really configured
                if (!string.IsNullOrEmpty(beginAuthUrl))
                {
                    LogManager.Info("its true");
                    if (MFAUserConsent())
                    {
                        LogManager.Info("MFAUserConsent required");
                        LogManager.Info("FToken" + flowToken);
                        LogManager.Info("CTX" + ctx);


                        //SMS based MFA
                        if (authMethodId.ToLower() == "onewaysms" || authMethodId.ToLower() == "phoneappotp")
                        {

                            NameValueCollection SASBeginAuthHeader = new NameValueCollection();
                            SASBeginAuthHeader.Add("Accept", "application/json");
                            SASBeginAuthHeader.Add("Referer", "https://login.microsoftonline.com/common/login");
                            SASBeginAuthHeader.Add("Accept-Language", "en-US");
                            SASBeginAuthHeader.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)");
                            SASBeginAuthHeader.Add("Host", "login.microsoftonline.com");

                            //begin auth
                            string SASBeginAuthBody = string.Format(StringConstants.SASBeginAuthPostBody, flowToken, ctx, authMethodId);
                            Task<string> SASBeginAuth = HttpClientHelper.PostAsync(beginAuthUrl, SASBeginAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                            SASBeginAuth.Wait();
                            string PollStart = DateTime.UtcNow.Ticks.ToString();


                            AuthResponse beginAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASBeginAuth.Result);
                            if (beginAuthResponse.Success.ToLower() == "true" && beginAuthResponse.ResultValue.ToLower() == "success")
                            {
                                string smsCode = PromptMFA(authMethodId);
                                string PollEnd = DateTime.UtcNow.Ticks.ToString();
                                string SASEndAuthBody = string.Format(StringConstants.SASEndAuthPostBody, beginAuthResponse.FlowToken, beginAuthResponse.SessionId, beginAuthResponse.Ctx, smsCode, PollStart, PollEnd, authMethodId);
                                Task<string> SASEndAuth = HttpClientHelper.PostAsync(endAuthUrl, SASEndAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                                SASEndAuth.Wait();

                                AuthResponse endAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASEndAuth.Result);
                                if (endAuthResponse.Success.ToLower() == "true" && endAuthResponse.ResultValue.ToLower() == "success")
                                {
                                    SASBeginAuthHeader["Accept"] = "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
                                    string SASProcessAuthBody = string.Format(StringConstants.SASProcessAuthPostBody, endAuthResponse.Ctx, endAuthResponse.FlowToken, LicenseManager.encode(canary), PollStart, PollEnd, rememberMFA.ToString().ToLower(), authMethodId);
                                    Task<string> SASProcessAuth = HttpClientHelper.PostAsync(processAuthUrl, SASProcessAuthBody, "application/x-www-form-urlencoded", authorizeCookies, SASBeginAuthHeader, true);
                                    SASProcessAuth.Wait();


                                    string outputControls = string.Empty;
                                    LoginConfig msPostConfig = _365DriveTenancyURL.renderConfig(SASProcessAuth.Result);

                                    if (msPostConfig == null)
                                    {
                                        outputControls = SASProcessAuth.Result;
                                    }
                                    else
                                    {
                                        string Authorizectx = msPostConfig.sCtx;
                                        string Authorizeflowtoken = msPostConfig.sFT;
                                        string msPostCanary = msPostConfig.canary;

                                        //getting ready for KMSI post
                                        string MSKMSIPostData = String.Format(StringConstants.KMSIPost, Authorizectx, Authorizeflowtoken, LicenseManager.encode(msPostCanary));
                                        Task<string> msKMSIPost = HttpClientHelper.PostAsync(StringConstants.loginKMSI, MSKMSIPostData, "application/x-www-form-urlencoded", authorizeCookies, new NameValueCollection());
                                        msKMSIPost.Wait();

                                        outputControls = msKMSIPost.Result;
                                    }

                                    return outputControls;
                                    //if (SASProcessAuth.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                                    //{
                                    //    //auth failed
                                    //    return string.Empty;
                                    //}
                                    //else
                                    //{
                                    //    NameValueCollection qscoll = HttpUtility.ParseQueryString(SASProcessAuth.Result.RequestMessage.RequestUri.Query);
                                    //    if (qscoll.Count > 0)
                                    //        return qscoll[0];
                                    //}

                                }
                                else
                                {
                                    //Its not MFA but probably wrong password or something else
                                    return string.Empty;
                                }

                            }
                            else
                            {
                                //Its not MFA but probably wrong password or something else
                                return string.Empty;
                            }
                        }
                        else if (authMethodId.ToLower() == "twowayvoicemobile" || authMethodId.ToLower() == "phoneappnotification" || authMethodId.ToLower() == "twowayvoiceoffice")
                        {

                            NameValueCollection SASBeginAuthHeader = new NameValueCollection();
                            SASBeginAuthHeader.Add("Accept", "application/json");
                            SASBeginAuthHeader.Add("Referer", "https://login.microsoftonline.com/common/login");
                            SASBeginAuthHeader.Add("Accept-Language", "en-US");
                            SASBeginAuthHeader.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)");
                            SASBeginAuthHeader.Add("Host", "login.microsoftonline.com");

                            //begin auth
                            string SASBeginAuthBody = string.Format(StringConstants.SASBeginAuthPostBody, flowToken, ctx, authMethodId);
                            Task<string> SASBeginAuth = HttpClientHelper.PostAsync(beginAuthUrl, SASBeginAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                            SASBeginAuth.Wait();
                            string PollStart = DateTime.UtcNow.Ticks.ToString();


                            AuthResponse beginAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASBeginAuth.Result);
                            if (beginAuthResponse.Success.ToLower() == "true" && beginAuthResponse.ResultValue.ToLower() == "success")
                            {

                                string verify = PromptMFA(authMethodId);
                                if (verify.ToLower() == "true")
                                {
                                    string PollEnd = DateTime.UtcNow.Ticks.ToString();
                                    string SASEndAuthBody = string.Format(StringConstants.SASCallEndAuthPostBody, beginAuthResponse.FlowToken, beginAuthResponse.SessionId, beginAuthResponse.Ctx, PollStart, PollEnd, authMethodId);
                                    Task<string> SASEndAuth = HttpClientHelper.PostAsync(endAuthUrl, SASEndAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                                    SASEndAuth.Wait();

                                    AuthResponse endAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASEndAuth.Result);
                                    if (endAuthResponse.Success.ToLower() == "true" && endAuthResponse.ResultValue.ToLower() == "success")
                                    {

                                        SASBeginAuthHeader["Accept"] = "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
                                        string SASProcessAuthBody = string.Format(StringConstants.SASProcessAuthPostBody, endAuthResponse.Ctx, endAuthResponse.FlowToken, LicenseManager.encode(canary), PollStart, PollEnd, rememberMFA.ToString().ToLower(), authMethodId);
                                        Task<string> SASProcessAuth = HttpClientHelper.PostAsync(processAuthUrl, SASProcessAuthBody, "application/x-www-form-urlencoded", authorizeCookies, SASBeginAuthHeader, true);
                                        SASProcessAuth.Wait();


                                        string outputControls = string.Empty;
                                        LoginConfig msPostConfig = _365DriveTenancyURL.renderConfig(SASProcessAuth.Result);

                                        if (msPostConfig == null)
                                        {
                                            outputControls = SASProcessAuth.Result;
                                        }
                                        else
                                        {
                                            string Authorizectx = msPostConfig.sCtx;
                                            string Authorizeflowtoken = msPostConfig.sFT;
                                            string msPostCanary = msPostConfig.canary;

                                            //getting ready for KMSI post
                                            string MSKMSIPostData = String.Format(StringConstants.KMSIPost, Authorizectx, Authorizeflowtoken, LicenseManager.encode(msPostCanary));
                                            Task<string> msKMSIPost = HttpClientHelper.PostAsync(StringConstants.loginKMSI, MSKMSIPostData, "application/x-www-form-urlencoded", authorizeCookies, new NameValueCollection());
                                            msKMSIPost.Wait();

                                            outputControls = msKMSIPost.Result;
                                        }

                                        return outputControls;
                                    }
                                    else
                                    {
                                        //Its not MFA but probably wrong password or something else
                                        return string.Empty;
                                    }
                                }
                                else
                                {
                                    //User didnt receive call or pressed cancel
                                    return string.Empty;
                                }

                            }
                            else
                            {
                                //Its not MFA but probably wrong password or something else
                                return string.Empty;
                            }
                        }
                        else
                        {
                            //Its not MFA but probably wrong password or something else
                            return string.Empty;
                        }

                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }

            // ... Read the string.
            return string.Empty;
        }


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
                            smsCode = mfaForm.smsCode;
                            rememberMFA = mfaForm.rememberMFA;
                        }
                        else if (authMethod.ToLower() == "twowayvoicemobile" || authMethod.ToLower() == "phoneappnotification" || authMethod.ToLower() == "twowayvoiceoffice")
                        {
                            smsCode = mfaForm.verify.ToString();
                            rememberMFA = mfaForm.rememberMFA;
                        }

                    }
                    catch { }
                }
            });
            return smsCode;
        }

        public static string getCanary2(string response)
        {
            int indexofCanary = response.IndexOf("\"canary\":") + 10;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string Canary = response.Substring(indexofCanary, endIndex - indexofCanary);
            return Canary;
        }

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
        private static void AboutForm_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Application curApp = System.Windows.Application.Current;
            Window mainWindow = curApp.MainWindow;
            ((ModernDialog)sender).Left = mainWindow.Left + (mainWindow.Width - ((ModernDialog)sender).ActualWidth) / 2;
            ((ModernDialog)sender).Top = mainWindow.Top + (mainWindow.Height - ((ModernDialog)sender).ActualHeight) / 2;
        }
    }

}
