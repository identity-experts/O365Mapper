using System;
using System.Linq;
using System.Text;
using System.Net;
using System.ServiceModel;
using System.Xml;
using System.ServiceModel.Channels;
using System.IO;
using System.Xml.Linq;
using System.IdentityModel.Protocols.WSTrust;
using System.Reflection;
using _365Drive.Office365.GetTenancyURL;
using System.Threading.Tasks;
using CsQuery;
using System.Collections.Specialized;
using System.Net.Http;
using System.Web;

namespace _365Drive.Office365.CloudConnector
{
    public class o365cookieManager
    {

        #region Properties

        readonly string _username;
        readonly string _password;
        readonly bool _useRtfa;
        readonly Uri _host;

        public static CookieContainer _cachedCookieContainer = null;
        public static DateTime _expires = DateTime.MinValue;

        #endregion

        /// <summary>
        /// delete all cached cookies
        /// </summary>
        public static void signout()
        {
            _cachedCookieContainer = null;
            _expires = DateTime.MinValue;
        }

        #region Constructors
        public o365cookieManager(string host, string username, string password)
            : this(new Uri(host), username, password)
        {

        }
        public o365cookieManager(Uri host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            _useRtfa = true;
        }
        public o365cookieManager(Uri host, string username, string password, bool useRtfa)
        {
            _host = host;
            _username = username;
            _password = password;
            _useRtfa = useRtfa;
        }
        #endregion

        #region Constants
        public const string office365STS = "https://login.microsoftonline.com/extSTS.srf";
        public const string office365Login = "https://login.microsoftonline.com/login.srf";
        public const string office365Metadata = "https://nexus.microsoftonline-p.com/federationmetadata/2007-06/federationmetadata.xml";
        public const string wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        public const string wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private const string userAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
        #endregion


        class MsoCookies
        {
            public string FedAuth { get; set; }
            public string rtFa { get; set; }

            //IPUT
            public string build { get; set; }
            public string estsauthpersistent { get; set; }

            public DateTime Expires { get; set; }
            public Uri Host { get; set; }
        }

        // Method used to add cookies to CSOM
        //public void clientContext_ExecutingWebRequest(object sender, WebRequestEventArgs e)
        //{
        //    e.WebRequestExecutor.WebRequest.CookieContainer = getCookieContainer();
        //    //e.WebRequestExecutor.WebRequest.UserAgent = userAgent;
        //}

