using _365Drive.Office365.GetTenancyURL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.UpdateManager
{
    public static class Versions
    {
        static VersionResponse _versionResponse;


        /// <summary>
        /// compare two versions 
        /// </summary>
        /// <param name="oldVersion">the current version</param>
        /// <param name="newVersion">version received from api</param>
        public static bool compareVersion(string oldVersion, string newVersion)
        {
            try
            {
                Version vOldVersion = new Version(oldVersion);
                Version vnewVersion = new Version(newVersion);

                var result = vnewVersion.CompareTo(vOldVersion);

                if (result > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return false;
        }


        public static VersionResponse LatestVersion()
        {
            if (_versionResponse != null)
                return _versionResponse;

            try
            {
                string latestVersionGetUrl = String.Format(Constants.latestVersionUrl, Constants.licensingBaseDomain, Constants.ieLatestVersion);

                //get the initial license details
                Task<string> versionCall = Task.Run(() => HttpClientHelper.GetAsync(latestVersionGetUrl));
                versionCall.Wait();

                string versionResult = versionCall.Result;

                _versionResponse = JsonConvert.DeserializeObject<VersionResponse>(versionResult);

                return _versionResponse;
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return _versionResponse;
        }

    }
}
