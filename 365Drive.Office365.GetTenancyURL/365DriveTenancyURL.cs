﻿using _365Drive.Office365.GetTenancyURL;
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Forms.Integration;

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
            else
            {
                //if not found in registry, we need to go and try gettting online
                if (DriveManager.FederationType == FedType.Cloud)
                {
                    TenancyName = cloudItentityTenancy(upn, password);
                }
                else if (DriveManager.FederationType == FedType.AAD)
                {
                    TenancyName = aadConnectTenancy(upn, password);
                }
                else if (DriveManager.FederationType == FedType.ADFS)
                {
                    TenancyName = adfsConnectTenancy(upn, password);
                }
            }

            //Set the tennacy name at registry for next time use
            if (!string.IsNullOrEmpty(TenancyName) && TenancyName != "0")
                RegistryManager.Set(RegistryKeys.TenancyName, TenancyName);

            return TenancyName;
        }

        /// <summary>
        /// Get tnancy url for cloud identity 
        /// </summary>
        /// <returns></returns>
        public static string cloudItentityTenancy(string upn, string password)
        {
            string tenancyUniqueName = string.Empty;
            //FedType authType = userRealM(upn);
            try
            {
                //are we ready?
                if (!Utility.ready())
                    return string.Empty;

                // Getting user activation step 1
                string Authorizectx = string.Empty,
                    Authorizeflowtoken = string.Empty,
                    authorizeCall = string.Empty,
                    AuthorizeCanaryapi = string.Empty,
                    AuthorizeclientRequestID = string.Empty,
                    Authorizehpgact = string.Empty,
                    Authorizehpgid = string.Empty,
                    dssoCanary = string.Empty,
                    authorizeCanary = string.Empty;
                // string call3result = string.Empty;
                string msPostFlow = string.Empty;
                string msPostCtx = string.Empty;
                string msPostHpgact = string.Empty;
                string msPostHpgid = string.Empty;
                string msPostCanary = string.Empty;
                string pollStartFlowToken = string.Empty;
                string pollStartctx = string.Empty;
                string authCode = string.Empty;
                string accessToken = string.Empty;

                CookieContainer authorizeCookies = new CookieContainer();

                ///Making call 1, this will be an initial call to microsoftonline to get the apiCananary, flow and ctx values
                LogManager.Verbose("Get request: " + String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri));
                string call1Url = String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri);
                Task<string> call1Result = HttpClientHelper.GetAsync(call1Url, authorizeCookies);
                call1Result.Wait();
                authorizeCall = call1Result.Result;

                ///Fetch the ctx and flow token
                CQ htmlparser = CQ.Create(authorizeCall);
                var items = htmlparser["input"];
                foreach (var li in items)
                {
                    if (li.Name == "ctx")
                    {
                        Authorizectx = li.Value;
                    }
                    if (li.Name == "flowToken")
                    {
                        Authorizeflowtoken = li.Value;
                    }
                }

                ///get other values of call 1
                Authorizehpgact = getHPGact(authorizeCall);
                Authorizehpgid = getHPGId(authorizeCall);
                msPostCanary = getCanary2(authorizeCall);
                LogManager.Verbose("Call 1 finished. output: ctx=" + Authorizectx + " flow=" + Authorizeflowtoken + " cookie count: " + authorizeCookies.Count.ToString());

                //getting ready for call 2
                string MSloginpostData = String.Format(StringConstants.AzureActivationUserLogin, upn, password, Authorizectx, Authorizeflowtoken, LicenseManager.encode(msPostCanary));
                NameValueCollection MSloginpostHeader = new NameValueCollection();
                MSloginpostHeader.Add("Accept", "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*");
                MSloginpostHeader.Add("Referer", String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri));
                MSloginpostHeader.Add("Accept-Language", "en-US");
                //request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                //MSloginpostHeader.Add("Accept-Encoding", "gzip, deflate");
                MSloginpostHeader.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)");
                MSloginpostHeader.Add("Host", "login.microsoftonline.com");
                MSloginpostHeader.Add("Accept", "application/json");
                Task<HttpResponseMessage> msLoginPostResponse = HttpClientHelper.PostAsyncFullResponse(StringConstants.AzureActivateUserStep2, MSloginpostData, "application/x-www-form-urlencoded", authorizeCookies, MSloginpostHeader);
                msLoginPostResponse.Wait();

                if (msLoginPostResponse.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                {
                    string code = retrieveCodeFromMFA(msLoginPostResponse.Result.Content.ReadAsStringAsync().Result, authorizeCookies);
                    if (string.IsNullOrEmpty(code))
                    {
                        ///if user has opted for remind me later, lets not say credentials wrong and send some code to core service so it can understand the reason
                        if (remindLater)
                            return "0";
                        else
                            return string.Empty;
                    }
                    else
                    {
                        authCode = code;
                    }
                }
                else
                {
                    NameValueCollection qscoll = HttpUtility.ParseQueryString(msLoginPostResponse.Result.RequestMessage.RequestUri.Query);
                    if (qscoll.Count > 0)
                        authCode = qscoll[0];
                }
                LogManager.Verbose("Call 2 finished");
                LogManager.Verbose("Call 2 code:" + authCode);


                //Getting user activation step 3
                string accessTokenpostData = String.Format(StringConstants.AzureActivationUserToken, authCode, StringConstants.clientID, LicenseManager.encode(StringConstants.clientSecret), StringConstants.appRedirectURL, StringConstants.appResourceUri);
                LogManager.Verbose("Access Token postdata:" + accessTokenpostData);

                Task<String> accessTokenResponse = HttpClientHelper.PostAsync((StringConstants.AzureActivateUserStep3), accessTokenpostData, "application/x-www-form-urlencoded");
                accessTokenResponse.Wait();
                AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(accessTokenResponse.Result);
                accessToken = tokenresponse.AccessToken;
                LogManager.Verbose("Access Token received:" + accessToken);

                //get the tenancy URL
                LogManager.Verbose("heading for get defaultURL call: " + String.Format(StringConstants.AzureActivateUserStep4, upn));
                NameValueCollection officeAPICallHeader = new NameValueCollection();
                officeAPICallHeader.Add("authorization", "bearer " + accessToken);
                Task<string> officeAPIcallResponse = HttpClientHelper.GetAsync(String.Format(StringConstants.AzureActivateUserStep4, upn), officeAPICallHeader);
                officeAPIcallResponse.Wait();

                MysiteResponse userSiteResponse = JsonConvert.DeserializeObject<MysiteResponse>(officeAPIcallResponse.Result);
                string rootSiteURL = userSiteResponse.value.FirstOrDefault(u => (u.entityKey.ToLower().Contains(StringConstants.rootUrlFinder))).serviceResourceId;

                string rootSitedocLibUrl = rootSiteURL.EndsWith("/") ? rootSiteURL + "Shared Documents" : rootSiteURL + "/Shared Documents";

                //as this is going to be needed at many places, we will save it 
                DriveManager.rootSiteUrl = rootSiteURL;
                RegistryManager.Set(RegistryKeys.RootSiteUrl, rootSiteURL);

                if (!string.IsNullOrEmpty(rootSiteURL))
                {
                    Uri url = new Uri(rootSiteURL);
                    tenancyUniqueName = url.Host.ToLower().Replace(StringConstants.rootUrltobeRemoved, "");
                }

                //Set onedrive host
                DriveManager.oneDriveHostSiteUrl = "https://" + tenancyUniqueName + "-my.sharepoint.com";
                RegistryManager.Set(RegistryKeys.MySiteUrl, DriveManager.oneDriveHostSiteUrl);


                LogManager.Verbose("office api call finished");
                LogManager.Verbose("tenancy name: " + tenancyUniqueName + StringConstants.rootUrltobeReplacedWith);
                tenancyUniqueName = tenancyUniqueName + StringConstants.rootUrltobeReplacedWith;
            }

            //}
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            //return the required name
            return tenancyUniqueName;
        }

        private static string retrieveCodeFromMFA(string responseContent, CookieContainer authorizeCookies)
        {


            //Task<string> msLoginPostResponsestrongAuth = HttpClientHelper.PostAsync(StringConstants.AzureActivateUserStep2, MSloginpostData, "application/x-www-form-urlencoded", authorizeCookies, MSloginpostHeader);
            //msLoginPostResponsestrongAuth.Wait();

            //Task<string> responseContent = msLoginPostResponse.Result.Content.ReadAsStringAsync();
            string strongAuthConstant = getStrongAuthConstant(responseContent);
            string strongAuthContext = getStrongAuthContext(responseContent);
            string canary = getCanary2(responseContent);
            //ensure the strong auth required or not
            if (!string.IsNullOrEmpty(strongAuthContext.Trim()))
            {
                StrongAuthConstantResponse sAuthConstantResponse = JsonConvert.DeserializeObject<StrongAuthConstantResponse>(strongAuthConstant);
                StrongAuthContextResponse sAuthContextResponse = JsonConvert.DeserializeObject<StrongAuthContextResponse>(strongAuthContext);

                //Check whether MFA is really configured
                if (sAuthContextResponse.Result.ToLower() == "true")
                {
                    if (MFAUserConsent())
                    {
                        string authMethodId = sAuthContextResponse.DefaultMethod.AuthMethodId;

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
                            string SASBeginAuthBody = string.Format(StringConstants.SASBeginAuthPostBody, sAuthConstantResponse.FlowToken, sAuthConstantResponse.Ctx, authMethodId);
                            Task<string> SASBeginAuth = HttpClientHelper.PostAsync(sAuthConstantResponse.SASControllerBeginAuthUrl, SASBeginAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                            SASBeginAuth.Wait();
                            string PollStart = DateTime.UtcNow.Ticks.ToString();


                            AuthResponse beginAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASBeginAuth.Result);
                            if (beginAuthResponse.Result.ToLower() == "true" && beginAuthResponse.ResultValue.ToLower() == "success")
                            {
                                string smsCode = PromptMFA(authMethodId);
                                string PollEnd = DateTime.UtcNow.Ticks.ToString();
                                string SASEndAuthBody = string.Format(StringConstants.SASEndAuthPostBody, beginAuthResponse.FlowToken, beginAuthResponse.SessionId, beginAuthResponse.Ctx, smsCode, PollStart, PollEnd, authMethodId);
                                Task<string> SASEndAuth = HttpClientHelper.PostAsync(sAuthConstantResponse.SASControllerEndAuthUrl, SASEndAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                                SASEndAuth.Wait();

                                AuthResponse endAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASEndAuth.Result);
                                if (endAuthResponse.Result.ToLower() == "true" && endAuthResponse.ResultValue.ToLower() == "success")
                                {
                                    SASBeginAuthHeader["Accept"] = "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
                                    string SASProcessAuthBody = string.Format(StringConstants.SASProcessAuthPostBody, endAuthResponse.Ctx, endAuthResponse.FlowToken, LicenseManager.encode(canary), PollStart, PollEnd, rememberMFA.ToString().ToLower(), authMethodId);
                                    Task<HttpResponseMessage> SASProcessAuth = HttpClientHelper.PostAsyncFullResponse(sAuthConstantResponse.SASControllerProcessAuthUrl, SASProcessAuthBody, "application/x-www-form-urlencoded", authorizeCookies, SASBeginAuthHeader, true);
                                    SASProcessAuth.Wait();

                                    if (SASProcessAuth.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                                    {
                                        //auth failed
                                        return string.Empty;
                                    }
                                    else
                                    {
                                        NameValueCollection qscoll = HttpUtility.ParseQueryString(SASProcessAuth.Result.RequestMessage.RequestUri.Query);
                                        if (qscoll.Count > 0)
                                            return qscoll[0];
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
                        else if (authMethodId.ToLower() == "twowayvoicemobile" || authMethodId.ToLower() == "phoneappnotification" || authMethodId.ToLower() == "twowayvoiceoffice")
                        {

                            NameValueCollection SASBeginAuthHeader = new NameValueCollection();
                            SASBeginAuthHeader.Add("Accept", "application/json");
                            SASBeginAuthHeader.Add("Referer", "https://login.microsoftonline.com/common/login");
                            SASBeginAuthHeader.Add("Accept-Language", "en-US");
                            SASBeginAuthHeader.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)");
                            SASBeginAuthHeader.Add("Host", "login.microsoftonline.com");

                            //begin auth
                            string SASBeginAuthBody = string.Format(StringConstants.SASBeginAuthPostBody, sAuthConstantResponse.FlowToken, sAuthConstantResponse.Ctx, authMethodId);
                            Task<string> SASBeginAuth = HttpClientHelper.PostAsync(sAuthConstantResponse.SASControllerBeginAuthUrl, SASBeginAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                            SASBeginAuth.Wait();
                            string PollStart = DateTime.UtcNow.Ticks.ToString();


                            AuthResponse beginAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASBeginAuth.Result);
                            if (beginAuthResponse.Result.ToLower() == "true" && beginAuthResponse.ResultValue.ToLower() == "success")
                            {
                                string verify = PromptMFA(authMethodId);
                                if (verify.ToLower() == "true")
                                {
                                    string PollEnd = DateTime.UtcNow.Ticks.ToString();
                                    string SASEndAuthBody = string.Format(StringConstants.SASCallEndAuthPostBody, beginAuthResponse.FlowToken, beginAuthResponse.SessionId, beginAuthResponse.Ctx, PollStart, PollEnd, authMethodId);
                                    Task<string> SASEndAuth = HttpClientHelper.PostAsync(sAuthConstantResponse.SASControllerEndAuthUrl, SASEndAuthBody, "application/json", authorizeCookies, SASBeginAuthHeader);
                                    SASEndAuth.Wait();

                                    AuthResponse endAuthResponse = JsonConvert.DeserializeObject<AuthResponse>(SASEndAuth.Result);
                                    if (endAuthResponse.Result.ToLower() == "true" && endAuthResponse.ResultValue.ToLower() == "success")
                                    {
                                        SASBeginAuthHeader["Accept"] = "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
                                        string SASProcessAuthBody = string.Format(StringConstants.SASProcessAuthPostBody, endAuthResponse.Ctx, endAuthResponse.FlowToken, LicenseManager.encode(canary), PollStart, PollEnd, rememberMFA.ToString().ToLower(), authMethodId);
                                        Task<HttpResponseMessage> SASProcessAuth = HttpClientHelper.PostAsyncFullResponse(sAuthConstantResponse.SASControllerProcessAuthUrl, SASProcessAuthBody, "application/x-www-form-urlencoded", authorizeCookies, SASBeginAuthHeader, true);
                                        SASProcessAuth.Wait();

                                        if (SASProcessAuth.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                                        {
                                            //auth failed
                                            return string.Empty;
                                        }
                                        else
                                        {
                                            NameValueCollection qscoll = HttpUtility.ParseQueryString(SASProcessAuth.Result.RequestMessage.RequestUri.Query);
                                            if (qscoll.Count > 0)
                                                return qscoll[0];
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
                                    //User denied to call verification
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

            else { return string.Empty; }
            // ... Read the string.
            return string.Empty;
        }

        private static void AboutForm_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Application curApp = System.Windows.Application.Current;
            Window mainWindow = curApp.MainWindow;
            ((ModernDialog)sender).Left = mainWindow.Left + (mainWindow.Width - ((ModernDialog)sender).ActualWidth) / 2;
            ((ModernDialog)sender).Top = mainWindow.Top + (mainWindow.Height - ((ModernDialog)sender).ActualHeight) / 2;
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
        public static string aadConnectTenancy(string upn, string password)
        {
            string tenancyUniqueName = string.Empty;
            try
            {
                //are we ready?
                if (!Utility.ready())
                    return string.Empty;

                // Getting user activation step 1
                string Authorizectx = string.Empty, Authorizeflowtoken = string.Empty, authorizeCall = string.Empty, AuthorizeCanaryapi = string.Empty, AuthorizeclientRequestID = string.Empty, Authorizehpgact = string.Empty, Authorizehpgid = string.Empty, dssoCanary = string.Empty, authorizeCanary = string.Empty;
                // string call3result = string.Empty;
                string msPostFlow = string.Empty;
                string msPostCtx = string.Empty;
                string msPostHpgact = string.Empty;
                string msPostHpgid = string.Empty;
                string msPostCanary = string.Empty;
                string pollStartFlowToken = string.Empty;
                string pollStartctx = string.Empty;
                string authCode = string.Empty;
                string accessToken = string.Empty;

                CookieContainer authorizeCookies = new CookieContainer();

                ///Making call 1, this will be an initial call to microsoftonline to get the apiCananary, flow and ctx values
                LogManager.Verbose("Get request: " + String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri));
                string call1Url = String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri);
                Task<string> call1Result = HttpClientHelper.GetAsync(call1Url, authorizeCookies);
                call1Result.Wait();
                authorizeCall = call1Result.Result;

                ///Fetch the ctx and flow token
                CQ htmlparser = CQ.Create(authorizeCall);
                var items = htmlparser["input"];
                foreach (var li in items)
                {
                    if (li.Name == "ctx")
                    {
                        Authorizectx = li.Value;
                    }
                    if (li.Name == "flowToken")
                    {
                        Authorizeflowtoken = li.Value;
                    }
                }

                ///get other values of call 1
                AuthorizeCanaryapi = getapiCanary(authorizeCall);
                authorizeCanary = getCanary2(authorizeCall);
                AuthorizeclientRequestID = getClientRequest(authorizeCall);
                Authorizehpgact = getHPGact(authorizeCall);
                Authorizehpgid = getHPGId(authorizeCall);
                LogManager.Verbose("Call 1 finished. output: ctx=" + Authorizectx + " flow=" + Authorizeflowtoken + " cookie count: " + authorizeCookies.Count.ToString());


                ///Making call 2 which is to inform Microsoft about desktop SSO. This will give us canary value
                //Prepare for namevalue pair using values retrieved from Authorize Call
                NameValueCollection dSSOHeader = new NameValueCollection();
                dSSOHeader.Add("canary", LicenseManager.encode(AuthorizeCanaryapi));
                dSSOHeader.Add("Referrer", "https://login.microsoftonline.com/common/login");
                dSSOHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                dSSOHeader.Add("client-request-id", AuthorizeclientRequestID);
                dSSOHeader.Add("Accept", "application/json");
                dSSOHeader.Add("X-Requested-With", "XMLHttpRequest");
                dSSOHeader.Add("hpgid", Authorizehpgid);
                dSSOHeader.Add("hpgact", Authorizehpgact);

                ///Adding other required cookies
                authorizeCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("testcookie", "testcookie"));
                authorizeCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSSOTILES", "1"));
                authorizeCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("AADSSOTILES", "1"));
                authorizeCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                authorizeCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSC", "00"));


                ///Heading for call 2 which is desktop SSO
                /// 
                Task<String> dSSOresponse = HttpClientHelper.PostAsync((StringConstants.dssoPoll), StringConstants.dssoPollBody, "application/json", authorizeCookies, dSSOHeader);
                dSSOresponse.Wait();

                apiCanaryResponse apiCanaryResponse = JsonConvert.DeserializeObject<apiCanaryResponse>(dSSOresponse.Result);
                dssoCanary = apiCanaryResponse.apiCanary;

                LogManager.Verbose("dsso call finished. Here is canary: " + dssoCanary);

                //preparing for call 3 which is post to MS login page 
                var msLoginPostCookies = new CookieContainer();

                Uri LoginPosturi = new Uri(StringConstants.loginPost);
                IEnumerable<Cookie> requestCookiesenum = authorizeCookies.GetCookies(LoginPosturi).Cast<Cookie>();

                foreach (Cookie cookie in requestCookiesenum)
                {
                    msLoginPostCookies.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie(cookie.Name, cookie.Value));
                }
                msLoginPostCookies.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("testcookie", "testcookie"));
                msLoginPostCookies.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("ESTSSSOTILES", "1"));
                msLoginPostCookies.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("AADSSOTILES", "1"));
                msLoginPostCookies.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                msLoginPostCookies.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("ESTSSC", "00"));


                string msLoginPostData = String.Format(StringConstants.loginPostData, upn, password, Authorizectx, Authorizeflowtoken, LicenseManager.encode(authorizeCanary));
                NameValueCollection postCalHeader = new NameValueCollection();
                Task<HttpResponseMessage> postCalresponse = HttpClientHelper.PostAsyncFullResponse((StringConstants.loginPost), msLoginPostData, "application/x-www-form-urlencoded", msLoginPostCookies, postCalHeader);
                postCalresponse.Wait();
                string postCalresponseString = postCalresponse.Result.Content.ReadAsStringAsync().Result;

                if (postCalresponse.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                {
                    CQ msPOSTcq = CQ.Create(postCalresponseString);

                    string isPollingRequired = getpolingRequired(postCalresponseString);
                    bool pollingRequired = false;
                    bool.TryParse(isPollingRequired, out pollingRequired);

                    if (pollingRequired)
                    {
                        var msPOSTcqItems = msPOSTcq["input"];
                        foreach (var li in msPOSTcqItems)
                        {
                            if (li.Name == "ctx")
                            {
                                if (!string.IsNullOrEmpty(li.Value))
                                    msPostCtx = li.Value;
                            }
                            if (li.Name == "flowToken")
                            {
                                msPostFlow = li.Value;
                            }
                        }

                        msPostHpgact = getHPGact(postCalresponseString);
                        msPostHpgid = getHPGId(postCalresponseString);
                        msPostCanary = getapiCanary(postCalresponseString);
                        LogManager.Verbose("MS post call finished. Canary: " + msPostCanary);

                        //preparing for call 4 which is poll start

                        //poll start cookie container
                        CookieContainer pollStartCookieContainer = new CookieContainer();
                        Uri msLoginPosturi = new Uri(StringConstants.loginPost);
                        IEnumerable<Cookie> responseCookies = msLoginPostCookies.GetCookies(msLoginPosturi).Cast<Cookie>();
                        foreach (Cookie cookie in responseCookies)
                        {
                            pollStartCookieContainer.Add(new Uri("https://" + new Uri(StringConstants.AADPoll).Authority), new Cookie(cookie.Name, cookie.Value));
                        }
                        pollStartCookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("testcookie", "testcookie"));

                        NameValueCollection pollStartHeader = new NameValueCollection();
                        pollStartHeader.Add("canary", LicenseManager.encode(msPostCanary));
                        pollStartHeader.Add("Referrer", "https://login.microsoftonline.com/common/login");
                        pollStartHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                        pollStartHeader.Add("client-request-id", AuthorizeclientRequestID);
                        pollStartHeader.Add("Accept", "application/json");
                        pollStartHeader.Add("X-Requested-With", "XMLHttpRequest");
                        pollStartHeader.Add("hpgid", msPostHpgid);
                        pollStartHeader.Add("hpgact", msPostHpgact);

                        string pollStartData = String.Format(StringConstants.AADPollBody, msPostFlow, msPostCtx);

                        //poll start heading
                        Task<String> pollStartResponse = HttpClientHelper.PostAsync((StringConstants.AADPoll), pollStartData, "application/json", pollStartCookieContainer, pollStartHeader);
                        pollStartResponse.Wait();

                        pollStartFlowToken = JsonConvert.DeserializeObject<pollResponse>(pollStartResponse.Result).flowToken;
                        pollStartctx = JsonConvert.DeserializeObject<pollResponse>(pollStartResponse.Result).ctx;
                        LogManager.Verbose("Poll start call finished. FlowToken: " + pollStartFlowToken + ", ctx: " + pollStartctx);


                        //poll end cookie container
                        CookieContainer pollEndCookieContainer = new CookieContainer();
                        foreach (Cookie cookie in responseCookies)
                        {
                            pollEndCookieContainer.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie(cookie.Name, cookie.Value));
                        }
                        pollEndCookieContainer.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie("testcookie", "testcookie"));

                        NameValueCollection pollEndHeader = new NameValueCollection();
                        pollEndHeader.Add("Referrer", "https://login.microsoftonline.com/common/login");
                        pollEndHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                        pollEndHeader.Add("Accept", "application/json");
                        pollEndHeader.Add("X-Requested-With", "XMLHttpRequest");

                        string pollEndData = String.Format(StringConstants.AADPollEndBody, pollStartFlowToken, pollStartctx);

                        //poll start heading
                        Task<HttpResponseMessage> pollEndResponse = HttpClientHelper.PostAsyncFullResponse((StringConstants.AADPollEnd), pollEndData, "application/x-www-form-urlencoded", pollEndCookieContainer, pollEndHeader);
                        pollEndResponse.Wait();
                        if (pollEndResponse.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.AADFailedLoginUrl.ToLower())
                        {
                            string code = retrieveCodeFromMFA(pollEndResponse.Result.Content.ReadAsStringAsync().Result, authorizeCookies);
                            if (string.IsNullOrEmpty(code))
                            {
                                ///if user has opted for remind me later, lets not say credentials wrong and send some code to core service so it can understand the reason
                                if (remindLater)
                                    return "0";
                                else
                                    return string.Empty;
                            }
                            else
                            {
                                authCode = code;
                            }
                        }
                        else
                        {
                            NameValueCollection qscoll = HttpUtility.ParseQueryString(pollEndResponse.Result.RequestMessage.RequestUri.Query);
                            if (qscoll.Count > 0)
                                authCode = qscoll[0];
                        }
                    }
                    else
                    {
                        if (postCalresponse.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                        {
                            string code = retrieveCodeFromMFA(postCalresponse.Result.Content.ReadAsStringAsync().Result, authorizeCookies);
                            if (string.IsNullOrEmpty(code))
                            {
                                ///if user has opted for remind me later, lets not say credentials wrong and send some code to core service so it can understand the reason
                                if (remindLater)
                                    return "0";
                                else
                                    return string.Empty;
                                //return string.Empty;
                            }
                            else
                            {
                                authCode = code;
                            }
                        }
                        else
                        {
                            NameValueCollection qscoll = HttpUtility.ParseQueryString(postCalresponse.Result.RequestMessage.RequestUri.Query);
                            if (qscoll.Count > 0)
                                authCode = qscoll[0];
                        }
                    }

                }
                else
                {
                    NameValueCollection qscoll = HttpUtility.ParseQueryString(postCalresponse.Result.RequestMessage.RequestUri.Query);
                    if (qscoll.Count > 0)
                        authCode = qscoll[0];
                }



                //NameValueCollection pollEndRedirect = HttpUtility.ParseQueryString(pollEndResponse.Result.RequestMessage.RequestUri.Query);
                //if (pollEndRedirect.Count > 0)
                //    authCode = pollEndRedirect[0];

                LogManager.Verbose("Poll end call finished. Code: " + authCode);

                //get access token from auth code
                string accessTokenpostData = String.Format(StringConstants.AzureActivationUserToken, authCode, StringConstants.clientID, LicenseManager.encode(StringConstants.clientSecret), StringConstants.appRedirectURL, StringConstants.appResourceUri);
                LogManager.Verbose("Access Token postdata:" + accessTokenpostData);

                Task<String> accessTokenResponse = HttpClientHelper.PostAsync((StringConstants.AzureActivateUserStep3), accessTokenpostData, "application/x-www-form-urlencoded");
                accessTokenResponse.Wait();
                AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(accessTokenResponse.Result);
                accessToken = tokenresponse.AccessToken;
                LogManager.Verbose("Access Token received:" + accessToken);

                //get the tenancy URL
                LogManager.Verbose("heading for get defaultURL call: " + String.Format(StringConstants.AzureActivateUserStep4, upn));
                NameValueCollection officeAPICallHeader = new NameValueCollection();
                officeAPICallHeader.Add("authorization", "bearer " + accessToken);
                Task<string> officeAPIcallResponse = HttpClientHelper.GetAsync(String.Format(StringConstants.AzureActivateUserStep4, upn), officeAPICallHeader);
                officeAPIcallResponse.Wait();

                MysiteResponse userSiteResponse = JsonConvert.DeserializeObject<MysiteResponse>(officeAPIcallResponse.Result);
                string rootSiteURL = userSiteResponse.value.FirstOrDefault(u => (u.entityKey.ToLower().Contains(StringConstants.rootUrlFinder))).serviceResourceId;

                string rootSitedocLibUrl = rootSiteURL.EndsWith("/") ? rootSiteURL + "Shared Documents" : rootSiteURL + "/Shared Documents";

                //as this is going to be needed at many places, we will save it 
                DriveManager.rootSiteUrl = rootSiteURL;
                RegistryManager.Set(RegistryKeys.RootSiteUrl, DriveManager.rootSiteUrl);

                if (!string.IsNullOrEmpty(rootSiteURL))
                {
                    Uri url = new Uri(rootSiteURL);
                    tenancyUniqueName = url.Host.ToLower().Replace(StringConstants.rootUrltobeRemoved, "");
                }

                //Set onedrive host
                DriveManager.oneDriveHostSiteUrl = "https://" + tenancyUniqueName + "-my.sharepoint.com";
                RegistryManager.Set(RegistryKeys.MySiteUrl, DriveManager.oneDriveHostSiteUrl);

                LogManager.Verbose("office api call finished");
                LogManager.Verbose("tenancy name: " + tenancyUniqueName + StringConstants.rootUrltobeReplacedWith);
                tenancyUniqueName = tenancyUniqueName + StringConstants.rootUrltobeReplacedWith;

            }

            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            return tenancyUniqueName;

        }


        public static string adfsConnectTenancy(string upn, string password)
        {
            string tenancyUniqueName = string.Empty;
            try
            {
                //are we ready?
                if (!Utility.ready())
                    return string.Empty;


                //ADFS tenancy name get code goes here

                CookieContainer authorizeCookies = new CookieContainer();

                ///Making call 1, this will be an initial call to microsoftonline to get the apiCananary, flow and ctx values
                LogManager.Verbose("Get request: " + String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri));
                string call1Url = String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri);
                Task<string> call1Result = HttpClientHelper.GetAsync(call1Url, authorizeCookies);
                call1Result.Wait();
                string authorizeCall = call1Result.Result;


                ////retrieving ctx
                string MSctx = string.Empty, flowToken = string.Empty, canary = string.Empty;
                CQ msPOSTcq = CQ.Create(authorizeCall);
                var msPOSTcqItems = msPOSTcq["input"];
                foreach (var li in msPOSTcqItems)
                {
                    if (li.Name == "ctx")
                    {
                        if (!string.IsNullOrEmpty(li.Value))
                            MSctx = li.Value;
                    }
                }


                //call 2 ADFS
                //string adfsPostUrl = string.Format(StringConstants.AdfsPost, DriveManager.ADFSAuthURL, upn, MSctx);
                string ADFSRealM = String.Format(StringConstants.ADFSRealM, LicenseManager.encode(upn), MSctx);
                Task<string> ADFSRealMResult = HttpClientHelper.GetAsync(ADFSRealM, authorizeCookies);
                ADFSRealMResult.Wait();

                RealM ADFSRealMresponse = JsonConvert.DeserializeObject<RealM>(ADFSRealMResult.Result);

                //get the ADFS post URL
                string strADFSPostUrl = ADFSRealMresponse.AuthURL;
                string adfsPostBody = string.Format(StringConstants.AdfsPostBody, upn, password);
                Task<string> adfsloginPostBodyResult = HttpClientHelper.PostAsync(strADFSPostUrl, adfsPostBody, "application/x-www-form-urlencoded", authorizeCookies);
                adfsloginPostBodyResult.Wait();


                //retrieving rst
                string rst = string.Empty;
                CQ adfsPostResponse = CQ.Create(adfsloginPostBodyResult.Result);
                var adfsPostResponseItems = adfsPostResponse["input"];
                foreach (var li in adfsPostResponseItems)
                {
                    if (li.Name == "wresult")
                    {
                        if (!string.IsNullOrEmpty(li.Value))
                            rst = li.Value;
                    }
                }

                string authCode = string.Empty, accessToken = string.Empty;
                //post rst to microsoft
                string strrstPostBody = string.Format(StringConstants.ADFSrstPostBody, LicenseManager.encode(rst), MSctx);
                Task<HttpResponseMessage> ADFSrstPostResult = HttpClientHelper.PostAsyncFullResponse(StringConstants.ADFSrstPost, strrstPostBody, "application/x-www-form-urlencoded", authorizeCookies, new NameValueCollection());
                ADFSrstPostResult.Wait();

                if (ADFSrstPostResult.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.ADFSailedLoginUrl.ToLower())
                {
                    string code = retrieveCodeFromMFA(ADFSrstPostResult.Result.Content.ReadAsStringAsync().Result, authorizeCookies);
                    if (string.IsNullOrEmpty(code))
                    {
                        ///if user has opted for remind me later, lets not say credentials wrong and send some code to core service so it can understand the reason
                        if (remindLater)
                            return "0";
                        else
                            return string.Empty;
                        //return string.Empty;
                    }
                    else
                    {
                        authCode = code;
                    }
                }
                else
                {
                    NameValueCollection qscoll = HttpUtility.ParseQueryString(ADFSrstPostResult.Result.RequestMessage.RequestUri.Query);
                    if (qscoll.Count > 0)
                        authCode = qscoll[0];
                }


                //NameValueCollection pollEndRedirect = HttpUtility.ParseQueryString(ADFSrstPostResult.Result.RequestMessage.RequestUri.Query);
                //if (pollEndRedirect.Count > 0)
                //    authCode = pollEndRedirect[0];

                LogManager.Verbose("Poll end call finished. Code: " + authCode);

                //get access token from auth code
                string accessTokenpostData = String.Format(StringConstants.AzureActivationUserToken, authCode, StringConstants.clientID, LicenseManager.encode(StringConstants.clientSecret), StringConstants.appRedirectURL, StringConstants.appResourceUri);
                LogManager.Verbose("Access Token postdata:" + accessTokenpostData);

                Task<String> accessTokenResponse = HttpClientHelper.PostAsync((StringConstants.AzureActivateUserStep3), accessTokenpostData, "application/x-www-form-urlencoded");
                accessTokenResponse.Wait();
                AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(accessTokenResponse.Result);
                accessToken = tokenresponse.AccessToken;
                LogManager.Verbose("Access Token received:" + accessToken);

                //get the tenancy URL
                LogManager.Verbose("heading for get defaultURL call: " + String.Format(StringConstants.AzureActivateUserStep4, upn));
                NameValueCollection officeAPICallHeader = new NameValueCollection();
                officeAPICallHeader.Add("authorization", "bearer " + accessToken);
                Task<string> officeAPIcallResponse = HttpClientHelper.GetAsync(String.Format(StringConstants.AzureActivateUserStep4, upn), officeAPICallHeader);
                officeAPIcallResponse.Wait();

                MysiteResponse userSiteResponse = JsonConvert.DeserializeObject<MysiteResponse>(officeAPIcallResponse.Result);
                string rootSiteURL = userSiteResponse.value.FirstOrDefault(u => (u.entityKey.ToLower().Contains(StringConstants.rootUrlFinder))).serviceResourceId;

                string rootSitedocLibUrl = rootSiteURL.EndsWith("/") ? rootSiteURL + "Shared Documents" : rootSiteURL + "/Shared Documents";

                //as this is going to be needed at many places, we will save it 
                DriveManager.rootSiteUrl = rootSiteURL;
                RegistryManager.Set(RegistryKeys.RootSiteUrl, DriveManager.rootSiteUrl);

                if (!string.IsNullOrEmpty(rootSiteURL))
                {
                    Uri url = new Uri(rootSiteURL);
                    tenancyUniqueName = url.Host.ToLower().Replace(StringConstants.rootUrltobeRemoved, "");
                }

                //Set onedrive host
                DriveManager.oneDriveHostSiteUrl = "https://" + tenancyUniqueName + "-my.sharepoint.com";
                RegistryManager.Set(RegistryKeys.MySiteUrl, DriveManager.oneDriveHostSiteUrl);

                LogManager.Verbose("office api call finished");
                LogManager.Verbose("tenancy name: " + tenancyUniqueName + StringConstants.rootUrltobeReplacedWith);
                tenancyUniqueName = tenancyUniqueName + StringConstants.rootUrltobeReplacedWith;


            }

            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            return tenancyUniqueName;

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