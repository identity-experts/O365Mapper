using _365Drive.Office365.CloudConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace _365Drive.Office365.UI
{
    public static class ValidatorExtensions
    {


        /// <summary>
        /// Make sure given string is valid email address or not
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsValidEmailAddress(this string s)
        {
            Regex regex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
            return regex.IsMatch(s);
        }

        public static bool IsValidRootSiteURL(this string s)
        {
            if (s.ToLower().Contains(StringConstants.rootUrltobeRemoved))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Get the resouce string value by passing resource key as a parameter
        /// </summary>
        /// <returns></returns>
        //public static string ResourceValue(string resourceKey)
        //{
        //    _365Drive.Office365.UI.Globalization.Globalization.credentials;
        //}
    }
}
