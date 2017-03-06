using _365Drive.Office365.CloudConnector;
using _365Drive.Office365.GetTenancyURL.CookieManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.GetTenancyURL
{
    public class GlobalCookieManager
    {
        #region Properties

        readonly string _username;
        readonly string _password;
        readonly bool _useRtfa;
        readonly Uri _host;

        CookieContainer _cachedCookieContainer = null;

        #endregion

        public GlobalCookieManager(string host, string username, string password)
            : this(new Uri(host), username, password)
        {

        }
        public GlobalCookieManager(Uri host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            _useRtfa = true;
        }

        public CookieContainer getCookieContainer()
        {
            CookieContainer userCookies = new CookieContainer();

            if (DriveManager.FederationType == FedType.AAD)
            {
                AADConnectCookieManager cookieManager = new AADConnectCookieManager(_host.ToString(), _username, _password);
                userCookies = cookieManager.getCookieContainer();
            }
            else if(DriveManager.FederationType == FedType.Cloud)
            {
                o365cookieManager cookieManager = new o365cookieManager(_host.ToString(),_username,_password);
                userCookies = cookieManager.getCookieContainer();
            }

            return userCookies;
        }
    }
}
