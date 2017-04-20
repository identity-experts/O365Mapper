using System;

namespace _365Drive.Office365.GetTenancyURL.CookieManager
{
    internal class SPOAuthCookies
    {
        public string FedAuth
        {
            get;
            set;
        }

        public string RtFA
        {
            get;
            set;
        }

        public Uri Host
        {
            get;
            set;
        }

        public DateTime Expires
        {
            get;
            set;
        }
    }
}
