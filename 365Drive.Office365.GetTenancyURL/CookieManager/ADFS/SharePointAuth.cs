using _365Drive.Office365.CloudConnector;
using CsQuery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace _365Drive.Office365.GetTenancyURL.CookieManager
{
    public class ADFSAuth
    {
        SamlSecurityToken stsAuthToken;
        Uri spSiteUrl;
        string username;
        string password;
        Uri adfsIntegratedAuthUrl;
        Uri adfsAuthUrl;
        bool useIntegratedWindowsAuth;
        public static CookieContainer _cachedCookieContainer = null;
        public static DateTime _expires = DateTime.MinValue;

        const string msoStsUrl = "https://login.microsoftonline.com/extSTS.srf";
        const string msoLoginUrl = "https://login.microsoftonline.com/login.srf";
        const string msoHrdUrl = "https://login.microsoftonline.com/GetUserRealm.srf";
        const string wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        const string wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        const string wst = "http://schemas.xmlsoap.org/ws/2005/02/trust";
        const string saml = "urn:oasis:names:tc:SAML:1.0:assertion";
        const string spowssigninUri = "_forms/default.aspx?wa=wsignin1.0";


        /// <summary>
        /// delete all cached cookies
        /// </summary>
        public static void signout()
        {
            _cachedCookieContainer = null;
            _expires = DateTime.MinValue;
        }

        public ADFSAuth(Uri spSiteUrl, string username, string password, bool useIntegratedWindowsAuth)
        {
            this.spSiteUrl = spSiteUrl;
            this.username = username;
            this.password = password;
            this.useIntegratedWindowsAuth = useIntegratedWindowsAuth;

            stsAuthToken = new SamlSecurityToken();
        }

        public ADFSAuth(string username, string password, bool useIntegratedWindowsAuth)
        {
            this.username = username;
            this.password = password;
            this.useIntegratedWindowsAuth = useIntegratedWindowsAuth;

            stsAuthToken = new SamlSecurityToken();
        }


        public CookieContainer getCookieContainer()
        {
            if (_cachedCookieContainer == null || DateTime.Now > _expires)
            {
                //CookieContainer cookieContainer = GetCookieContainer();
                CookieContainer cookieContainer = getIECookies();

                if (cookieContainer != null && cookieContainer.Count > 0)
                {
                    var cookies = from Cookie cookie in cookieContainer.GetCookies(spSiteUrl)
                                  where cookie.Name == "FedAuth"
                                  select cookie;

                    if (cookies.Any())
                    {
                        _cachedCookieContainer = cookieContainer;
                        _expires = DateTime.Now.AddHours(Constants.AuthcookieExpiryHours);
                    }
                }
                else
                {
                    return null;
                }
            }
            return _cachedCookieContainer;
        }

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
        /// Retrieve cookies in new way
        /// </summary>
        /// <returns></returns>
        public CookieContainer getIECookies()
        {



            CookieContainer ret = new CookieContainer();
            try
            {
                //are we ready?
                if (!Utility.ready())
                    return null;

                //get nonse
                string authorizeCall = string.Empty, Authorizectx = string.Empty, Authorizeflowtoken = string.Empty, authorizeCanary = string.Empty, nonce = string.Empty, clientRequestId = string.Empty;
                string AuthrequestUrl = string.Format(StringConstants.AuthenticateRequestUrl, spSiteUrl.ToString());
                CookieContainer AuthrequestCookies = new CookieContainer();
                Task<HttpResponseMessage> AuthrequestResponse = HttpClientHelper.GetAsyncFullResponse(AuthrequestUrl, AuthrequestCookies, true);
                AuthrequestResponse.Wait();

                NameValueCollection qscoll = HttpUtility.ParseQueryString(AuthrequestResponse.Result.RequestMessage.RequestUri.Query);
                if (qscoll.Count > 0)
                {
                    nonce = qscoll["nonce"];
                    clientRequestId = qscoll["client-request-id"];
                }


                //lets again start as we need more details (I hope)
                //MS call 1 to get 
                //post to MS
                string ADFSMSCall1 = string.Format(StringConstants.MSADFSGetRST, clientRequestId);
                Task<string> ADFSMSCall1Result = HttpClientHelper.PostAsync(StringConstants.ADFSrstPost, ADFSMSCall1, "application/x-www-form-urlencoded", AuthrequestCookies);
                ADFSMSCall1Result.Wait();

                //get the ctx which is RST
                string ADFSMScall1CTX = string.Empty;
                CQ ADFSMScall1Parser = CQ.Create(ADFSMSCall1Result.Result);
                var ADFSMScall1ParserInputs = ADFSMScall1Parser["input"];
                foreach (var li in ADFSMScall1ParserInputs)
                {
                    if (li.Name == "ctx")
                    {
                        if (!string.IsNullOrEmpty(li.Value))
                            ADFSMScall1CTX = li.Value;

                    }
                }

                var Wreply = spSiteUrl.GetLeftPart(UriPartial.Authority) + "/_forms/default.aspx";

                string WindowsoAuthCallUrl = String.Format(StringConstants.getCloudCookieStep0, LicenseManager.encode(Wreply), nonce, clientRequestId);
                Task<string> call0Result = HttpClientHelper.GetAsync(WindowsoAuthCallUrl, AuthrequestCookies);
                call0Result.Wait();

                //first call to get flow token, ctx and canary
                string MSOnlineoAuthCallUrl = String.Format(StringConstants.getCloudCookieStep1, LicenseManager.encode(Wreply), nonce, clientRequestId);
                Task<string> call1Result = HttpClientHelper.GetAsync(MSOnlineoAuthCallUrl, AuthrequestCookies);
                call1Result.Wait();


                authorizeCall = call1Result.Result;

                ///Fetch the ctx and flow token and canary
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
                authorizeCanary = _365DriveTenancyURL.getCanary2(authorizeCall);


                //get user realM
                string ADFSRealM = String.Format(StringConstants.ADFSRealM, LicenseManager.encode(username), Authorizectx);
                Task<string> ADFSRealMResult = HttpClientHelper.GetAsync(ADFSRealM, AuthrequestCookies);
                ADFSRealMResult.Wait();

                RealM ADFSRealMresponse = JsonConvert.DeserializeObject<RealM>(ADFSRealMResult.Result);

                //get the ADFS post URL
                string strADFSPostUrl = ADFSRealMresponse.AuthURL;


                //is SSO
                string isSSO = RegistryManager.Get(RegistryKeys.SSO);

                CQ adfsPostResponse = null;
                //if SSO, then do different set of calls
                if (!string.IsNullOrEmpty(isSSO) && isSSO == "1")
                {
                    LogManager.Info("SSO");
                    string NoWAAuthstrADFSPostUrl = RemoveQueryStringByKey(strADFSPostUrl, "wauth");
                    Uri ADFSAuthUri = new Uri(NoWAAuthstrADFSPostUrl);
                    string WIAUrl = String.Format("{0}{1}{2}{3}{4}{5}", ADFSAuthUri.Scheme, Uri.SchemeDelimiter, ADFSAuthUri.Authority, ADFSAuthUri.AbsolutePath, ADFSAuthUri.AbsolutePath.EndsWith("/") ? "wia" : "/wia", ADFSAuthUri.Query);

                    //ADFS GET URI
                    LogManager.Info("ADFS WIA get:" + WIAUrl);

                    //render header
                    NameValueCollection ADFSGetHeader = new NameValueCollection();
                    ADFSGetHeader.Add("Referrer", MSOnlineoAuthCallUrl);
                    ADFSGetHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                    ADFSGetHeader.Add("Accept", "text/html, application/xhtml+xml, image/jxr, */*");

                    Task<string> adfsloginPostBodyResult = HttpClientHelper.GetAsync(WIAUrl, ADFSGetHeader, true);
                    adfsloginPostBodyResult.Wait();


                    LogManager.Info("ADFS WIA resposne:" + adfsloginPostBodyResult.Result);
                    //retrieving rst
                    adfsPostResponse = CQ.Create(adfsloginPostBodyResult.Result);
                }
                else
                {
                    //post to ADFS now
                    string adfsPostBody = string.Format(StringConstants.AdfsPostBody, username, password);
                    Task<string> adfsloginPostBodyResult = HttpClientHelper.PostAsync(strADFSPostUrl, adfsPostBody, "application/x-www-form-urlencoded", AuthrequestCookies);
                    adfsloginPostBodyResult.Wait();

                    //retrieving rst
                    adfsPostResponse = CQ.Create(adfsloginPostBodyResult.Result);
                }





                //retrieve code, idtoken 
                string code = string.Empty, id_token = string.Empty, state = string.Empty, session_state = string.Empty;

                //retrieving rst
                string rst = string.Empty;
                string wctx = string.Empty;
                //CQ adfsPostResponse = CQ.Create(adfsloginPostBodyResult.Result);
                var adfsPostResponseItems = adfsPostResponse["input"];
                foreach (var li in adfsPostResponseItems)
                {
                    if (li.Name == "wresult")
                    {
                        if (!string.IsNullOrEmpty(li.Value))
                            rst = li.Value;
                    }
                }


                //ADFS + MFA (MS case)
                if (string.IsNullOrEmpty(rst))
                {

                    return getMSIECookies();


                    //get the ADFS URL using realM
                    string ADFSMSCall2RealM = String.Format(StringConstants.ADFSRealM, LicenseManager.encode(username), Authorizectx);
                    Task<string> ADFSMSCall2RealMResult = HttpClientHelper.GetAsync(ADFSMSCall2RealM, AuthrequestCookies);
                    ADFSMSCall2RealMResult.Wait();


                    var adfsPostResponseAnchors = adfsPostResponse["a"];
                    foreach (var a in adfsPostResponseAnchors)
                    {
                        if (a.Id == "WindowsAzureMultiFactorAuthentication")
                        {

                            //its a case of microsoft!

                            if (GlobalCookieManager.MFAUserConsent())
                            {
                                //Initiate the MFA
                                string adfsMFAPostBody = StringConstants.AdfsPhoneMFAPostBody;
                                Task<string> adfsMFAPostBodyResult = HttpClientHelper.PostAsync(strADFSPostUrl, adfsMFAPostBody, "application/x-www-form-urlencoded", AuthrequestCookies);
                                adfsMFAPostBodyResult.Wait();

                                var context = string.Empty;
                                //get the context value
                                CQ adfsMFAPostBodyResponse = CQ.Create(adfsMFAPostBodyResult.Result);
                                var adfsMFAPostBodyResponseItems = adfsMFAPostBodyResponse["input"];
                                foreach (var li in adfsMFAPostBodyResponseItems)
                                {
                                    if (li.Name.ToLower() == "context")
                                    {
                                        if (!string.IsNullOrEmpty(li.Value))
                                            context = li.Value;
                                    }
                                }
                                //string verify = GlobalCookieManager.PromptMFA("phoneappnotification");
                                //if (verify.ToLower() == "true")
                                //The call is synchronized with auth 
                                if (true)
                                {
                                    //Lets check
                                    string adfsMFADoneBody = string.Format(StringConstants.AdfsPhoneMFAPostDoneBody, context);
                                    Task<string> adfsMFAPostDoneBodyResult = HttpClientHelper.PostAsync(strADFSPostUrl, adfsMFADoneBody, "application/x-www-form-urlencoded", AuthrequestCookies);
                                    adfsMFAPostDoneBodyResult.Wait();

                                    CQ MFADoneBodyResponseParser = CQ.Create(adfsMFAPostDoneBodyResult.Result);
                                    var MFADoneBodyResponseParserInputs = MFADoneBodyResponseParser["input"];
                                    foreach (var li in MFADoneBodyResponseParserInputs)
                                    {
                                        if (li.Name == "wresult")
                                        {
                                            if (!string.IsNullOrEmpty(li.Value))
                                                rst = li.Value;
                                        }
                                        if (li.Name == "wctx")
                                        {
                                            if (!string.IsNullOrEmpty(li.Value))
                                                wctx = li.Value;

                                        }
                                    }

                                    NameValueCollection MSstrrstPostHeader = new NameValueCollection();
                                    MSstrrstPostHeader.Add("Origin", "https://msft.sts.microsoft.com");
                                    MSstrrstPostHeader.Add("Upgrade-Insecure-Requests", "1");
                                    MSstrrstPostHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                                    MSstrrstPostHeader.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                                    MSstrrstPostHeader.Add("X-Requested-With", "XMLHttpRequest");
                                    MSstrrstPostHeader.Add("Referer", strADFSPostUrl);

                                    string MSstrrstPostBody = string.Format(StringConstants.MSADFSrstPostBody, LicenseManager.encode(rst), LicenseManager.encode(wctx));
                                    Task<String> MSADFSrstPostResult = HttpClientHelper.PostAsync(StringConstants.ADFSrstPost, MSstrrstPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, MSstrrstPostHeader);
                                    MSADFSrstPostResult.Wait();


                                    //get the token "t"
                                    string t = string.Empty;
                                    CQ MSpostBodyResponseParser = CQ.Create(MSADFSrstPostResult.Result);
                                    var MSpostBodyResponseInputs = MSpostBodyResponseParser["input"];
                                    foreach (var li in MSpostBodyResponseInputs)
                                    {
                                        if (li.Name == "t")
                                        {
                                            t = li.Value;
                                        }

                                    }

                                    //get all code, state...
                                    string MSADCodeUrl = string.Format(StringConstants.MSADGetCodeandTokenCall, clientRequestId);
                                    Task<string> MSADCodeResponse = HttpClientHelper.GetAsync(MSADCodeUrl, AuthrequestCookies);
                                    MSADCodeResponse.Wait();


                                    CQ MSADCodeResponseParser = CQ.Create(MSADCodeResponse.Result);
                                    var MSADCodeResponseInputs = MSADCodeResponseParser["input"];
                                    foreach (var li in MSADCodeResponseInputs)
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
                                        if (li.Name == "session_state")
                                        {
                                            session_state = li.Value;
                                        }
                                    }

                                    goto codereceived;
                                }

                            }

                        }
                    }
                }




                //post rst to microsoft
                string strrstPostBody = string.Format(StringConstants.ADFSrstPostBody, LicenseManager.encode(rst), Authorizectx);
                Task<String> ADFSrstPostResult = HttpClientHelper.PostAsync(StringConstants.ADFSrstPost, strrstPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, new NameValueCollection());
                ADFSrstPostResult.Wait();

                CQ postBodyResponseParser = CQ.Create(ADFSrstPostResult.Result);
                var postBodyResponseInputs = postBodyResponseParser["input"];
                foreach (var li in postBodyResponseInputs)
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
                    if (li.Name == "session_state")
                    {
                        session_state = li.Value;
                    }
                }

                ///Check for MFA
                if (string.IsNullOrEmpty(code))
                {
                    string postResponse = GlobalCookieManager.retrieveCodeFromMFA(ADFSrstPostResult.Result, AuthrequestCookies);
                    postBodyResponseParser = CQ.Create(postResponse);
                    postBodyResponseInputs = postBodyResponseParser["input"];
                    foreach (var li in postBodyResponseInputs)
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
                        if (li.Name == "session_state")
                        {
                            session_state = li.Value;
                        }
                    }
                }

                codereceived:
                //post everyhing to sharepoint
                string SharePointPostBody = string.Format(StringConstants.SharePointFormPost, code, id_token, state, session_state);
                NameValueCollection SharePointPostHeader = new NameValueCollection();
                SharePointPostHeader.Add("Origin", "https://login.microsoftonline.com");
                SharePointPostHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                SharePointPostHeader.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                SharePointPostHeader.Add("X-Requested-With", "XMLHttpRequest");
                SharePointPostHeader.Add("Referer", "https://login.microsoftonline.com/common/login");
                Task<HttpResponseMessage> SharePointPostResult = null;
                try
                {
                    SharePointPostResult = HttpClientHelper.PostAsyncFullResponse(Wreply, SharePointPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, SharePointPostHeader);
                    SharePointPostResult.Wait();
                }
                catch (Exception ex)
                {
                    if (SharePointPostResult.Result.StatusCode == HttpStatusCode.Forbidden)
                    {

                    }
                    else
                    {
                        throw ex;
                    }
                }
                foreach (Cookie SPCookie in AuthrequestCookies.GetCookies(new Uri(Wreply)))
                {
                    ret.Add(SPCookie);
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            return ret;
        }


        /// <summary>
        /// Get cookies for Microsoft AUTH 
        /// </summary>
        /// <returns></returns>
        public CookieContainer getMSIECookies()
        {
            CookieContainer ret = new CookieContainer();
            try
            {
                //are we ready?
                if (!Utility.ready())
                    return null;

                //get nonse
                string authorizeCall = string.Empty, Authorizectx = string.Empty, Authorizeflowtoken = string.Empty, authorizeCanary = string.Empty, nonce = string.Empty, clientRequestId = string.Empty;
                string AuthrequestUrl = string.Format(StringConstants.AuthenticateRequestUrl, spSiteUrl.ToString());
                CookieContainer AuthrequestCookies = new CookieContainer();
                Task<HttpResponseMessage> AuthrequestResponse = HttpClientHelper.GetAsyncFullResponse(AuthrequestUrl, AuthrequestCookies, true);
                AuthrequestResponse.Wait();

                NameValueCollection qscoll = HttpUtility.ParseQueryString(AuthrequestResponse.Result.RequestMessage.RequestUri.Query);
                if (qscoll.Count > 0)
                {
                    nonce = qscoll["nonce"];
                    clientRequestId = qscoll["client-request-id"];
                }


                string strADFSPostUrl = AuthrequestResponse.Result.RequestMessage.RequestUri.ToString();

                //lets again start as we need more details (I hope)
                //MS call 1 to get 
                //post to MS
                string ADFSMSCall1 = string.Format(StringConstants.MSADFSGetRST, clientRequestId);
                Task<string> ADFSMSCall1Result = HttpClientHelper.PostAsync(StringConstants.ADFSrstPost, ADFSMSCall1, "application/x-www-form-urlencoded", AuthrequestCookies);
                ADFSMSCall1Result.Wait();

                //get the ctx which is RST
                string ADFSMScall1CTX = string.Empty;
                CQ ADFSMScall1Parser = CQ.Create(ADFSMSCall1Result.Result);
                var ADFSMScall1ParserInputs = ADFSMScall1Parser["input"];
                foreach (var li in ADFSMScall1ParserInputs)
                {
                    if (li.Name == "ctx")
                    {
                        if (!string.IsNullOrEmpty(li.Value))
                            ADFSMScall1CTX = li.Value;

                    }
                }

                var Wreply = spSiteUrl.GetLeftPart(UriPartial.Authority) + "/_forms/default.aspx";

                string WindowsoAuthCallUrl = String.Format(StringConstants.getCloudCookieStep0, LicenseManager.encode(Wreply), nonce, clientRequestId);
                Task<string> call0Result = HttpClientHelper.GetAsync(WindowsoAuthCallUrl, AuthrequestCookies);
                call0Result.Wait();

                //first call to get flow token, ctx and canary
                string MSOnlineoAuthCallUrl = String.Format(StringConstants.getCloudCookieStep1, LicenseManager.encode(Wreply), nonce, clientRequestId);
                Task<HttpResponseMessage> call1Result = HttpClientHelper.GetAsyncFullResponse(MSOnlineoAuthCallUrl, AuthrequestCookies);
                call1Result.Wait();

                //string strADFSPostUrl = call1Result.Result.RequestMessage.RequestUri.Query;
                authorizeCall = call1Result.Result.Content.ReadAsStringAsync().Result;

                ///Fetch the ctx and flow token and canary
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
                authorizeCanary = _365DriveTenancyURL.getCanary2(authorizeCall);


                //get user realM
                string ADFSRealM = String.Format(StringConstants.ADFSRealM, LicenseManager.encode(username), Authorizectx);
                Task<string> ADFSRealMResult = HttpClientHelper.GetAsync(ADFSRealM, AuthrequestCookies);
                ADFSRealMResult.Wait();
                RealM ADFSRealMresponse = JsonConvert.DeserializeObject<RealM>(ADFSRealMResult.Result);

                //get the ADFS post URL
                //string strADFSPostUrl = ADFSRealMresponse.AuthURL;

                //post to ADFS now
                string adfsPostBody = string.Format(StringConstants.AdfsPostBody, username, password);
                Task<string> adfsloginPostBodyResult = HttpClientHelper.PostAsync(strADFSPostUrl, adfsPostBody, "application/x-www-form-urlencoded", AuthrequestCookies);
                adfsloginPostBodyResult.Wait();

                //retrieve code, idtoken 
                string code = string.Empty, id_token = string.Empty, state = string.Empty, session_state = string.Empty;

                //retrieving rst
                string rst = string.Empty;
                string wctx = string.Empty;
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


                //ADFS + MFA (MS case)
                if (string.IsNullOrEmpty(rst))
                {




                    //get the ADFS URL using realM
                    string ADFSMSCall2RealM = String.Format(StringConstants.ADFSRealM, LicenseManager.encode(username), Authorizectx);
                    Task<string> ADFSMSCall2RealMResult = HttpClientHelper.GetAsync(ADFSMSCall2RealM, AuthrequestCookies);
                    ADFSMSCall2RealMResult.Wait();


                    var adfsPostResponseAnchors = adfsPostResponse["a"];
                    foreach (var a in adfsPostResponseAnchors)
                    {
                        if (a.Id == "WindowsAzureMultiFactorAuthentication")
                        {

                            //its a case of microsoft!

                            if (GlobalCookieManager.MFAUserConsent())
                            {
                                //Initiate the MFA
                                string adfsMFAPostBody = StringConstants.AdfsPhoneMFAPostBody;
                                Task<string> adfsMFAPostBodyResult = HttpClientHelper.PostAsync(strADFSPostUrl, adfsMFAPostBody, "application/x-www-form-urlencoded", AuthrequestCookies);
                                adfsMFAPostBodyResult.Wait();

                                var context = string.Empty;
                                //get the context value
                                CQ adfsMFAPostBodyResponse = CQ.Create(adfsMFAPostBodyResult.Result);
                                var adfsMFAPostBodyResponseItems = adfsMFAPostBodyResponse["input"];
                                foreach (var li in adfsMFAPostBodyResponseItems)
                                {
                                    if (li.Name.ToLower() == "context")
                                    {
                                        if (!string.IsNullOrEmpty(li.Value))
                                            context = li.Value;
                                    }
                                }
                                //string verify = GlobalCookieManager.PromptMFA("phoneappnotification");
                                //if (verify.ToLower() == "true")
                                //The call is synchronized with auth 
                                if (true)
                                {
                                    //Lets check
                                    string adfsMFADoneBody = string.Format(StringConstants.AdfsPhoneMFAPostDoneBody, context);
                                    Task<string> adfsMFAPostDoneBodyResult = HttpClientHelper.PostAsync(strADFSPostUrl, adfsMFADoneBody, "application/x-www-form-urlencoded", AuthrequestCookies);
                                    adfsMFAPostDoneBodyResult.Wait();

                                    CQ MFADoneBodyResponseParser = CQ.Create(adfsMFAPostDoneBodyResult.Result);
                                    var MFADoneBodyResponseParserInputs = MFADoneBodyResponseParser["input"];
                                    foreach (var li in MFADoneBodyResponseParserInputs)
                                    {
                                        if (li.Name == "wresult")
                                        {
                                            if (!string.IsNullOrEmpty(li.Value))
                                                rst = li.Value;
                                        }
                                        if (li.Name == "wctx")
                                        {
                                            if (!string.IsNullOrEmpty(li.Value))
                                                wctx = li.Value;

                                        }
                                    }

                                    NameValueCollection MSstrrstPostHeader = new NameValueCollection();
                                    MSstrrstPostHeader.Add("Origin", "https://msft.sts.microsoft.com");
                                    MSstrrstPostHeader.Add("Upgrade-Insecure-Requests", "1");
                                    MSstrrstPostHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                                    MSstrrstPostHeader.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                                    MSstrrstPostHeader.Add("X-Requested-With", "XMLHttpRequest");
                                    MSstrrstPostHeader.Add("Referer", strADFSPostUrl);

                                    string MSstrrstPostBody = string.Format(StringConstants.MSADFSrstPostBody, LicenseManager.encode(rst), LicenseManager.encode(wctx));
                                    Task<String> MSADFSrstPostResult = HttpClientHelper.PostAsync(StringConstants.ADFSrstPost, MSstrrstPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, MSstrrstPostHeader);
                                    MSADFSrstPostResult.Wait();


                                    ////get the token "t"
                                    //string t = string.Empty;
                                    //CQ MSpostBodyResponseParser = CQ.Create(MSADFSrstPostResult.Result);
                                    //var MSpostBodyResponseInputs = MSpostBodyResponseParser["input"];
                                    //foreach (var li in MSpostBodyResponseInputs)
                                    //{
                                    //    if (li.Name == "t")
                                    //    {
                                    //        t = li.Value;
                                    //    }

                                    //}

                                    ////get all code, state...
                                    //string MSADCodeUrl = string.Format(StringConstants.MSADGetCodeandTokenCall, clientRequestId);
                                    //Task<string> MSADCodeResponse = HttpClientHelper.GetAsync(MSADCodeUrl, AuthrequestCookies);
                                    //MSADCodeResponse.Wait();


                                    CQ MSADCodeResponseParser = CQ.Create(MSADFSrstPostResult.Result);
                                    var MSADCodeResponseInputs = MSADCodeResponseParser["input"];
                                    foreach (var li in MSADCodeResponseInputs)
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
                                        if (li.Name == "session_state")
                                        {
                                            session_state = li.Value;
                                        }
                                    }

                                    goto codereceived;
                                }

                            }

                        }
                    }
                }




                //post rst to microsoft
                string strrstPostBody = string.Format(StringConstants.ADFSrstPostBody, LicenseManager.encode(rst), Authorizectx);
                Task<String> ADFSrstPostResult = HttpClientHelper.PostAsync(StringConstants.ADFSrstPost, strrstPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, new NameValueCollection());
                ADFSrstPostResult.Wait();

                CQ postBodyResponseParser = CQ.Create(ADFSrstPostResult.Result);
                var postBodyResponseInputs = postBodyResponseParser["input"];
                foreach (var li in postBodyResponseInputs)
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
                    if (li.Name == "session_state")
                    {
                        session_state = li.Value;
                    }
                }

                ///Check for MFA
                if (string.IsNullOrEmpty(code))
                {
                    string postResponse = GlobalCookieManager.retrieveCodeFromMFA(ADFSrstPostResult.Result, AuthrequestCookies);
                    postBodyResponseParser = CQ.Create(postResponse);
                    postBodyResponseInputs = postBodyResponseParser["input"];
                    foreach (var li in postBodyResponseInputs)
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
                        if (li.Name == "session_state")
                        {
                            session_state = li.Value;
                        }
                    }
                }

                codereceived:
                //post everyhing to sharepoint
                string SharePointPostBody = string.Format(StringConstants.SharePointFormPost, code, id_token, state, session_state);
                NameValueCollection SharePointPostHeader = new NameValueCollection();
                SharePointPostHeader.Add("Origin", "https://login.microsoftonline.com");
                SharePointPostHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                SharePointPostHeader.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                SharePointPostHeader.Add("X-Requested-With", "XMLHttpRequest");
                SharePointPostHeader.Add("Referer", "https://login.microsoftonline.com/common/login");
                Task<HttpResponseMessage> SharePointPostResult = null;
                try
                {
                    SharePointPostResult = HttpClientHelper.PostAsyncFullResponse(Wreply, SharePointPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, SharePointPostHeader);
                    SharePointPostResult.Wait();
                }
                catch (Exception ex)
                {
                    if (SharePointPostResult.Result.StatusCode == HttpStatusCode.Forbidden)
                    {

                    }
                    else
                    {
                        throw ex;
                    }
                }
                foreach (Cookie SPCookie in AuthrequestCookies.GetCookies(new Uri(Wreply)))
                {
                    ret.Add(SPCookie);
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            return ret;
        }

        public CookieContainer Authenticate()
        {
            if (_cachedCookieContainer == null || DateTime.Now > _expires)
            {
                CookieContainer cookieContainer = GetCookieContainer();

                if (cookieContainer != null && cookieContainer.Count > 0)
                {
                    var cookies = from Cookie cookie in cookieContainer.GetCookies(spSiteUrl)
                                  where cookie.Name == "FedAuth"
                                  select cookie;

                    if (cookies.Any())
                    {
                        _cachedCookieContainer = cookieContainer;
                        //return cookieContainer;
                    }
                    //throw new Exception("Could not retrieve Auth cookies");
                }
                return null;
            }
            return _cachedCookieContainer;
        }

        public CookieContainer GetCookieContainer()
        {
            if (stsAuthToken != null)
            {
                if (DateTime.Now > stsAuthToken.Expires)
                {
                    stsAuthToken = GetMsoStsSAMLToken();

                    if (stsAuthToken.Token != null)
                    {
                        SPOAuthCookies cookies = GetSPOAuthCookies(stsAuthToken);
                        CookieContainer cc = new CookieContainer();

                        Cookie samlAuthCookie = new Cookie("FedAuth", cookies.FedAuth)
                        {
                            Path = "/",
                            Expires = stsAuthToken.Expires,
                            Secure = cookies.Host.Scheme.Equals("https"),
                            HttpOnly = true,
                            Domain = cookies.Host.Host
                        };

                        cc.Add(spSiteUrl, samlAuthCookie);

                        Cookie rtFACookie = new Cookie("rtFA", cookies.RtFA)
                        {
                            Path = "/",
                            Expires = this.stsAuthToken.Expires,
                            Secure = cookies.Host.Scheme.Equals("https"),
                            HttpOnly = true,
                            Domain = cookies.Host.Host
                        };
                        _expires = this.stsAuthToken.Expires;
                        cc.Add(spSiteUrl, rtFACookie);

                        return cc;
                    }
                }
            }

            return null;
        }

        private SPOAuthCookies GetSPOAuthCookies(SamlSecurityToken stsToken)
        {
            try
            {
                // signs in to SPO with the security token issued by MSO STS and gets the fed auth cookies
                // the fed auth cookie needs to be attached to all SPO REST services requests
                Uri siteUri = spSiteUrl;
                Uri wsSigninUrl = new Uri(String.Format("{0}://{1}/{2}", siteUri.Scheme, siteUri.Authority, spowssigninUri));
                var clientHandler = new HttpClientHandler();

                SendHttpRequest(
                    wsSigninUrl,
                    HttpMethod.Post,
                    new MemoryStream(stsToken.Token),
                    "application/x-www-form-urlencoded",
                    clientHandler);

                SPOAuthCookies spoAuthCookies = new SPOAuthCookies();
                spoAuthCookies.FedAuth = clientHandler.CookieContainer.GetCookies(wsSigninUrl)["FedAuth"].Value;
                spoAuthCookies.RtFA = clientHandler.CookieContainer.GetCookies(wsSigninUrl)["rtFA"].Value;
                spoAuthCookies.Expires = stsToken.Expires;
                spoAuthCookies.Host = wsSigninUrl;

                return spoAuthCookies;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private Uri GetAdfsAuthUrl()
        {
            // make a post request with the user's login name to MSO HRD (Home Realm Discovery) service 
            // so it can find out the url of the federation service (corporate ADFS) responsible for authenticating the user
            byte[] response = SendHttpRequest(
                 new Uri(msoHrdUrl),
                 HttpMethod.Post,
                 new MemoryStream(Encoding.UTF8.GetBytes(String.Format("handler=1&login={0}", username))), // pass in the login name in the body of the form
                 "application/x-www-form-urlencoded",
                 null);

            StreamReader sr = new StreamReader(new MemoryStream(response));
            var jObj = JObject.Parse(Encoding.UTF8.GetString(response, 0, response.Length));

            // the corporate STS url is in the AuthURL element of the response body
            Uri corpAdfsProxyUrl = jObj["AuthURL"] != null ? new Uri(jObj["AuthURL"].ToString()) : null;

            return corpAdfsProxyUrl;
        }

        private string GetAdfsSAMLTokenUsernamePassword()
        {
            // makes a seurity token request to the corporate ADFS proxy usernamemixed endpoint using
            // the user's corporate credentials. The logon token is used to talk to MSO STS to get
            // an O365 service token that can then be used to sign into SPO.
            string samlAssertion = null;

            // the corporate ADFS proxy endpoint that issues SAML seurity tokens given username/password credentials 
            string stsUsernameMixedUrl = String.Format("https://{0}/adfs/services/trust/2005/usernamemixed/", adfsAuthUrl.Host);

            // generate the WS-Trust security token request SOAP message passing in the user's corporate credentials 
            // and the site we want access to. We send the token request to the corporate ADFS proxy usernamemixed endpoint.
            byte[] requestBody = Encoding.UTF8.GetBytes(ParameterizeSoapRequestTokenMsgWithUsernamePassword(
                "urn:federation:MicrosoftOnline", // we are requesting a logon token to talk to the Microsoft Federation Gateway
                username,
                password,
                stsUsernameMixedUrl));

            try
            {
                byte[] response = SendHttpRequest(
                    new Uri(stsUsernameMixedUrl),
                    HttpMethod.Post,
                    new MemoryStream(requestBody),
                    "application/soap+xml; charset=utf-8",
                    null);

                // the logon token is in the SAML assertion element of the message body
                XDocument xDoc = XDocument.Parse(Encoding.UTF8.GetString(response, 0, response.Length), LoadOptions.PreserveWhitespace);
                var assertion = from e in xDoc.Descendants()
                                where e.Name == XName.Get("Assertion", saml)
                                select e;

                samlAssertion = assertion.FirstOrDefault().ToString();

                // for some reason the assertion string needs to be loaded into an XDocument
                // and written out for for the XML to be valid. Otherwise we get an invalid
                // XML error back from ADFSs
                XDocument doc1 = XDocument.Parse(samlAssertion);
                samlAssertion = doc1.ToString(SaveOptions.DisableFormatting);
            }
            catch
            {
                // we failed to sign the user using corporate credentials
            }

            return samlAssertion;
        }

        private string GetAdfsSAMLTokenWinAuth()
        {
            // makes a seurity token request to the corporate ADFS proxy integrated auth endpoint.
            // If the user is logged on to a machine joined to the corporate domain with her Windows credentials and connected
            // to the corporate network Kerberos automatically takes care of authenticating the security token 
            // request to ADFS.
            // The logon token is used to talk to MSO STS to get an O365 service token that can then be used to sign into SPO.

            string samlAssertion = null;

            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true; // use the default credentials so Kerberos can take care of authenticating our request

            byte[] stsresponse = SendHttpRequest(
                adfsIntegratedAuthUrl,
                HttpMethod.Get,
                null,
                "text/html; charset=utf-8",
                handler);

            StreamReader sr = new StreamReader(new MemoryStream(stsresponse));
            XDocument xDoc = XDocument.Parse(sr.ReadToEnd(), LoadOptions.PreserveWhitespace);

            try
            {
                var body = from e in xDoc.Descendants()
                           where e.Name == XName.Get("body")
                           select e;

                var form = from e in body.FirstOrDefault().Descendants()
                           where e.Name == XName.Get("form")
                           select e;

                // the security token response we got from ADFS is in the wresult input element 
                var wresult = from e in form.FirstOrDefault().Descendants()
                              where e.Name == XName.Get("input") &&
                              e.Attribute(XName.Get("name")) != null &&
                              e.Attribute(XName.Get("name")).Value == "wresult"
                              select e;

                if (wresult.FirstOrDefault() != null)
                {
                    // the logon token is in the SAML assertion element
                    XDocument xDoc1 = XDocument.Parse(wresult.FirstOrDefault().Attribute(XName.Get("value")).Value, LoadOptions.PreserveWhitespace);
                    var assertion = from e in xDoc1.Descendants()
                                    where e.Name == XName.Get("Assertion", saml)
                                    select e;

                    samlAssertion = assertion.FirstOrDefault().ToString();

                    // for some reason the assertion string needs to be loaded into an XDocument
                    // and written out for for the XML to be valid. Otherwise we get an invalid
                    // XML error back from ADFSs
                    XDocument doc1 = XDocument.Parse(samlAssertion);
                    samlAssertion = doc1.ToString(SaveOptions.DisableFormatting);
                }
            }
            catch
            {
                // we failed to sign the user using integrated Windows Auth
            }

            return samlAssertion;
        }

        public SamlSecurityToken GetMsoStsSAMLToken()
        {
            // Makes a request that conforms with the WS-Trust standard to 
            // Microsoft Online Services Security Token Service to get a SAML
            // security token back so we can then use it to sign the user to SPO 

            var samlST = new SamlSecurityToken();
            byte[] saml11RTBytes = null;
            string logonToken = null;

            // find out whether the user's domain is a federated domain
            adfsAuthUrl = GetAdfsAuthUrl();

            // get logon token using windows integrated auth when the user is connected to the corporate network 
            if (adfsAuthUrl != null && useIntegratedWindowsAuth)
            {
                UriBuilder ub = new UriBuilder();
                ub.Scheme = adfsAuthUrl.Scheme;
                ub.Host = adfsAuthUrl.Host;
                ub.Path = string.Format("{0}auth/integrated/", adfsAuthUrl.LocalPath);

                // specify in the query string we want a logon token to present to the Microsoft Federation Gateway
                // for the corresponding user
                ub.Query = String.Format("{0}&wa=wsignin1.0&wtrealm=urn:federation:MicrosoftOnline", adfsAuthUrl.Query.Remove(0, 1)).
                    Replace("&username=", String.Format("&username={0}", username));

                adfsIntegratedAuthUrl = ub.Uri;

                // get the logon token from the corporate ADFS using Windows Integrated Auth
                logonToken = GetAdfsSAMLTokenWinAuth();

                if (!string.IsNullOrEmpty(logonToken))
                {
                    // generate the WS-Trust security token request SOAP message passing in the logon token we got from the corporate ADFS
                    // and the site we want access to 
                    saml11RTBytes = Encoding.UTF8.GetBytes(ParameterizeSoapRequestTokenMsgWithAssertion(
                        spSiteUrl.ToString(),
                        logonToken,
                        msoStsUrl));
                }
            }

            // get logon token using the user's corporate credentials. Likely when not connected to the corporate network
            if (logonToken == null && adfsAuthUrl != null && !string.IsNullOrEmpty(password))
            {
                logonToken = GetAdfsSAMLTokenUsernamePassword(); // get the logon token from the corporate ADFS proxy usernamemixed enpoint

                if (logonToken != null)
                {
                    // generate the WS-Trust security token request SOAP message passing in the logon token we got from the corporate ADFS
                    // and the site we want access to 
                    saml11RTBytes = Encoding.UTF8.GetBytes(ParameterizeSoapRequestTokenMsgWithAssertion(
                      String.IsNullOrEmpty(Convert.ToString(spSiteUrl)) ? "urn:federation:MicrosoftOnline" : Convert.ToString(spSiteUrl),
                      logonToken,
                      msoStsUrl));
                }
            }

            if (logonToken == null && this.adfsAuthUrl == null && string.IsNullOrEmpty(password)) // login with O365 credentials. Not a federated login.
            {
                // generate the WS-Trust security token request SOAP message passing in the user's credentials and the site we want access to 
                saml11RTBytes = Encoding.UTF8.GetBytes(ParameterizeSoapRequestTokenMsgWithUsernamePassword(
                     String.IsNullOrEmpty(Convert.ToString(spSiteUrl)) ? "urn:federation:MicrosoftOnline" : Convert.ToString(spSiteUrl),
                    username,
                    password,
                    msoStsUrl));
            }

            if (saml11RTBytes != null)
            {
                // make the post request to MSO STS with the WS-Trust payload
                byte[] response = SendHttpRequest(
                    new Uri(msoStsUrl),
                    HttpMethod.Post,
                    new MemoryStream(saml11RTBytes),
                    "application/soap+xml; charset=utf-8",
                    null);

                StreamReader sr = new StreamReader(new MemoryStream(response));


                string strResponse = sr.ReadToEnd();
                // the SAML security token is in the BinarySecurityToken element of the message body
                XDocument xDoc = XDocument.Parse(strResponse);

                //check error code



                var binaryST = from e in xDoc.Descendants()
                               where e.Name == XName.Get("BinarySecurityToken", wsse)
                               select e;

                // get the security token expiration date from the message body
                var expires = from e in xDoc.Descendants()
                              where e.Name == XName.Get("Expires", wsu)
                              select e;

                if (binaryST.FirstOrDefault() != null && expires.FirstOrDefault() != null)
                {

                    samlST.Token = Encoding.UTF8.GetBytes(binaryST.FirstOrDefault().Value);
                    samlST.Expires = DateTime.Parse(expires.FirstOrDefault().Value);
                }
            }

            return samlST;
        }



        public string GetMsoStsSAMLTokenForTenancyName()
        {
            // Makes a request that conforms with the WS-Trust standard to 
            // Microsoft Online Services Security Token Service to get a SAML
            // security token back so we can then use it to sign the user to SPO 

            var samlST = new SamlSecurityToken();
            byte[] saml11RTBytes = null;
            string logonToken = null;

            // find out whether the user's domain is a federated domain
            adfsAuthUrl = GetAdfsAuthUrl();

            // get logon token using windows integrated auth when the user is connected to the corporate network 
            if (adfsAuthUrl != null && useIntegratedWindowsAuth)
            {
                UriBuilder ub = new UriBuilder();
                ub.Scheme = adfsAuthUrl.Scheme;
                ub.Host = adfsAuthUrl.Host;
                ub.Path = string.Format("{0}auth/integrated/", adfsAuthUrl.LocalPath);

                // specify in the query string we want a logon token to present to the Microsoft Federation Gateway
                // for the corresponding user
                ub.Query = String.Format("{0}&wa=wsignin1.0&wtrealm=urn:federation:MicrosoftOnline", adfsAuthUrl.Query.Remove(0, 1)).
                    Replace("&username=", String.Format("&username={0}", username));

                adfsIntegratedAuthUrl = ub.Uri;

                // get the logon token from the corporate ADFS using Windows Integrated Auth
                logonToken = GetAdfsSAMLTokenWinAuth();

                if (!string.IsNullOrEmpty(logonToken))
                {
                    // generate the WS-Trust security token request SOAP message passing in the logon token we got from the corporate ADFS
                    // and the site we want access to 
                    saml11RTBytes = Encoding.UTF8.GetBytes(ParameterizeSoapRequestTokenMsgWithAssertion(
                        spSiteUrl.ToString(),
                        logonToken,
                        msoStsUrl));
                }
            }

            // get logon token using the user's corporate credentials. Likely when not connected to the corporate network
            if (logonToken == null && adfsAuthUrl != null && !string.IsNullOrEmpty(password))
            {
                logonToken = GetAdfsSAMLTokenUsernamePassword(); // get the logon token from the corporate ADFS proxy usernamemixed enpoint

                if (logonToken != null)
                {
                    // generate the WS-Trust security token request SOAP message passing in the logon token we got from the corporate ADFS
                    // and the site we want access to 
                    saml11RTBytes = Encoding.UTF8.GetBytes(ParameterizeSoapRequestTokenMsgWithAssertion(
                      String.IsNullOrEmpty(Convert.ToString(spSiteUrl)) ? "urn:federation:MicrosoftOnline" : Convert.ToString(spSiteUrl),
                      logonToken,
                      msoStsUrl));

                    string rts = ParameterizeRST("2017-04-19T05:50:51.892Z", ParameterizeSoapRequestTokenMsgWithAssertion(
                      String.IsNullOrEmpty(Convert.ToString(spSiteUrl)) ? "urn:federation:MicrosoftOnline" : Convert.ToString(spSiteUrl),
                      logonToken,
                      msoStsUrl));

                    return rts;

                    StreamReader sr = new StreamReader(new MemoryStream(saml11RTBytes));

                    // the SAML security token is in the BinarySecurityToken element of the message body
                    XDocument xDoc = XDocument.Parse(sr.ReadToEnd());
                    var binaryST = from e in xDoc.Descendants()
                                   where e.Name == XName.Get("saml:Conditions", wsse)
                                   select e;
                }
            }

            if (logonToken == null && this.adfsAuthUrl == null && string.IsNullOrEmpty(password)) // login with O365 credentials. Not a federated login.
            {
                // generate the WS-Trust security token request SOAP message passing in the user's credentials and the site we want access to 
                saml11RTBytes = Encoding.UTF8.GetBytes(ParameterizeSoapRequestTokenMsgWithUsernamePassword(
                     String.IsNullOrEmpty(Convert.ToString(spSiteUrl)) ? "urn:federation:MicrosoftOnline" : Convert.ToString(spSiteUrl),
                    username,
                    password,
                    msoStsUrl));
            }

            if (saml11RTBytes != null)
            {
                // make the post request to MSO STS with the WS-Trust payload
                byte[] response = SendHttpRequest(
                    new Uri(msoStsUrl),
                    HttpMethod.Post,
                    new MemoryStream(saml11RTBytes),
                    "application/soap+xml; charset=utf-8",
                    null);

                StreamReader sr = new StreamReader(new MemoryStream(response));

                // the SAML security token is in the BinarySecurityToken element of the message body
                XDocument xDoc = XDocument.Parse(sr.ReadToEnd());
                var binaryST = from e in xDoc.Descendants()
                               where e.Name == XName.Get("BinarySecurityToken", wsse)
                               select e;

                // get the security token expiration date from the message body
                var expires = from e in xDoc.Descendants()
                              where e.Name == XName.Get("Expires", wsu)
                              select e;

                if (binaryST.FirstOrDefault() != null && expires.FirstOrDefault() != null)
                {

                    samlST.Token = Encoding.UTF8.GetBytes(binaryST.FirstOrDefault().Value);
                    samlST.Expires = DateTime.Parse(expires.FirstOrDefault().Value);
                }
            }

            return "";
        }
        private string ParameterizeSoapRequestTokenMsgWithUsernamePassword(string url, string username, string password, string toUrl)
        {
            string samlRTString = "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">\n  <s:Header>\n    <a:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</a:Action>\n    <a:ReplyTo>\n      <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>\n    </a:ReplyTo>\n    <a:To s:mustUnderstand=\"1\">[toUrl]</a:To>\n    <o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">\n      <o:UsernameToken>\n        <o:Username>[username]</o:Username>\n        <o:Password>[password]</o:Password>\n      </o:UsernameToken>\n    </o:Security>\n  </s:Header>\n  <s:Body>\n    <t:RequestSecurityToken xmlns:t=\"http://schemas.xmlsoap.org/ws/2005/02/trust\">\n      <wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">\n        <a:EndpointReference>\n          <a:Address>[url]</a:Address>\n        </a:EndpointReference>\n      </wsp:AppliesTo>\n      <t:KeyType>http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey</t:KeyType>\n      <t:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</t:RequestType>\n      <t:TokenType>urn:oasis:names:tc:SAML:1.0:assertion</t:TokenType>\n    </t:RequestSecurityToken>\n  </s:Body>\n</s:Envelope>";
            samlRTString = samlRTString.Replace("[username]", username);
            samlRTString = samlRTString.Replace("[password]", password);
            samlRTString = samlRTString.Replace("[url]", url);
            samlRTString = samlRTString.Replace("[toUrl]", toUrl);

            return samlRTString;
        }

        private string ParameterizeSoapRequestTokenMsgWithAssertion(string url, string samlAssertion, string toUrl)
        {
            string samlRTString = "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">\n  <s:Header>\n    <a:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</a:Action>\n    <a:ReplyTo>\n      <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>\n    </a:ReplyTo>\n    <a:To s:mustUnderstand=\"1\">[toUrl]</a:To>\n    <o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">[assertion]\n    </o:Security>\n  </s:Header>\n  <s:Body>\n<t:RequestSecurityToken xmlns:t=\"http://schemas.xmlsoap.org/ws/2005/02/trust\">\n      <wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">\n        <a:EndpointReference>\n          <a:Address>[url]</a:Address>\n        </a:EndpointReference>\n      </wsp:AppliesTo>\n      <t:KeyType>http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey</t:KeyType>\n      <t:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</t:RequestType>\n      <t:TokenType>urn:oasis:names:tc:SAML:1.0:assertion</t:TokenType>\n    </t:RequestSecurityToken>\n  </s:Body>\n</s:Envelope>";
            samlRTString = samlRTString.Replace("[assertion]", samlAssertion);
            samlRTString = samlRTString.Replace("[url]", url);
            samlRTString = samlRTString.Replace("[toUrl]", toUrl);

            return samlRTString;
        }

        private string ParameterizeRST(string time, string token)
        {
            string samlRTString = "<t:RequestSecurityTokenResponse xmlns:t=\"http://schemas.xmlsoap.org/ws/2005/02/trust\" ><t:Lifetime><wsu:Created xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">2017-04-19T05:50:51.892Z</wsu:Created><wsu:Expires xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">[TIME]</wsu:Expires></t:Lifetime><wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\" ><wsa:EndpointReference xmlns:wsa=\"http://www.w3.org/2005/08/addressing\" ><wsa:Address>urn:federation:MicrosoftOnline</wsa:Address></wsa:EndpointReference></wsp:AppliesTo><t:RequestedSecurityToken>[TOKEN]<t:TokenType>urn:oasis:names:tc:SAML:1.0:assertion</t:TokenType><t:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</t:RequestType><t:KeyType>http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey</t:KeyType></t:RequestSecurityTokenResponse>";
            samlRTString = samlRTString.Replace("[TIME]", time);
            samlRTString = samlRTString.Replace("[TOKEN]", token);

            return samlRTString;
        }


        /// <summary>
        /// Sends an http request to the specified uri and returns the response as a byte array 
        /// </summary>
        /// <param name="uri">The request uri</param>
        /// <param name="method">The http method</param>
        /// <param name="requestContent">A stream containing the request content</param>
        /// <param name="contentType">The content type of the http request</param>
        /// <param name="clientHandler">The request client handler</param>
        /// <param name="headers">The http headers to append to the request</param>
        public static byte[] SendHttpRequest(Uri uri, HttpMethod method, Stream requestContent = null, string contentType = null, HttpClientHandler clientHandler = null, Dictionary<string, string> headers = null)
        {
            var req = clientHandler == null ? new HttpClient() : new HttpClient(clientHandler);
            var message = new HttpRequestMessage(method, uri);
            byte[] response;

            req.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
            message.Headers.Add("Accept", contentType); // set the content type of the request


            if (requestContent != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Delete))
            {
                message.Content = new StreamContent(requestContent); //set the body for the request

                if (!string.IsNullOrEmpty(contentType))
                {
                    message.Content.Headers.Add("Content-Type", contentType); // if the request has a body set the MIME type
                }
            }

            // append additional headers to the request
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (message.Headers.Contains(header.Key))
                    {
                        message.Headers.Remove(header.Key);
                    }

                    message.Headers.Add(header.Key, header.Value);
                }
            }

            // Send the request and read the response as an array of bytes
            using (var res = req.SendAsync(message).Result)
            {
                response = res.Content.ReadAsByteArrayAsync().Result;
            }

            return response;
        }
    }
}
