using _365Drive.Office365.GetTenancyURL;
using CsQuery;
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

namespace _365Drive.Office365.CloudConnector
{
    public static class _365DriveTenancyURL
    {
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
            }

            //Set the tennacy name at registry for next time use
            if (!string.IsNullOrEmpty(TenancyName))
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
                LogManager.Verbose("Call 1 finished. output: ctx=" + Authorizectx + " flow=" + Authorizeflowtoken + " cookie count: " + authorizeCookies.Count.ToString());

                //getting ready for call 2
                string MSloginpostData = String.Format(StringConstants.AzureActivationUserLogin, upn, password, Authorizectx, Authorizeflowtoken);
                NameValueCollection MSloginpostHeader = new NameValueCollection();
                MSloginpostHeader.Add("Accept", "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*");
                MSloginpostHeader.Add("Referer", String.Format(StringConstants.AzureActivateUserStep1, upn, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri));
                MSloginpostHeader.Add("Accept-Language", "en-US");
                //request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                MSloginpostHeader.Add("Accept-Encoding", "gzip, deflate");
                MSloginpostHeader.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)");
                MSloginpostHeader.Add("Host", "login.microsoftonline.com");
                MSloginpostHeader.Add("Accept", "application/json");
                Task<HttpResponseMessage> msLoginPostResponse = HttpClientHelper.PostAsyncFullResponse(StringConstants.AzureActivateUserStep2, MSloginpostData, "application/x-www-form-urlencoded", MSloginpostHeader);
                msLoginPostResponse.Wait();

                if (msLoginPostResponse.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                {
                    return string.Empty;
                    //LogManager.Verbose("Login failed :(");
                    //return string.Empty;
                }
                // ... Read the string.

                NameValueCollection qscoll = HttpUtility.ParseQueryString(msLoginPostResponse.Result.RequestMessage.RequestUri.Query);
                if (qscoll.Count > 0)
                    authCode = qscoll[0];

                LogManager.Verbose("Call 2 finished");
                LogManager.Verbose("Call 2 code:" + authCode);


                //Getting user activation step 3
                string accessTokenpostData = String.Format(StringConstants.AzureActivationUserToken, authCode, StringConstants.clientID, StringConstants.clientSecret, StringConstants.appRedirectURL, StringConstants.appResourceUri);
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

        /// <summary>
        /// Get tnancy url for cloud identity 
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
                dSSOHeader.Add("canary", AuthorizeCanaryapi);
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


                string msLoginPostData = String.Format(StringConstants.loginPostData, upn, password, Authorizectx, Authorizeflowtoken, authorizeCanary);
                Task<String> postCalresponse = HttpClientHelper.PostAsync((StringConstants.loginPost), msLoginPostData, "application/x-www-form-urlencoded", msLoginPostCookies);
                postCalresponse.Wait();

                CQ msPOSTcq = CQ.Create(postCalresponse.Result);
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

                msPostHpgact = getHPGact(postCalresponse.Result);
                msPostHpgid = getHPGId(postCalresponse.Result);
                msPostCanary = getapiCanary(postCalresponse.Result);
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
                pollStartHeader.Add("canary", msPostCanary);
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


                NameValueCollection pollEndRedirect = HttpUtility.ParseQueryString(pollEndResponse.Result.RequestMessage.RequestUri.Query);
                if (pollEndRedirect.Count > 0)
                    authCode = pollEndRedirect[0];

                LogManager.Verbose("Poll end call finished. Code: " + authCode);

                //get access token from auth code
                string accessTokenpostData = String.Format(StringConstants.AzureActivationUserToken, authCode, StringConstants.clientID, StringConstants.clientSecret, StringConstants.appRedirectURL, StringConstants.appResourceUri);
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
                tenancyUniqueName += tenancyUniqueName + StringConstants.rootUrltobeReplacedWith;

                //__________________________________________________________________________________________________
                //using (var postBodyHandler = new HttpClientHandler() { CookieContainer = postBodyContainer })

                //using (HttpClient request = new HttpClient(handler))
                //{

                //    LogManager.Verbose("Get request: " + String.Format(StringConstants.AzureActivateUserStep1, upn));
                //    Task<HttpResponseMessage> response = request.GetAsync(String.Format(StringConstants.AzureActivateUserStep1, upn));
                //    response.Wait();

                //    if (response.Result.Content != null)
                //    {

                //        using (HttpContent content = response.Result.Content)
                //        {
                //            //read all cookies

                //            // ... Read the string.
                //            Task<string> result = content.ReadAsStringAsync();
                //            authorizeCall = result.Result;
                //            CQ htmlparser = CQ.Create(result.Result);
                //            var items = htmlparser["input"];
                //            foreach (var li in items)
                //            {
                //                if (li.Name == "ctx")
                //                {
                //                    ctx = li.Value;
                //                }
                //                if (li.Name == "flowToken")
                //                {
                //                    flowtoken = li.Value;
                //                }
                //            }
                //        }

                //    }
                //}



                //AAD connect 
                //if (isItmodernAuth(call2result))
                //{
                // LogManager.Verbose("Its not a normal cloud auth. Moving for AAD connect auth as realM suggests its cloud identity, it must be Modern Auth");







                //append cookies
                //var cookieContainer = new CookieContainer();
                //using (var pollHandler = new HttpClientHandler() { CookieContainer = cookieContainer })
                ////call 2 again as per AAD connect
                //using (HttpClient request = new HttpClient(pollHandler))
                //{
                //    Uri uri = new Uri(StringConstants.AzureActivateUserStep1);
                //    IEnumerable<Cookie> responseCookies = authorizeCookies.GetCookies(uri).Cast<Cookie>();

                //    foreach (Cookie cookie in responseCookies)
                //    {
                //        cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie(cookie.Name, cookie.Value));
                //    }
                //    cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("testcookie", "testcookie"));
                //    cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSSOTILES", "1"));
                //    cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("AADSSOTILES", "1"));
                //    cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                //    cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSC", "00"));
                //    //cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.AADPoll).Authority), new Cookie("ESTSAUTHPERSISTENT", "AQABAAEAAADRNYRQ3dhRSrm-4K-adpCJR9-o-mKIab1XFlLX2Zww26R14c1SK-Gm5sq3ndf9emPSk_6z5Ib1YTUqKNPivt5mDP5aYI9p4W8XB4BsyERuUIFJH55ZCeL4swr2ahRa6i4S5_B_dNzRPl_UwMCrd1vwZ-LUntw681RRCn-v2gETJSAHYGPiq3erAZq1SI1Q8dA2BV8el8uQaH0_hKotDq11X9tsMwGBVHb4UGvr9cqen6UWS0uD2kTy9LNTvkljzOSvqEh4eHBH_u4Uns4_Zta2uVBTepCuHJj1d2wo2JdbIX350QlfqMqMSBWCe9xInHJhnidbz_FyBlivwxYUjXfstU8Lain_kZDJoFBl_Kek0CB5WlgGQtn1GxOIqDo6EVM4ljvSOMS9fbnDfUlWWKfMblPl5SlXPmJ5E0b5T_K4PdXO8YjU6lROwxXxdvXAVi3HFbwtDjqDGe_1HXDH6jaXgiVIm3QONQvuuC8IyIRE4KvVKvKGKYIh-gs_abvrVC0BRclt7kGmMIAcaLx41WNzxnsCjVf4ibTeXHpll5zXLa9ljdMnypVsH9LgMArcQe2F1zvwahexikfvuNE60CP9IAA"));
                //    //cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.AADPoll).Authority), new Cookie("ESTSAUTH", "QVFBQkFBRUFBQURSTllSUTNkaFJTcm0tNEstYWRwQ0pCcnJLM3J0VzZrZzdZcURYdy1NeFZ0SHl4UVVXdmdaRVg5Ukg2NVZWejVhdk9EVExwLWFLV256UjVDdGdyOU1DTU13Zy15d0dFcDhNSE1NZDNyWldwQ0FB"));

                //    //string aadPollpostData = String.Format(StringConstants.AADPollBody, Authorizeflowtoken, Authorizectx);
                //    //LogManager.Verbose("Call AAD poll  postdata:" + aadPollpostData);
                //    // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                //    request.DefaultRequestHeaders.Add("canary", AuthorizeCanary);
                //    request.DefaultRequestHeaders.Add("Referrer", "https://login.microsoftonline.com/common/login");
                //    request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                //    request.DefaultRequestHeaders.Add("client-request-id", AuthorizeclientRequestID);
                //    request.DefaultRequestHeaders.Add("Accept", "application/json");
                //    request.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                //    request.DefaultRequestHeaders.Add("hpgid", Authorizehpgid);
                //    request.DefaultRequestHeaders.Add("hpgact", Authorizehpgact);
                //    //request.DefaultRequestHeaders.Add("Content-Length", aadPollpostData.Length.ToString());


                //    Task<HttpResponseMessage> response = request.PostAsync((StringConstants.dssoPoll), new StringContent(StringConstants.dssoPollBody, Encoding.UTF8, "application/json"));
                //    response.Wait();

                //    if (response.Result.Content != null)
                //    {
                //        using (HttpContent content = response.Result.Content)
                //        {
                //            Task<string> result = content.ReadAsStringAsync();
                //            apiCanaryResponse apiCanaryResponse = JsonConvert.DeserializeObject<apiCanaryResponse>(result.Result);
                //            dssoCanary = apiCanaryResponse.apiCanary;
                //        }
                //    }
                //}

                //append cookies
                //var postBodyContainer = new CookieContainer();
                //using (var postBodyHandler = new HttpClientHandler() { CookieContainer = postBodyContainer })
                ////call 2 again as per AAD connect
                //using (HttpClient postBodyrequest = new HttpClient(postBodyHandler))

                //{

                //    string call1Canary = getCanary2(authorizeCall);
                //    string postData = String.Format(StringConstants.loginPostData, upn, password, Authorizectx, Authorizeflowtoken, call1Canary);
                //    LogManager.Verbose("Call 3 postdata:" + postData);
                //    // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");


                //    //request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                //    Task<HttpResponseMessage> postCalresponse = postBodyrequest.PostAsync((StringConstants.loginPost), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));
                //    postCalresponse.Wait();

                //    if (postCalresponse.Result.Content != null)
                //    {
                //        using (HttpContent content = postCalresponse.Result.Content)
                //        {
                //            Task<string> result = content.ReadAsStringAsync();
                //            call3result = result.Result;
                //            CQ htmlparser = CQ.Create(result.Result);
                //            var items = htmlparser["input"];
                //            foreach (var li in items)
                //            {
                //                if (li.Name == "ctx")
                //                {
                //                    if (!string.IsNullOrEmpty(li.Value))
                //                        msPostCtx = li.Value;
                //                }
                //                if (li.Name == "flowToken")
                //                {
                //                    msPostFlow = li.Value;
                //                }
                //            }

                //            msPostHpgact = getHPGact(call3result);
                //            msPostHpgid = getHPGId(call3result);
                //            msPostCanary = getapiCanary(call3result);

                //        }
                //    }
                //}
                //pollResponse pollStartresponse = new pollResponse();
                //var pollCookies = new CookieContainer();
                //using (var pollHandler = new HttpClientHandler() { CookieContainer = pollCookies })
                ////call 2 again as per AAD connect
                //using (HttpClient request = new HttpClient(pollHandler))
                //{

                //    string postData = String.Format(StringConstants.AADPollBody, msPostFlow, msPostCtx);
                //    LogManager.Verbose("Call 3 postdata:" + postData);
                //    // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                //    Uri uri = new Uri(StringConstants.loginPost);
                //    IEnumerable<Cookie> responseCookies = postBodyContainer.GetCookies(uri).Cast<Cookie>();

                //    foreach (Cookie cookie in responseCookies)
                //    {
                //        pollCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPoll).Authority), new Cookie(cookie.Name, cookie.Value));
                //    }
                //    pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("testcookie", "testcookie"));
                //    //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSSOTILES", "1"));
                //    //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("AADSSOTILES", "1"));
                //    //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                //    //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSC", "00"));

                //    request.DefaultRequestHeaders.Add("canary", msPostCanary);
                //    request.DefaultRequestHeaders.Add("Referrer", "https://login.microsoftonline.com/common/login");
                //    request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                //    request.DefaultRequestHeaders.Add("client-request-id", AuthorizeclientRequestID);
                //    request.DefaultRequestHeaders.Add("Accept", "application/json");
                //    request.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                //    request.DefaultRequestHeaders.Add("hpgid", msPostHpgid);
                //    request.DefaultRequestHeaders.Add("hpgact", msPostHpgact);

                //    Task<HttpResponseMessage> response = request.PostAsync((StringConstants.AADPoll), new StringContent(postData, Encoding.UTF8, "application/json"));


                //    response.Wait();

                //    if (response.Result.Content != null)
                //    {
                //        using (HttpContent content = response.Result.Content)
                //        {
                //            Task<string> result = content.ReadAsStringAsync();
                //            pollStartresponse = JsonConvert.DeserializeObject<pollResponse>(result.Result);
                //            //AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(result.Result);
                //            //   var token = JsonConvert.SerializeObject( result.Result);
                //            //access_token = tokenresponse.AccessToken;
                //        }
                //    }
                //}

                ////call 2 again as per AAD connect
                //var pollendCookies = new CookieContainer();

                //using (var pollHandler = new HttpClientHandler() { CookieContainer = pollendCookies })
                //using (HttpClient request = new HttpClient(pollHandler))
                //{

                //    string postData = String.Format(StringConstants.AADPollEndBody, pollStartresponse.flowToken, pollStartresponse.ctx);
                //    LogManager.Verbose("Call 3 postdata:" + postData);
                //    // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                //    Uri uri = new Uri(StringConstants.loginPost);
                //    IEnumerable<Cookie> responseCookies = postBodyContainer.GetCookies(uri).Cast<Cookie>();

                //    foreach (Cookie cookie in responseCookies)
                //    {
                //        pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie(cookie.Name, cookie.Value));
                //    }
                //    pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie("testcookie", "testcookie"));

                //    request.DefaultRequestHeaders.Add("Referrer", "https://login.microsoftonline.com/common/login");
                //    request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                //    request.DefaultRequestHeaders.Add("Accept", "application/json");
                //    request.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

                //    Task<HttpResponseMessage> response = request.PostAsync((StringConstants.AADPollEnd), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));


                //    response.Wait();

                //    if (response.Result.Content != null)
                //    {
                //        using (HttpContent content = response.Result.Content)
                //        {
                //            Task<string> result = content.ReadAsStringAsync();
                //            //call2result = result.Result;

                //            // Make sure its authenticated
                //            if (response.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                //            {
                //                //LogManager.Verbose("Login failed :(");
                //                //return string.Empty;
                //            }
                //            // ... Read the string.

                //            NameValueCollection qscoll = HttpUtility.ParseQueryString(response.Result.RequestMessage.RequestUri.Query);
                //            if (qscoll.Count > 0)
                //                code = qscoll[0];


                //            //AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(result.Result);
                //            //   var token = JsonConvert.SerializeObject( result.Result);
                //            //access_token = tokenresponse.AccessToken;
                //        }
                //    }
                //}


                //string access_token = string.Empty;
                //using (HttpClient request = new HttpClient())
                //{

                //    string postData = String.Format(StringConstants.AzureActivationUserToken, code);
                //    LogManager.Verbose("Call 3 postdata:" + postData);
                //    // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                //    Task<HttpResponseMessage> response = request.PostAsync((StringConstants.AzureActivateUserStep3), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));


                //    response.Wait();

                //    if (response.Result.Content != null)
                //    {
                //        using (HttpContent content = response.Result.Content)
                //        {
                //            Task<string> result = content.ReadAsStringAsync();

                //            AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(result.Result);
                //            //   var token = JsonConvert.SerializeObject( result.Result);
                //            access_token = tokenresponse.AccessToken;
                //        }
                //    }
                //}


                //using (HttpClient request = new HttpClient())
                //{
                //    LogManager.Verbose("Call 3 url: " + String.Format(StringConstants.AzureActivateUserStep4, upn));
                //    request.DefaultRequestHeaders.Add("authorization", "bearer " + access_token);
                //    Task<HttpResponseMessage> response = request.GetAsync(String.Format(StringConstants.AzureActivateUserStep4, upn));
                //    response.Wait();

                //    if (response.Result.Content != null)
                //    {
                //        using (HttpContent content = response.Result.Content)
                //        {
                //            Task<string> result = content.ReadAsStringAsync();
                //            MysiteResponse tokenresponse = JsonConvert.DeserializeObject<MysiteResponse>(result.Result);
                //            string rootSiteURL = tokenresponse.value.FirstOrDefault(u => (u.entityKey.ToLower().Contains(StringConstants.rootUrlFinder))).serviceResourceId;

                //            string rootSitedocLibUrl = rootSiteURL.EndsWith("/") ? rootSiteURL + "Shared Documents" : rootSiteURL + "/Shared Documents";

                //            //as this is going to be needed at many places, we will save it 
                //            DriveManager.rootSiteUrl = rootSiteURL;

                //            if (!string.IsNullOrEmpty(rootSiteURL))
                //            {
                //                Uri url = new Uri(rootSiteURL);
                //                tenancyUniqueName = url.Host.ToLower().Replace(StringConstants.rootUrltobeRemoved, "");
                //            }

                //            //Set onedrive host
                //            DriveManager.oneDriveHostSiteUrl = "https://" + tenancyUniqueName + "-my.sharepoint.com";
                //        }
                //    }
                //}

                //LogManager.Verbose("Call 4 finished");
                //LogManager.Verbose("tenancy name: " + tenancyUniqueName + StringConstants.rootUrltobeReplacedWith);
                //tenancyUniqueName += tenancyUniqueName + StringConstants.rootUrltobeReplacedWith;
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

        public static string getHPGId(string response)
        {
            int indexofCanary = response.IndexOf("\"hpgid\":") + 8;
            int endIndex = response.IndexOf(",", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }
    }
}