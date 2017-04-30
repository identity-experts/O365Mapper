using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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

        private string ParameterizeRST(string time,string token)
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
