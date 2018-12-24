using _365Drive.Office365.CloudConnector;
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

namespace _365Drive.Office365.GetTenancyURL.CookieManager
{
    public class AADConnectCookieManager
    {
        #region Properties

        readonly string _username;
        readonly string _password;
        readonly bool _useRtfa;
        readonly Uri _host;

        public static CookieContainer _cachedCookieContainer = null;
        public static DateTime _expires = DateTime.MinValue;

        #endregion

        public AADConnectCookieManager(string host, string username, string password)
            : this(new Uri(host), username, password)
        {

        }
        public AADConnectCookieManager(Uri host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            _useRtfa = true;
        }


        /// <summary>
        /// delete all cached cookies
        /// </summary>
        public static void signout()
        {
            _cachedCookieContainer = null;
            _expires = DateTime.MinValue;
        }

        class MsoCookies
        {
            //IPUT
            public string build { get; set; }
            public string estsauthpersistent { get; set; }

            public string FedAuth { get; set; }
            public string rtFa { get; set; }
            public DateTime Expires { get; set; }
            public Uri Host { get; set; }
        }

        public CookieContainer getCookieContainer()
        {
            CookieContainer cc = new CookieContainer();
            MsoCookies cookies;
            try
            {
                if (!Utility.ready())
                    return null;


                //Only retrieve cookies when its NULL or expired. otherwise get it from cache.
                if (_cachedCookieContainer == null || DateTime.Now > _expires)
                {
                    // Get the SAML tokens from SPO STS (via MSO STS) using fed auth passive approach
                    cookies = getAzureAADConnectCookies();

                    //retrieving cookies from here
                    if (!string.IsNullOrEmpty(cookies.FedAuth))
                    {

                        // Create cookie collection with the SAML token                    
                        _expires = cookies.Expires;


                        // Set the FedAuth cookie
                        Cookie samlAuth = new Cookie("FedAuth", cookies.FedAuth)
                        {
                            Expires = cookies.Expires,
                            Path = "/",
                            Secure = cookies.Host.Scheme == "https",
                            HttpOnly = true,
                            Domain = cookies.Host.Host
                        };
                        cc.Add(samlAuth);


                        if (_useRtfa)
                        {
                            Cookie rtFa = new Cookie("rtFA", cookies.rtFa)
                            {
                                Expires = cookies.Expires,
                                Path = "/",
                                Secure = cookies.Host.Scheme == "https",
                                HttpOnly = true,
                                Domain = cookies.Host.Host
                            };
                            cc.Add(rtFa);
                        }

                        //IPUT
                        // Set the buid cookie
                        Cookie buid = new Cookie("buid", cookies.build)
                        {
                            Expires = cookies.Expires,
                            Path = "/",
                            Secure = cookies.Host.Scheme == "https",
                            HttpOnly = true,
                            Domain = cookies.Host.Host
                        };
                        cc.Add(buid);

                        // Set the buid cookie
                        Cookie estsauthpersistent = new Cookie("ESTSAUTHPERSISTENT", cookies.estsauthpersistent)
                        {
                            Expires = cookies.Expires,
                            Path = "/",
                            Secure = cookies.Host.Scheme == "https",
                            HttpOnly = true,
                            Domain = cookies.Host.Host
                        };
                        cc.Add(estsauthpersistent);

                        _cachedCookieContainer = cc;
                        return cc;
                    }
                    return null;

                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return _cachedCookieContainer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        MsoCookies getAzureAADConnectCookies()
        {
            try
            {
                //are we ready?
                if (!Utility.ready())
                    return null;

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
                string nonce = string.Empty;
                string code = string.Empty, id_token = string.Empty, session_state = string.Empty, state = string.Empty, correlation_id = string.Empty;
                string desktopSsoConfig = string.Empty;
                string dssoToken = string.Empty;

                string AuthrequestUrl = string.Format(StringConstants.AuthenticateRequestUrl, _host);
                CookieContainer AuthrequestCookies = new CookieContainer();
                CookieContainer wreplyCookies = new CookieContainer();

                //render header
                NameValueCollection loginPostHeader = new NameValueCollection();
                loginPostHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36");
                loginPostHeader.Add("Accept", "*/*");


                Task<HttpResponseMessage> AuthrequestResponse = HttpClientHelper.GetAsyncFullResponse(AuthrequestUrl, wreplyCookies, true, loginPostHeader);
                AuthrequestResponse.Wait();

                //we need same cookies now
                AuthrequestCookies = wreplyCookies;

                Task<string> response = AuthrequestResponse.Result.Content.ReadAsStringAsync();
                response.Wait();
                authorizeCall = response.Result;

                NameValueCollection qscoll = HttpUtility.ParseQueryString(AuthrequestResponse.Result.RequestMessage.RequestUri.Query);
                if (qscoll.Count > 0)
                    nonce = qscoll["nonce"];

                CookieContainer authorizeCookies = new CookieContainer();
                ///Making call 1, this will be an initial call to microsoftonline to get the apiCananary, flow and ctx values
                /// 
                //  string redirectURI = new Uri(_host.Scheme + "://" + _host.Host + "/");
                // LogManager.Verbose("Get request: " + String.Format(StringConstants.AzureActivateUserStep1, _username, StringConstants.sharepointClientID, _host, _host));
                //string call1Url = String.Format(StringConstants.AzureActivateUserStep1, _username, StringConstants.sharepointClientID, "http://office.microsoft.com/sharepoint", _host);
                string call1Url = String.Format(StringConstants.getAADCookieStep1, _host, nonce);

                //Task<string> call1Result = HttpClientHelper.GetAsync(call1Url, AuthrequestCookies, loginPostHeader);
                //call1Result.Wait();
                //authorizeCall = call1Result.Result;

                //retrieve the ctx, flow and cannary
                LoginConfig config = _365DriveTenancyURL.renderConfig(authorizeCall);
                Authorizectx = config.sCtx;
                Authorizeflowtoken = config.sFT;
                authorizeCanary = config.canary;


                ///get other values of call 1
                AuthorizeCanaryapi = _365DriveTenancyURL.getapiCanary(authorizeCall);
                //authorizeCanary = _365DriveTenancyURL.getCanary2(authorizeCall);
                AuthorizeclientRequestID = _365DriveTenancyURL.getClientRequest(authorizeCall);
                Authorizehpgact = _365DriveTenancyURL.getHPGact(authorizeCall);
                Authorizehpgid = _365DriveTenancyURL.getHPGId(authorizeCall);
                LogManager.Verbose("Call 1 finished. output: ctx=" + Authorizectx + " flow=" + Authorizeflowtoken + " cookie count: " + authorizeCookies.Count.ToString());

                //is SSO
                string isSSO = RegistryManager.Get(RegistryKeys.AutoSSO);

                //if SSO, Its a shortcut, lets try it and try to finish from here
                if (!string.IsNullOrEmpty(isSSO) && isSSO == "1")
                {
                    //desktopSsoConfig = getDesktopSsoConfig(authorizeCall);
                    dSSOConfig DSSOConfig = _365DriveTenancyURL.renderdSSOConfig(authorizeCall);
                    desktopSsoConfig = string.Format(DSSOConfig.desktopSsoConfig.iwaEndpointUrlFormat, DriveManager.AADSSODomainName) + "&client-request-id=" + AuthorizeclientRequestID;

                    //render header
                    NameValueCollection AADConnectSSOGetHeader = new NameValueCollection();
                    AADConnectSSOGetHeader.Add("Referrer", call1Url);
                    AADConnectSSOGetHeader.Add("Origin", "https://login.microsoftonline.com");
                    AADConnectSSOGetHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                    AADConnectSSOGetHeader.Add("Accept", "*/*");


                    Task<string> AADConnectloginPostBodyResult = HttpClientHelper.GetAsync(desktopSsoConfig, AADConnectSSOGetHeader, true);
                    AADConnectloginPostBodyResult.Wait();


                    LogManager.Info("AAD SSO WIA resposne:" + AADConnectloginPostBodyResult.Result);
                    //retrieving rst
                    dssoToken = AADConnectloginPostBodyResult.Result;
                }


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


                foreach (Cookie cookie in AuthrequestCookies.GetCookies(new Uri(StringConstants.dssoPoll)))
                {
                    authorizeCookies.Add(cookie);
                }

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

                string msLoginPostData = String.Format(StringConstants.loginPostData, _username, _password, Authorizectx, Authorizeflowtoken, LicenseManager.encode(authorizeCanary), dssoToken);
                Task<String> postCalresponse = HttpClientHelper.PostAsync((StringConstants.AADConnectCookieloginPost), msLoginPostData, "application/x-www-form-urlencoded", msLoginPostCookies);
                postCalresponse.Wait();


                string outputControls = string.Empty;
                string MSKMSIPostData = string.Empty;
                LoginConfig msPostConfig;
                Task<string> msKMSIPost;
                msPostConfig = _365DriveTenancyURL.renderConfig(postCalresponse.Result);
                if (msPostConfig == null)
                {
                    outputControls = postCalresponse.Result;
                }
                else
                {

                    Authorizectx = msPostConfig.sCtx;
                    Authorizeflowtoken = msPostConfig.sFT;
                    authorizeCanary = msPostConfig.canary;

                    //getting ready for KMSI post
                    MSKMSIPostData = String.Format(StringConstants.KMSIPost, Authorizectx, Authorizeflowtoken, LicenseManager.encode(authorizeCanary));
                    msKMSIPost = HttpClientHelper.PostAsync(StringConstants.loginKMSI, MSKMSIPostData, "application/x-www-form-urlencoded", AuthrequestCookies);
                    msKMSIPost.Wait();
                    outputControls = msKMSIPost.Result;
                }

                CQ msPOSTcq = CQ.Create(outputControls);
                var msPOSTcqItems = msPOSTcq["input"];

                foreach (var li in msPOSTcqItems)
                {
                    if (li.Name == "code")
                    {
                        code = li.Value;
                    }
                    if (li.Name == "id_token")
                    {
                        id_token = li.Value;
                    }
                    if (li.Name == "session_state")
                    {
                        session_state = li.Value;
                    }
                    if (li.Name == "state")
                    {
                        state = li.Value;
                    }
                    if (li.Name == "correlation_id")
                    {
                        correlation_id = li.Value;
                    }
                }

                if (string.IsNullOrEmpty(code))
                {
                    string isPollingRequired = _365DriveTenancyURL.getpolingRequired(outputControls);
                    bool pollingRequired = false;
                    bool.TryParse(isPollingRequired, out pollingRequired);

                    if (pollingRequired)
                    {
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

                        msPostHpgact = _365DriveTenancyURL.getHPGact(postCalresponse.Result);
                        msPostHpgid = _365DriveTenancyURL.getHPGId(postCalresponse.Result);
                        msPostCanary = _365DriveTenancyURL.getapiCanary(postCalresponse.Result);
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
                        pollEndHeader.Add("Referrer", "https://login.microsoftonline.com/1d962d68-d448-4434-b62d-5660971874c4/login");
                        pollEndHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                        pollEndHeader.Add("Accept", "application/json");
                        //pollEndHeader.Add("X-Requested-With", "XMLHttpRequest");
                        pollEndHeader.Add("DNT", "1");

                        string pollEndData = String.Format(StringConstants.AADPollEndBody, pollStartFlowToken, pollStartctx);

                        //poll start heading
                        Task<String> pollEndResponse = HttpClientHelper.PostAsync((StringConstants.AADPollEnd), pollEndData, "application/x-www-form-urlencoded", pollEndCookieContainer, pollEndHeader);
                        pollEndResponse.Wait();

                        msPostConfig = _365DriveTenancyURL.renderConfig(pollEndResponse.Result);
                        Authorizectx = msPostConfig.sCtx;
                        Authorizeflowtoken = msPostConfig.sFT;
                        authorizeCanary = msPostConfig.canary;

                        if (string.IsNullOrEmpty(Authorizectx))
                        {
                            outputControls = postCalresponse.Result;
                        }
                        else
                        {
                            //getting ready for KMSI post
                            MSKMSIPostData = String.Format(StringConstants.KMSIPost, Authorizectx, Authorizeflowtoken, LicenseManager.encode(authorizeCanary));
                            msKMSIPost = HttpClientHelper.PostAsync(StringConstants.loginKMSI, MSKMSIPostData, "application/x-www-form-urlencoded", AuthrequestCookies);
                            msKMSIPost.Wait();
                            outputControls = msKMSIPost.Result;
                        }

                        //var pollendResponse = await pollEndResponse.Content.ReadAsStringAsync();
                        CQ pollEndResponsecQ = CQ.Create(outputControls);
                        var pollEndResponses = pollEndResponsecQ["input"];

                        foreach (var li in pollEndResponses)
                        {
                            if (li.Name == "code")
                            {
                                code = li.Value;
                            }
                            if (li.Name == "id_token")
                            {
                                id_token = li.Value;
                            }
                            if (li.Name == "session_state")
                            {
                                session_state = li.Value;
                            }
                            if (li.Name == "state")
                            {
                                state = li.Value;
                            }
                            if (li.Name == "correlation_id")
                            {
                                correlation_id = li.Value;
                            }
                        }

                        ///Check for MFA
                        if (string.IsNullOrEmpty(code))
                        {
                            string postResponse = GlobalCookieManager.retrieveCodeFromMFA(outputControls, AuthrequestCookies);
                            pollEndResponsecQ = CQ.Create(postResponse);
                            pollEndResponses = pollEndResponsecQ["input"];
                            foreach (var li in pollEndResponses)
                            {
                                if (li.Name == "code")
                                {
                                    code = li.Value;
                                }
                                if (li.Name == "id_token")
                                {
                                    id_token = li.Value;
                                }
                                if (li.Name == "state")
                                {
                                    state = li.Value;
                                }
                                if (li.Name == "correlation_id")
                                {
                                    correlation_id = li.Value;
                                }
                                if (li.Name == "session_state")
                                {
                                    session_state = li.Value;
                                }
                            }
                        }
                    }
                    else
                    {
                        string postResponse = GlobalCookieManager.retrieveCodeFromMFA(outputControls, AuthrequestCookies);
                        var pollEndResponsecQ = CQ.Create(postResponse);
                        var pollEndResponses = pollEndResponsecQ["input"];
                        foreach (var li in pollEndResponses)
                        {
                            if (li.Name == "code")
                            {
                                code = li.Value;
                            }
                            if (li.Name == "id_token")
                            {
                                id_token = li.Value;
                            }
                            if (li.Name == "state")
                            {
                                state = li.Value;
                            }
                            if (li.Name == "correlation_id")
                            {
                                correlation_id = li.Value;
                            }
                            if (li.Name == "session_state")
                            {
                                session_state = li.Value;
                            }
                        }
                    }
                }

                string postCodeBody = String.Format(StringConstants.postCodeBody, code, id_token, state, session_state, correlation_id);
                Task<String> postCodeResponse = HttpClientHelper.PostAsync(String.Format(StringConstants.postCodeUrl, _host), postCodeBody, "application/x-www-form-urlencoded", wreplyCookies, false);
                postCodeResponse.Wait();

                LogManager.Verbose("cookies retrieved. Code: " + authCode);
                MsoCookies ret = new MsoCookies();
                CookieCollection spCookies = wreplyCookies.GetCookies(_host);

                ret.FedAuth = spCookies["FedAuth"].Value;
                ret.rtFa = spCookies["rtFa"].Value;
                ret.Host = _host;

                //IPUT
                try
                {
                    ret.build = AuthrequestCookies.GetCookies(new Uri("https://login.microsoftonline.com"))["buid"].Value;
                    ret.estsauthpersistent = AuthrequestCookies.GetCookies(new Uri("https://login.microsoftonline.com"))["ESTSAUTHPERSISTENT"].Value;
                }
                catch
                {
                    // do nothing! its an edge case for IPUT but its crashing for everyone now
                }
                //setting expiry
                ret.Expires = DateTime.Now.AddHours(Constants.AuthcookieExpiryHours);

                return ret;
            }

            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return null;
        }
        public CookieContainer CookieContainer
        {
            get
            {
                if (_cachedCookieContainer == null || DateTime.Now > _expires)
                {
                    return getCookieContainer();
                }
                return _cachedCookieContainer;
            }
        }
    }
}
