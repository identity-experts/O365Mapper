using _365Drive.Office365.GetTenancyURL;
using _365Drive.Office365.GetTenancyURL.CookieManager;
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
                else if (DriveManager.FederationType == FedType.ADFS)
                {
                    TenancyName = adfsConnectTenancy(upn, password);
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
                Task<HttpResponseMessage> msLoginPostResponse = HttpClientHelper.PostAsyncFullResponse(StringConstants.AzureActivateUserStep2, MSloginpostData, "application/x-www-form-urlencoded", authorizeCookies, MSloginpostHeader);
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
                string adfsPostUrl = DriveManager.ADFSAuthURL+ MSctx;
                string adfsPostBody = string.Format(StringConstants.AdfsPostBody, upn, password);
                Task<string> adfsloginPostBodyResult = HttpClientHelper.PostAsync(adfsPostUrl, adfsPostBody, "application/x-www-form-urlencoded", authorizeCookies);
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
                string strrstPostBody = string.Format(StringConstants.ADFSrstPostBody,LicenseManager.encode(rst),MSctx);
                Task<HttpResponseMessage> ADFSrstPostResult = HttpClientHelper.PostAsyncFullResponse(StringConstants.ADFSrstPost, strrstPostBody, "application/x-www-form-urlencoded", authorizeCookies,new NameValueCollection());
                ADFSrstPostResult.Wait();

                NameValueCollection pollEndRedirect = HttpUtility.ParseQueryString(ADFSrstPostResult.Result.RequestMessage.RequestUri.Query);
                if (pollEndRedirect.Count > 0)
                    authCode = pollEndRedirect[0];

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