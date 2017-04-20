using System;

namespace _365Drive.Office365.GetTenancyURL.CookieManager
{
    public class SamlSecurityToken
    {
        public byte[] Token
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
