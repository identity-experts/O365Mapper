using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace _365Drive.Office365.CloudConnector
{
    public static class DriveMapper
    {
        /// <summary>
        /// Ensures whether user has valid license or not
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static LicenseValidationState EnsureLicense(string userName, string password, CookieContainer userCookies)
        {


            var licenseMode = LicenseValidationState.CouldNotVerify;

            ///ONLY FOR TESTING STOP ICON
            /// 
            //return licenseMode;
            try
            {
                if (!Utility.ready())
                    return LicenseValidationState.CouldNotVerify;

                LogManager.Verbose("ensuring license");

                //get the tenancy name
                string tenancyName = _365DriveTenancyURL.Get365TenancyName(userName, password);
                if (!string.IsNullOrEmpty(tenancyName))
                {
                    LogManager.Verbose("call to license valid");
                    if (LicenseManager.licenseCheckTimeNow && LicenseManager.lastLicenseState == null)
                    {
                        LicenseValidationState state = LicenseManager.isLicenseValid(tenancyName, userName, userCookies);
                        LogManager.Verbose("license validation result: " + Convert.ToString(state));
                        licenseMode = state;
                        LicenseManager.lastLicenseState = state;
                    }
                    else
                    {
                        licenseMode = (LicenseValidationState)LicenseManager.lastLicenseState;
                    }
                }
                else
                {
                    licenseMode = LicenseValidationState.LoginFailed;
                }

            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return licenseMode;
        }


        /// <summary>
        /// Ensures whether user has valid license or not
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static LicenseValidationState retrieveTenancyName(string userName, string password)
        {


            var licenseMode = LicenseValidationState.CouldNotVerify;

            ///ONLY FOR TESTING STOP ICON
            /// 
            //return licenseMode;
            try
            {
                if (!Utility.ready())
                    return LicenseValidationState.CouldNotVerify;

                LogManager.Verbose("ensuring license");

                //get the tenancy name
                string tenancyName = _365DriveTenancyURL.Get365TenancyName(userName, password);
                if (string.IsNullOrEmpty(tenancyName))
                {
                    licenseMode = LicenseValidationState.LoginFailed;
                }
                else if (tenancyName == "0")
                {
                    return LicenseValidationState.MFARemindLater;
                }
                else
                {
                    return LicenseValidationState.Ok;
                }

            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return licenseMode;
        }

        /// <summary>
        /// Ensure user has access, if yes then map it
        /// </summary>
        /// <returns></returns>
        public static bool userHasAccess(Uri docLibUrl, CookieContainer authCookies)
        {
            try
            {
                if (!Utility.ready())
                    return false;

                LogManager.Verbose("Inside ensure access");
                var responsereceived = true;
                while (responsereceived)
                {
                    try
                    {
                        var doclibrequest = (HttpWebRequest)WebRequest.Create(docLibUrl);
                        doclibrequest.CookieContainer = authCookies;
                        doclibrequest.Method = "GET";
                        doclibrequest.ContentType = "text/xml";
                        doclibrequest.Accept = "*/*";
                        doclibrequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                        doclibrequest.Headers.Add("X-RequestForceAuthentication", "true");
                        doclibrequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "t");
                        //doclibrequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";

                        if (WebRequest.DefaultWebProxy.GetProxy(docLibUrl).ToString() != docLibUrl.ToString())
                        {
                            LogManager.Verbose("Proxy found: " + WebRequest.DefaultWebProxy.GetProxy(docLibUrl).ToString());
                            WebProxy proxy = new WebProxy((WebRequest.DefaultWebProxy.GetProxy(docLibUrl)));
                            proxy.Credentials = CredentialCache.DefaultCredentials;
                            proxy.UseDefaultCredentials = true;
                            doclibrequest.Proxy = proxy;
                        }

                        var responseValue = string.Empty;

                        LogManager.Verbose("Hitting to check user has accees");
                        using (var response = (HttpWebResponse)doclibrequest.GetResponse())
                        {
                            responsereceived = false;
                            LogManager.Verbose("response code for ensure access drive: " + docLibUrl.ToString() + " is " + response.StatusDescription);
                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("403"))
                        {
                            responsereceived = false;
                            return false;
                        }
                        if (ex.Message.Contains("404"))
                        {
                            responsereceived = false;
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return true;
        }



        /// <summary>
        /// Retrieve the user mysite URL
        /// </summary>
        /// <param name="host"></param>
        /// <param name="authCookies"></param>
        /// <returns></returns>
        public static string getOneDriveUrl(string host, CookieContainer authCookies)
        {
            string oneDriveUrl = string.Empty;
            try
            {
                if (!Utility.ready())
                    return string.Empty;
                //get the request digest which will be neeeded to fetch the mysite URL
                LogManager.Verbose("Retrieving user's onedrive Url");

                string requestDigest = getRequestDigest(host, authCookies);
                LogManager.Verbose("got request digest:" + requestDigest);

                //get mysite url now


                //initialize Webrequest
                var oneDriveUrlCall = (HttpWebRequest)WebRequest.Create((host.EndsWith("//") ? host : host + "//") + "_api/SP.UserProfiles.PeopleManager/GetMyProperties/personalURL");

                //Set header info along with digest value
                oneDriveUrlCall.Accept = "application/json;odata=verbose";
                oneDriveUrlCall.ContentType = "application/json;odata=verbose";
                oneDriveUrlCall.CookieContainer = authCookies;
                oneDriveUrlCall.Method = "POST";
                oneDriveUrlCall.ContentLength = 0;
                oneDriveUrlCall.Headers.Add("X-RequestDigest", requestDigest);


                //Make sure it doesnt have any proxy
                if (WebRequest.DefaultWebProxy.GetProxy(new Uri(host)) != new Uri(host))
                {
                    try
                    {
                        LogManager.Verbose("Proxy found: " + WebRequest.DefaultWebProxy.GetProxy(new Uri(host)).ToString());
                        WebProxy proxy = new WebProxy((WebRequest.DefaultWebProxy.GetProxy(new Uri(host))));
                        proxy.Credentials = CredentialCache.DefaultCredentials;
                        proxy.UseDefaultCredentials = true;
                        oneDriveUrlCall.Proxy = proxy;
                    }
                    catch (Exception ex) { }
                }

                try
                {
                    using (var response = (HttpWebResponse)oneDriveUrlCall.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var digestResponse = response.GetResponseStream())
                            {
                                LogManager.Verbose("response code for oneDrive Url: " + host.ToString() + " is " + response.StatusDescription);

                                if (digestResponse != null)
                                    using (var rDigestResponse = new StreamReader(digestResponse))
                                    {
                                        var digestBytes = default(byte[]);
                                        using (var memstream = new MemoryStream())
                                        {
                                            rDigestResponse.BaseStream.CopyTo(memstream);
                                            digestBytes = memstream.ToArray();
                                        }
                                        var digestReader = JsonReaderWriterFactory.CreateJsonReader(digestBytes, new System.Xml.XmlDictionaryReaderQuotas());
                                        var rootNode = XElement.Load(digestReader);
                                        XmlDocument responseDoc = new XmlDocument();
                                        responseDoc.LoadXml(rootNode.ToString());
                                        string oneDrivepath = "root/d/PersonalUrl";
                                        var digestNodes = responseDoc.SelectNodes(oneDrivepath);
                                        oneDriveUrl = digestNodes[0].InnerText;
                                    }
                            }
                        }
                        ///error received
                        else
                        {
                            LogManager.Verbose("Could not retrive digest, response: " + response.StatusDescription);
                        }
                    }
                }
                catch (Exception ex) { }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            //return retrieved oneDrive Url 
            return oneDriveUrl.EndsWith("/") ? oneDriveUrl + "Documents" : oneDriveUrl + "/Documents";
        }




        /// <summary>
        /// get the request digest value which can be used to retrieve / post the mysite url or anything else from office 365
        /// </summary>
        /// <param name="host"></param>
        /// <param name="authCookies"></param>
        /// <returns></returns>
        static string getRequestDigest(string host, CookieContainer authCookies)
        {
            string requestDigest = string.Empty;
            try
            {
                if (!Utility.ready())
                    return string.Empty;

                LogManager.Verbose("Retrieving request digest");

                //initialize Webrequest
                var requestDigestCall = (HttpWebRequest)WebRequest.Create((host.EndsWith("//") ? host : host + "//") + "_api/contextinfo");

                //Set header info
                requestDigestCall.Accept = "application/json;odata=verbose";
                requestDigestCall.ContentType = "application/json;odata=verbose";
                requestDigestCall.CookieContainer = authCookies;
                requestDigestCall.Method = "POST";
                requestDigestCall.ContentLength = 0;

                //Make sure it doesnt have any proxy
                if (WebRequest.DefaultWebProxy.GetProxy(new Uri(host)) != new Uri(host))
                {
                    LogManager.Verbose("Proxy found: " + WebRequest.DefaultWebProxy.GetProxy(new Uri(host)).ToString());
                    WebProxy proxy = new WebProxy((WebRequest.DefaultWebProxy.GetProxy(new Uri(host))));
                    proxy.Credentials = CredentialCache.DefaultCredentials;
                    proxy.UseDefaultCredentials = true;
                    requestDigestCall.Proxy = proxy;
                }

                try
                {
                    using (var response = (HttpWebResponse)requestDigestCall.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var digestResponse = response.GetResponseStream())
                            {
                                LogManager.Verbose("response code for ensure access drive: " + host.ToString() + " is " + response.StatusDescription);

                                if (digestResponse != null)
                                    using (var rDigestResponse = new StreamReader(digestResponse))
                                    {
                                        var digestBytes = default(byte[]);
                                        using (var memstream = new MemoryStream())
                                        {
                                            rDigestResponse.BaseStream.CopyTo(memstream);
                                            digestBytes = memstream.ToArray();
                                        }
                                        var digestReader = JsonReaderWriterFactory.CreateJsonReader(digestBytes, new System.Xml.XmlDictionaryReaderQuotas());
                                        var rootNode = XElement.Load(digestReader);
                                        XmlDocument responseDoc = new XmlDocument();
                                        responseDoc.LoadXml(rootNode.ToString());
                                        string xpath = "root/d/GetContextWebInformation/FormDigestValue";
                                        var digestNodes = responseDoc.SelectNodes(xpath);
                                        requestDigest = digestNodes[0].InnerText;
                                    }
                            }
                        }
                        ///error received
                        else
                        {
                            LogManager.Verbose("Could not retrive digest, response: " + response.StatusDescription);
                        }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return requestDigest;
        }
    }
}