        // Creates or loads cached cookie container
        public CookieContainer getCookieContainer()
        {
            CookieContainer cc = new CookieContainer();
            try
            {
                if (!Utility.ready())
                    return null;

                if (_cachedCookieContainer == null || DateTime.Now > _expires)
                {

                    MsoCookies cookies = getIECookies();
                    // Get the SAML tokens from SPO STS (via MSO STS) using fed auth passive approach
                    //MsoCookies cookies = getSamlToken();

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
                            // Set the rtFA (sign-out) cookie, added march 2011
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


        /// <summary>
        /// Below (getSAMlToken) is a bit older way to get token. Making a new way now.
        /// </summary>
        /// <returns></returns>
        private MsoCookies getIECookies()
        {
            MsoCookies ret = new MsoCookies();
            try
            {
                //are we ready?
                if (!Utility.ready())
                    return null;

                //get nonse
                string authorizeCall = string.Empty, Authorizectx = string.Empty, Authorizeflowtoken = string.Empty, authorizeCanary = string.Empty, nonce = string.Empty, clientRequestId = string.Empty;
                string AuthrequestUrl = string.Format(StringConstants.AuthenticateRequestUrl, _host);
                CookieContainer AuthrequestCookies = new CookieContainer();
                Task<HttpResponseMessage> AuthrequestResponse = HttpClientHelper.GetAsyncFullResponse(AuthrequestUrl, AuthrequestCookies, true);
                AuthrequestResponse.Wait();

                NameValueCollection qscoll = HttpUtility.ParseQueryString(AuthrequestResponse.Result.RequestMessage.RequestUri.Query);
                if (qscoll.Count > 0)
                { 
                    nonce = qscoll["nonce"];
                    clientRequestId = qscoll["client-request-id"];
                }
                //if (!String.IsNullOrEmpty(Convert.ToString(AuthrequestResponse.Result.Headers.GetValues("request-id"))))
                //{
                //    clientRequestId = Convert.ToString(AuthrequestResponse.Result.Headers.GetValues("request-id"));

                //}
                //get the host with url
                var Wreply = _host.GetLeftPart(UriPartial.Authority) + "/_forms/default.aspx";

                string WindowsoAuthCallUrl = String.Format(StringConstants.getCloudCookieStep0, LicenseManager.encode(Wreply), nonce, clientRequestId);
                Task<string> call0Result = HttpClientHelper.GetAsync(WindowsoAuthCallUrl, AuthrequestCookies);
                call0Result.Wait();

                //first call to get flow token, ctx and canary
                //CookieContainer AuthrequestCookies = new CookieContainer();
                string MSOnlineoAuthCallUrl = String.Format(StringConstants.getCloudCookieStep1, LicenseManager.encode(Wreply), nonce, clientRequestId);
                Task<string> call1Result = HttpClientHelper.GetAsync(MSOnlineoAuthCallUrl, AuthrequestCookies);
                call1Result.Wait();


                authorizeCall = call1Result.Result;


                //retrieve the ctx, flow and cannary
                LoginConfig config = _365DriveTenancyURL.renderConfig(authorizeCall);
                Authorizectx = config.sCtx;
                Authorizeflowtoken = config.sFT;
                authorizeCanary = config.canary;


                /////Fetch the ctx and flow token and canary
                //CQ htmlparser = CQ.Create(authorizeCall);
                //var items = htmlparser["input"];
                //foreach (var li in items)
                //{
                //    if (li.Name == "ctx")
                //    {
                //        Authorizectx = li.Value;
                //    }
                //    if (li.Name == "flowToken")
                //    {
                //        Authorizeflowtoken = li.Value;
                //    }
                //}
                //authorizeCanary = _365DriveTenancyURL.getCanary2(authorizeCall);

                //string loginPostBody = string.Format(StringConstants.CloudloginPostData, _username, _password, Authorizectx, Authorizeflowtoken, LicenseManager.encode(authorizeCanary));
                //NameValueCollection loginPostHeader = new NameValueCollection();
                //loginPostHeader.Add("Origin", "https://login.microsoftonline.com");
                //loginPostHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                //loginPostHeader.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                //loginPostHeader.Add("X-Requested-With", "XMLHttpRequest");
                //loginPostHeader.Add("Referer", "https://login.microsoftonline.com/common/login");

                //Task<string> call2Result = HttpClientHelper.PostAsync(StringConstants.CloudloginPost, loginPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, loginPostHeader);
                //call2Result.Wait();


                LogManager.Verbose("Call 1 finished. output: ctx=" + Authorizectx + " flow=" + Authorizeflowtoken + " cookie count: " + AuthrequestCookies.Count.ToString());

                //getting ready for call 2
                string MSloginpostData = String.Format(StringConstants.newMSloginPost, _username, _password, LicenseManager.encode(authorizeCanary), Authorizectx, Authorizeflowtoken);
                NameValueCollection MSloginpostHeader = new NameValueCollection();
                MSloginpostHeader.Add("Accept", "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*");
                MSloginpostHeader.Add("Referer", String.Format(StringConstants.AzureActivateUserStep1, _username, StringConstants.clientID, StringConstants.appRedirectURL, StringConstants.appResourceUri));
                MSloginpostHeader.Add("Accept-Language", "en-US");
                //request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                //MSloginpostHeader.Add("Accept-Encoding", "gzip, deflate");
                MSloginpostHeader.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)");
                MSloginpostHeader.Add("Host", "login.microsoftonline.com");
                MSloginpostHeader.Add("Accept", "application/json");
                Task<string> msLoginPostResponse = HttpClientHelper.PostAsync(StringConstants.AzureActivateUserStep2, MSloginpostData, "application/x-www-form-urlencoded", AuthrequestCookies, MSloginpostHeader);
                msLoginPostResponse.Wait();


                LoginConfig msPostConfig = _365DriveTenancyURL.renderConfig(msLoginPostResponse.Result);
                Authorizectx = msPostConfig.sCtx;
                Authorizeflowtoken = msPostConfig.sFT;
                authorizeCanary = msPostConfig.canary;


                //getting ready for KMSI post
                string MSKMSIPostData = String.Format(StringConstants.KMSIPost, Authorizectx, Authorizeflowtoken, LicenseManager.encode(authorizeCanary));
                Task<string> msKMSIPost = HttpClientHelper.PostAsync(StringConstants.loginKMSI, MSKMSIPostData, "application/x-www-form-urlencoded", AuthrequestCookies, MSloginpostHeader);
                msKMSIPost.Wait();

                string code = string.Empty, id_token = string.Empty, state = string.Empty, session_state = string.Empty;
                ///Fetch the ctx and flow token and canary
                CQ postBodyResponseParser = CQ.Create(msKMSIPost.Result);
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
                if(string.IsNullOrEmpty(code))
                {
                    string postResponse = GlobalCookieManager.retrieveCodeFromMFA(msKMSIPost.Result, AuthrequestCookies);
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

                //post everyhing to sharepoint
                string SharePointPostBody = string.Format(StringConstants.SharePointFormPost, code, id_token, state, session_state);
                NameValueCollection SharePointPostHeader = new NameValueCollection();
                SharePointPostHeader.Add("Origin", "https://login.microsoftonline.com");
                SharePointPostHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                SharePointPostHeader.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                SharePointPostHeader.Add("X-Requested-With", "XMLHttpRequest");
                SharePointPostHeader.Add("Referer", "https://login.microsoftonline.com/common/login");
                Task<HttpResponseMessage> SharePointPostResult = null;


                //IPUT
                ret.build = AuthrequestCookies.GetCookies(new Uri("https://login.microsoftonline.com"))["buid"].Value;
                LogManager.Info("ret.build: " + ret.build);
                ret.estsauthpersistent = AuthrequestCookies.GetCookies(new Uri("https://login.microsoftonline.com"))["ESTSAUTHPERSISTENT"].Value;
                LogManager.Info("ret.ESTSAUTHPERSISTENT: " + ret.estsauthpersistent);

                try
                {
                    SharePointPostResult = HttpClientHelper.PostAsyncFullResponse(Wreply, SharePointPostBody, "application/x-www-form-urlencoded", AuthrequestCookies, SharePointPostHeader);
                    SharePointPostResult.Wait();
                }
                catch(Exception ex)
                {
                    if(SharePointPostResult.Result.StatusCode == HttpStatusCode.Forbidden)
                    {

                    }
                    else
                    {
                        throw ex;
                    }
                }
                foreach (Cookie SPCookie in AuthrequestCookies.GetCookies(new Uri(Wreply)))
                {
                    if (SPCookie.Name.ToLower() == "fedauth")
                    {
                        ret.FedAuth = SPCookie.Value;
                        ret.Expires = DateTime.Now.AddHours(Constants.AuthcookieExpiryHours);
                        ret.Host = new Uri(Wreply);
                    }
                    if (SPCookie.Name.ToLower() == "rtfa")
                    {
                        ret.rtFa = SPCookie.Value;
                    }
                }

            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            return ret;
        }





        private MsoCookies getSamlToken()
        {
            MsoCookies ret = new MsoCookies();

            try
            {
                if (!Utility.ready())
                    return null;

                var sharepointSite = new
                {
                    Wctx = office365Login,
                    Wreply = _host.GetLeftPart(UriPartial.Authority) + "/_forms/default.aspx?wa=wsignin1.0"
                };

                //get token from STS
                string stsResponse = getResponse(office365STS, sharepointSite.Wreply);

                // parse the token response
                XDocument doc = XDocument.Parse(stsResponse);

                // get the security token
                var crypt = from result in doc.Descendants()
                            where result.Name == XName.Get("BinarySecurityToken", wsse)
                            select result;

                // get the token expiration
                var expires = from result in doc.Descendants()
                              where result.Name == XName.Get("Expires", wsu)
                              select result;
                ret.Expires = Convert.ToDateTime(expires.First().Value);


                HttpWebRequest request = createRequest(sharepointSite.Wreply);
                byte[] data = Encoding.UTF8.GetBytes(crypt.FirstOrDefault().Value);
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();

                    using (HttpWebResponse webResponse = request.GetResponse() as HttpWebResponse)
                    {

                        // Handle redirect, added may 2011 for P-subscriptions
                        if (webResponse.StatusCode == HttpStatusCode.MovedPermanently)
                        {
                            HttpWebRequest request2 = createRequest(webResponse.Headers["Location"]);
                            using (Stream stream2 = request2.GetRequestStream())
                            {
                                stream2.Write(data, 0, data.Length);
                                stream2.Close();

                                using (HttpWebResponse webResponse2 = request2.GetResponse() as HttpWebResponse)
                                {
                                    ret.FedAuth = webResponse2.Cookies["FedAuth"].Value;
                                    ret.rtFa = webResponse2.Cookies["rtFa"].Value;
                                    ret.Host = request2.RequestUri;
                                }
                            }
                        }
                        else
                        {
                            ret.FedAuth = webResponse.Cookies["FedAuth"].Value;
                            ret.rtFa = webResponse.Cookies["rtFa"].Value;
                            ret.Host = request.RequestUri;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                return null;
            }
            return ret;
        }

        static HttpWebRequest createRequest(string url)
        {
            if (!Utility.ready())
                return null;

            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            try
            {
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = new CookieContainer();
                request.AllowAutoRedirect = false; // Do NOT automatically redirect
                request.UserAgent = userAgent;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return request;
        }

        private string getResponse(string stsUrl, string realm)
        {
            string strResponse = string.Empty;
            try
            {
                if (!Utility.ready())
                    return null;
                RequestSecurityToken rst = new RequestSecurityToken
                {
                    RequestType = RequestTypes.Issue,
                    KeyType = KeyTypes.Bearer,
                    AppliesTo = new EndpointReference(realm)
                };

                WSTrustFeb2005RequestSerializer trustSerializer = new WSTrustFeb2005RequestSerializer();

                WSHttpBinding binding = new WSHttpBinding();

                binding.Security.Mode = SecurityMode.TransportWithMessageCredential;

                binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
                binding.Security.Message.EstablishSecurityContext = false;
                binding.Security.Message.NegotiateServiceCredential = false;

                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

                EndpointAddress address = new EndpointAddress(stsUrl);

                using (WSTrustFeb2005ContractClient trustClient = new WSTrustFeb2005ContractClient(binding, address))
                {
                    trustClient.ClientCredentials.UserName.UserName = _username;
                    trustClient.ClientCredentials.UserName.Password = _password;
                    Message response = trustClient.EndIssue(
                        trustClient.BeginIssue(
                         Message.CreateMessage(
                                    MessageVersion.Default,
                                   "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue",
                                    new RequestBodyWriter(trustSerializer, rst)
                            ),
                            null,
                            null));
                    trustClient.Close();
                    using (XmlDictionaryReader reader = response.GetReaderAtBodyContents())
                    {
                        strResponse = reader.ReadOuterXml();
                    }
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return strResponse;
        }


    }
}
