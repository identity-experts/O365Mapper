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
            string tenancyUniqueName = string.Empty;
            FedType authType = userRealM(upn);
            try
            {
                if (!Utility.ready())
                    return string.Empty;

                // Getting user activation step 1
                string ctx = string.Empty;
                string flowtoken = string.Empty;
                string call1result = string.Empty;
                CookieContainer cookies = new CookieContainer();
                HttpClientHandler handler = new HttpClientHandler();
                handler.CookieContainer = cookies;
                //     IEnumerable<Cookie> responseCookies ;

                using (HttpClient request = new HttpClient(handler))
                {

                    LogManager.Verbose("Get request: " + String.Format(StringConstants.AzureActivateUserStep1, upn));
                    Task<HttpResponseMessage> response = request.GetAsync(String.Format(StringConstants.AzureActivateUserStep1, upn));
                    response.Wait();

                    if (response.Result.Content != null)
                    {

                        using (HttpContent content = response.Result.Content)
                        {



                            //read all cookies

                            // ... Read the string.
                            Task<string> result = content.ReadAsStringAsync();
                            call1result = result.Result;
                            CQ htmlparser = CQ.Create(result.Result);
                            var items = htmlparser["input"];
                            foreach (var li in items)
                            {
                                if (li.Name == "ctx")
                                {
                                    ctx = li.Value;
                                }
                                if (li.Name == "flowToken")
                                {
                                    flowtoken = li.Value;
                                }
                            }


                        }

                    }
                }

                //string AzureActivationUserLogin = "login={0}&passwd={1}&ctx={2}&flowToken={3}";
                //string AzureActivateUserStep2 = "https://login.microsoftonline.com/common/login";
                LogManager.Verbose("Call 1 finished");
                LogManager.Verbose("ctx: " + ctx);
                LogManager.Verbose("flowToken: " + flowtoken);

                //Getting user activation step 2 
                string code = string.Empty;
                string call2result = string.Empty;
                using (HttpClient request = new HttpClient())
                {


                    string postData = String.Format(StringConstants.AzureActivationUserLogin, upn, password, ctx, flowtoken);
                    LogManager.Verbose("Call 2 postdata:" + postData);

                    Task<HttpResponseMessage> response = request.PostAsync(String.Format(StringConstants.AzureActivateUserStep2), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));
                    request.DefaultRequestHeaders.Add("Accept", "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*");
                    request.DefaultRequestHeaders.Add("Referer", String.Format(StringConstants.AzureActivateUserStep1, upn));
                    request.DefaultRequestHeaders.Add("Accept-Language", "en-US");
                    //request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                    request.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                    request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)");
                    request.DefaultRequestHeaders.Add("Host", "login.microsoftonline.com");
                    request.DefaultRequestHeaders.Add("Accept", "application/json");


                    response.Wait();

                    if (response.Result.Content != null)
                    {
                        using (HttpContent content = response.Result.Content)
                        {
                            Task<string> result = content.ReadAsStringAsync();
                            call2result = result.Result;

                            // Make sure its authenticated
                            if (response.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                            {
                                //LogManager.Verbose("Login failed :(");
                                //return string.Empty;
                            }
                            // ... Read the string.

                            NameValueCollection qscoll = HttpUtility.ParseQueryString(response.Result.RequestMessage.RequestUri.Query);
                            if (qscoll.Count > 0)
                                code = qscoll[0];

                        }
                    }
                }
                LogManager.Verbose("Call 2 finished");

                LogManager.Verbose("Call 2 code:" + code);
                if (string.IsNullOrEmpty(code))
                {
                    //AAD connect 
                    if (isItmodernAuth(call2result))
                    {
                        LogManager.Verbose("Its not a normal cloud auth. Moving for AAD connect auth as realM suggests its cloud identity, it must be Modern Auth");

                        string step1Canary = string.Empty, clientRequestID = string.Empty, hpgact = string.Empty, hpgid = string.Empty;
                        step1Canary = getapiCanary(call1result);
                        clientRequestID = getClientRequest(call1result);
                        hpgact = getHPGact(call1result);
                        hpgid = getHPGId(call1result);
                        string step2Canary = string.Empty;

                        string call3result = string.Empty;
                        string call3Flow = string.Empty;
                        string call3ctx = string.Empty;
                        string call3hpgact = string.Empty;
                        string call3hpgid = string.Empty;
                        string call3canary = string.Empty;

                        //append cookies
                        var cookieContainer = new CookieContainer();
                        using (var pollHandler = new HttpClientHandler() { CookieContainer = cookieContainer })
                        //call 2 again as per AAD connect
                        using (HttpClient request = new HttpClient(pollHandler))
                        {
                            Uri uri = new Uri(StringConstants.AzureActivateUserStep1);
                            IEnumerable<Cookie> responseCookies = cookies.GetCookies(uri).Cast<Cookie>();

                            foreach (Cookie cookie in responseCookies)
                            {
                                cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie(cookie.Name, cookie.Value));
                            }
                            cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("testcookie", "testcookie"));
                            cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSSOTILES", "1"));
                            cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("AADSSOTILES", "1"));
                            cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                            cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSC", "00"));
                            //cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.AADPoll).Authority), new Cookie("ESTSAUTHPERSISTENT", "AQABAAEAAADRNYRQ3dhRSrm-4K-adpCJR9-o-mKIab1XFlLX2Zww26R14c1SK-Gm5sq3ndf9emPSk_6z5Ib1YTUqKNPivt5mDP5aYI9p4W8XB4BsyERuUIFJH55ZCeL4swr2ahRa6i4S5_B_dNzRPl_UwMCrd1vwZ-LUntw681RRCn-v2gETJSAHYGPiq3erAZq1SI1Q8dA2BV8el8uQaH0_hKotDq11X9tsMwGBVHb4UGvr9cqen6UWS0uD2kTy9LNTvkljzOSvqEh4eHBH_u4Uns4_Zta2uVBTepCuHJj1d2wo2JdbIX350QlfqMqMSBWCe9xInHJhnidbz_FyBlivwxYUjXfstU8Lain_kZDJoFBl_Kek0CB5WlgGQtn1GxOIqDo6EVM4ljvSOMS9fbnDfUlWWKfMblPl5SlXPmJ5E0b5T_K4PdXO8YjU6lROwxXxdvXAVi3HFbwtDjqDGe_1HXDH6jaXgiVIm3QONQvuuC8IyIRE4KvVKvKGKYIh-gs_abvrVC0BRclt7kGmMIAcaLx41WNzxnsCjVf4ibTeXHpll5zXLa9ljdMnypVsH9LgMArcQe2F1zvwahexikfvuNE60CP9IAA"));
                            //cookieContainer.Add(new Uri("https://" + new Uri(StringConstants.AADPoll).Authority), new Cookie("ESTSAUTH", "QVFBQkFBRUFBQURSTllSUTNkaFJTcm0tNEstYWRwQ0pCcnJLM3J0VzZrZzdZcURYdy1NeFZ0SHl4UVVXdmdaRVg5Ukg2NVZWejVhdk9EVExwLWFLV256UjVDdGdyOU1DTU13Zy15d0dFcDhNSE1NZDNyWldwQ0FB"));

                            string aadPollpostData = String.Format(StringConstants.AADPollBody, flowtoken, ctx);
                            LogManager.Verbose("Call AAD poll  postdata:" + aadPollpostData);
                            // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                            request.DefaultRequestHeaders.Add("canary", step1Canary);
                            request.DefaultRequestHeaders.Add("Referrer", "https://login.microsoftonline.com/common/login");
                            request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                            request.DefaultRequestHeaders.Add("client-request-id", clientRequestID);
                            request.DefaultRequestHeaders.Add("Accept", "application/json");
                            request.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                            request.DefaultRequestHeaders.Add("hpgid", hpgid);
                            request.DefaultRequestHeaders.Add("hpgact", hpgact);
                            //request.DefaultRequestHeaders.Add("Content-Length", aadPollpostData.Length.ToString());


                            Task<HttpResponseMessage> response = request.PostAsync((StringConstants.dssoPoll), new StringContent(StringConstants.dssoPollBody, Encoding.UTF8, "application/json"));
                            response.Wait();

                            if (response.Result.Content != null)
                            {
                                using (HttpContent content = response.Result.Content)
                                {
                                    Task<string> result = content.ReadAsStringAsync();
                                    apiCanaryResponse apiCanaryResponse = JsonConvert.DeserializeObject<apiCanaryResponse>(result.Result);
                                    step2Canary = apiCanaryResponse.apiCanary;
                                }
                            }
                        }

                        //append cookies
                        var postBodyContainer = new CookieContainer();
                        using (var pollHandler = new HttpClientHandler() { CookieContainer = postBodyContainer })
                        //call 2 again as per AAD connect
                        using (HttpClient request = new HttpClient(pollHandler))

                        {

                            string call1Canary = getCanary2(call1result);
                            string postData = String.Format(StringConstants.loginPostData, upn, password, ctx, flowtoken, call1Canary);
                            LogManager.Verbose("Call 3 postdata:" + postData);
                            // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                            Uri uri = new Uri(StringConstants.loginPost);
                            IEnumerable<Cookie> responseCookies = cookies.GetCookies(uri).Cast<Cookie>();

                            foreach (Cookie cookie in responseCookies)
                            {
                                postBodyContainer.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie(cookie.Name, cookie.Value));
                            }
                            postBodyContainer.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("testcookie", "testcookie"));
                            postBodyContainer.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("ESTSSSOTILES", "1"));
                            postBodyContainer.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("AADSSOTILES", "1"));
                            postBodyContainer.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                            postBodyContainer.Add(new Uri("https://" + new Uri(StringConstants.loginPost).Authority), new Cookie("ESTSSC", "00"));
                            //request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                            Task<HttpResponseMessage> response = request.PostAsync((StringConstants.loginPost), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));


                            response.Wait();

                            if (response.Result.Content != null)
                            {
                                using (HttpContent content = response.Result.Content)
                                {
                                    Task<string> result = content.ReadAsStringAsync();
                                    call3result = result.Result;
                                    CQ htmlparser = CQ.Create(result.Result);
                                    var items = htmlparser["input"];
                                    foreach (var li in items)
                                    {
                                        if (li.Name == "ctx")
                                        {
                                            if (!string.IsNullOrEmpty(li.Value))
                                                call3ctx = li.Value;
                                        }
                                        if (li.Name == "flowToken")
                                        {
                                            call3Flow = li.Value;
                                        }
                                    }

                                    call3hpgact = getHPGact(call3result);
                                    call3hpgid = getHPGId(call3result);
                                    call3canary = getapiCanary(call3result);

                                }
                            }
                        }
                        pollResponse pollStartresponse = new pollResponse();
                        var pollCookies = new CookieContainer();
                        using (var pollHandler = new HttpClientHandler() { CookieContainer = pollCookies })
                        //call 2 again as per AAD connect
                        using (HttpClient request = new HttpClient(pollHandler))
                        {

                            string postData = String.Format(StringConstants.AADPollBody, call3Flow, call3ctx);
                            LogManager.Verbose("Call 3 postdata:" + postData);
                            // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                            Uri uri = new Uri(StringConstants.loginPost);
                            IEnumerable<Cookie> responseCookies = postBodyContainer.GetCookies(uri).Cast<Cookie>();

                            foreach (Cookie cookie in responseCookies)
                            {
                                pollCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPoll).Authority), new Cookie(cookie.Name, cookie.Value));
                            }
                            pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("testcookie", "testcookie"));
                            //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSSOTILES", "1"));
                            //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("AADSSOTILES", "1"));
                            //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                            //pollCookies.Add(new Uri("https://" + new Uri(StringConstants.dssoPoll).Authority), new Cookie("ESTSSC", "00"));

                            request.DefaultRequestHeaders.Add("canary", call3canary);
                            request.DefaultRequestHeaders.Add("Referrer", "https://login.microsoftonline.com/common/login");
                            request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                            request.DefaultRequestHeaders.Add("client-request-id", clientRequestID);
                            request.DefaultRequestHeaders.Add("Accept", "application/json");
                            request.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                            request.DefaultRequestHeaders.Add("hpgid", call3hpgid);
                            request.DefaultRequestHeaders.Add("hpgact", call3hpgact);

                            Task<HttpResponseMessage> response = request.PostAsync((StringConstants.AADPoll), new StringContent(postData, Encoding.UTF8, "application/json"));


                            response.Wait();

                            if (response.Result.Content != null)
                            {
                                using (HttpContent content = response.Result.Content)
                                {
                                    Task<string> result = content.ReadAsStringAsync();
                                    pollStartresponse = JsonConvert.DeserializeObject<pollResponse>(result.Result);
                                    //AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(result.Result);
                                    //   var token = JsonConvert.SerializeObject( result.Result);
                                    //access_token = tokenresponse.AccessToken;
                                }
                            }
                        }

                        //call 2 again as per AAD connect
                        var pollendCookies = new CookieContainer();
                        
                        using (var pollHandler = new HttpClientHandler() { CookieContainer = pollendCookies })
                        using (HttpClient request = new HttpClient(pollHandler))
                        {

                            string postData = String.Format(StringConstants.AADPollEndBody, pollStartresponse.flowToken, pollStartresponse.ctx);
                            LogManager.Verbose("Call 3 postdata:" + postData);
                            // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                            Uri uri = new Uri(StringConstants.loginPost);
                            IEnumerable<Cookie> responseCookies = postBodyContainer.GetCookies(uri).Cast<Cookie>();

                            foreach (Cookie cookie in responseCookies)
                            {
                                pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie(cookie.Name, cookie.Value));
                            }
                            pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie("testcookie", "testcookie"));
                            //pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie("ESTSSSOTILES", "1"));
                            //pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie("AADSSOTILES", "1"));
                            //pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie("ESTSAUTHLIGHT", "+"));
                            //pollendCookies.Add(new Uri("https://" + new Uri(StringConstants.AADPollEnd).Authority), new Cookie("ESTSSC", "00"));

                            //request.DefaultRequestHeaders.Add("canary", call3canary);
                            request.DefaultRequestHeaders.Add("Referrer", "https://login.microsoftonline.com/common/login");
                            request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko");
                            //request.DefaultRequestHeaders.Add("client-request-id", clientRequestID);
                            request.DefaultRequestHeaders.Add("Accept", "application/json");
                            request.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                            //request.DefaultRequestHeaders.Add("hpgid", call3hpgid);
                            //request.DefaultRequestHeaders.Add("hpgact", call3hpgact);

                            Task<HttpResponseMessage> response = request.PostAsync((StringConstants.AADPollEnd), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));


                            response.Wait();

                            if (response.Result.Content != null)
                            {
                                using (HttpContent content = response.Result.Content)
                                {
                                    Task<string> result = content.ReadAsStringAsync();
                                    call2result = result.Result;

                                    // Make sure its authenticated
                                    if (response.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                                    {
                                        //LogManager.Verbose("Login failed :(");
                                        //return string.Empty;
                                    }
                                    // ... Read the string.

                                    NameValueCollection qscoll = HttpUtility.ParseQueryString(response.Result.RequestMessage.RequestUri.Query);
                                    if (qscoll.Count > 0)
                                        code = qscoll[0];


                                    //AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(result.Result);
                                    //   var token = JsonConvert.SerializeObject( result.Result);
                                    //access_token = tokenresponse.AccessToken;
                                }
                            }
                        }


                        string access_token = string.Empty;
                        using (HttpClient request = new HttpClient())
                        {

                            string postData = String.Format(StringConstants.AzureActivationUserToken, code);
                            LogManager.Verbose("Call 3 postdata:" + postData);
                            // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                            Task<HttpResponseMessage> response = request.PostAsync((StringConstants.AzureActivateUserStep3), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));


                            response.Wait();

                            if (response.Result.Content != null)
                            {
                                using (HttpContent content = response.Result.Content)
                                {
                                    Task<string> result = content.ReadAsStringAsync();

                                    AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(result.Result);
                                    //   var token = JsonConvert.SerializeObject( result.Result);
                                    access_token = tokenresponse.AccessToken;
                                }
                            }
                        }


                        using (HttpClient request = new HttpClient())
                        {
                            LogManager.Verbose("Call 3 url: " + String.Format(StringConstants.AzureActivateUserStep4, upn));
                            request.DefaultRequestHeaders.Add("authorization", "bearer " + access_token);
                            Task<HttpResponseMessage> response = request.GetAsync(String.Format(StringConstants.AzureActivateUserStep4, upn));
                            response.Wait();

                            if (response.Result.Content != null)
                            {
                                using (HttpContent content = response.Result.Content)
                                {
                                    Task<string> result = content.ReadAsStringAsync();
                                    MysiteResponse tokenresponse = JsonConvert.DeserializeObject<MysiteResponse>(result.Result);
                                    string rootSiteURL = tokenresponse.value.FirstOrDefault(u => (u.entityKey.ToLower().Contains(StringConstants.rootUrlFinder))).serviceResourceId;

                                    string rootSitedocLibUrl = rootSiteURL.EndsWith("/") ? rootSiteURL + "Shared Documents" : rootSiteURL + "/Shared Documents";

                                    //as this is going to be needed at many places, we will save it 
                                    DriveManager.rootSiteUrl = rootSiteURL;

                                    if (!string.IsNullOrEmpty(rootSiteURL))
                                    {
                                        Uri url = new Uri(rootSiteURL);
                                        tenancyUniqueName = url.Host.ToLower().Replace(StringConstants.rootUrltobeRemoved, "");
                                    }

                                    //Set onedrive host
                                    DriveManager.oneDriveHostSiteUrl = "https://" + tenancyUniqueName + "-my.sharepoint.com";
                                }
                            }
                        }

                        LogManager.Verbose("Call 4 finished");
                        LogManager.Verbose("tenancy name: " + tenancyUniqueName + StringConstants.rootUrltobeReplacedWith);
                        tenancyUniqueName += tenancyUniqueName + StringConstants.rootUrltobeReplacedWith;
                    }
                }
                ///Continue to cloud auth journey
                //Getting user activation step 3
                else
                {
                    string access_token = string.Empty;
                    using (HttpClient request = new HttpClient())
                    {

                        string postData = String.Format(StringConstants.AzureActivationUserToken, code);
                        LogManager.Verbose("Call 3 postdata:" + postData);
                        // request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                        Task<HttpResponseMessage> response = request.PostAsync((StringConstants.AzureActivateUserStep3), new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));


                        response.Wait();

                        if (response.Result.Content != null)
                        {
                            using (HttpContent content = response.Result.Content)
                            {
                                Task<string> result = content.ReadAsStringAsync();

                                AADJWTToken tokenresponse = JsonConvert.DeserializeObject<AADJWTToken>(result.Result);
                                //   var token = JsonConvert.SerializeObject( result.Result);
                                access_token = tokenresponse.AccessToken;
                            }
                        }
                    }
                    LogManager.Verbose("Call 3 finished");
                    LogManager.Verbose("Access token:" + access_token);

                    //Getting user activation step 4
                    //string Step4 = "https://api.office.com/discovery/v2.0/me/services";

                    //request.DefaultRequestHeaders.Add("Accept-Language", "en-US");
                    using (HttpClient request = new HttpClient())
                    {
                        LogManager.Verbose("Call 3 url: " + String.Format(StringConstants.AzureActivateUserStep4, upn));
                        request.DefaultRequestHeaders.Add("authorization", "bearer " + access_token);
                        Task<HttpResponseMessage> response = request.GetAsync(String.Format(StringConstants.AzureActivateUserStep4, upn));
                        response.Wait();

                        if (response.Result.Content != null)
                        {
                            using (HttpContent content = response.Result.Content)
                            {
                                Task<string> result = content.ReadAsStringAsync();
                                MysiteResponse tokenresponse = JsonConvert.DeserializeObject<MysiteResponse>(result.Result);
                                string rootSiteURL = tokenresponse.value.FirstOrDefault(u => (u.entityKey.ToLower().Contains(StringConstants.rootUrlFinder))).serviceResourceId;

                                string rootSitedocLibUrl = rootSiteURL.EndsWith("/") ? rootSiteURL + "Shared Documents" : rootSiteURL + "/Shared Documents";

                                //as this is going to be needed at many places, we will save it 
                                DriveManager.rootSiteUrl = rootSiteURL;

                                if (!string.IsNullOrEmpty(rootSiteURL))
                                {
                                    Uri url = new Uri(rootSiteURL);
                                    tenancyUniqueName = url.Host.ToLower().Replace(StringConstants.rootUrltobeRemoved, "");
                                }

                                //Set onedrive host
                                DriveManager.oneDriveHostSiteUrl = "https://" + tenancyUniqueName + "-my.sharepoint.com";
                            }
                        }
                    }

                    LogManager.Verbose("Call 4 finished");
                    LogManager.Verbose("tenancy name: " + tenancyUniqueName + StringConstants.rootUrltobeReplacedWith);
                    tenancyUniqueName += tenancyUniqueName + StringConstants.rootUrltobeReplacedWith;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            //return the required name
            return tenancyUniqueName;
        }


        /// <summary>
        /// Get user realM by posting to office 365
        /// </summary>
        /// <param name="upn">User principal name</param>
        /// <returns></returns>
        static FedType userRealM(string upn)
        {
            using (HttpClient request = new HttpClient())
            {

                LogManager.Verbose("Get request: " + String.Format(StringConstants.UserrealMrequest, upn));
                Task<HttpResponseMessage> response = request.GetAsync(String.Format(StringConstants.UserrealMrequest, upn));
                response.Wait();

                if (response.Result.Content != null)
                {
                    using (HttpContent content = response.Result.Content)
                    {
                        // ... Read the string.
                        Task<string> result = content.ReadAsStringAsync();
                    }

                }
            }
            return FedType.Cloud;
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
        static string getapiCanary(string response)
        {
            int indexofCanary = response.IndexOf("\"apiCanary\":") + 13;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string Canary = response.Substring(indexofCanary, endIndex - indexofCanary);
            return Canary;
        }

        static string getCanary2(string response)
        {
            int indexofCanary = response.IndexOf("\"canary\":") + 10;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string Canary = response.Substring(indexofCanary, endIndex - indexofCanary);
            return Canary;
        }
        static string getClientRequest(string response)
        {
            int indexofCanary = response.IndexOf("\"correlationId\":") + 17;
            int endIndex = response.IndexOf("\"", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }

        static string getHPGact(string response)
        {
            int indexofCanary = response.IndexOf("\"hpgact\":") + 9;
            int endIndex = response.IndexOf(",", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }

        static string getHPGId(string response)
        {
            int indexofCanary = response.IndexOf("\"hpgid\":") + 8;
            int endIndex = response.IndexOf(",", indexofCanary + 1);
            string clientRequest = response.Substring(indexofCanary, endIndex - indexofCanary);
            return clientRequest;
        }
    }
}