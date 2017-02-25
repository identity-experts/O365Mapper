using CsQuery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
            try
            {
                if (!Utility.ready())
                    return string.Empty;

                // Getting user activation step 1
                string ctx = string.Empty;
                string flowtoken = string.Empty;
                using (HttpClient request = new HttpClient())
                {

                    LogManager.Verbose("Get request: " + String.Format(StringConstants.AzureActivateUserStep1, upn));
                    Task<HttpResponseMessage> response = request.GetAsync(String.Format(StringConstants.AzureActivateUserStep1, upn));
                    response.Wait();

                    if (response.Result.Content != null)
                    {
                        using (HttpContent content = response.Result.Content)
                        {
                            // ... Read the string.
                            Task<string> result = content.ReadAsStringAsync();

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

                    response.Wait();

                    if (response.Result.Content != null)
                    {
                        using (HttpContent content = response.Result.Content)
                        {

                            // Make sure its authenticated
                            if (response.Result.RequestMessage.RequestUri.ToString().ToLower() == StringConstants.FailedLoginUrl.ToLower())
                            {
                                LogManager.Verbose("Login failed :(");
                                return string.Empty;
                            }
                            // ... Read the string.

                            NameValueCollection qscoll = HttpUtility.ParseQueryString(response.Result.RequestMessage.RequestUri.Query);
                            code = qscoll[0];

                        }
                    }
                }
                LogManager.Verbose("Call 2 finished");

                LogManager.Verbose("Call 2 code:" + code);
                //Getting user activation step 3
                //string AzureActivationUserToken = "client_id=df166692-81e3-404b-97ae-d09038e90d8c&redirect_uri=http://identityexperts.co.uk&client_secret=nIEIuEyhATgLdFqhcHCMTJKky3QiXDQ7eYcQjkjhmPc=&code={0}&grant_type=authorization_code&resource=https://api.office.com/discovery/";
                //string AzureActivateUserStep3 = "https://login.microsoftonline.com/common/oauth2/token";

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
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            //return the required name
            return tenancyUniqueName;
        }

    }
}