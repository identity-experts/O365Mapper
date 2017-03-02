using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365
{
    public static class Constants
    {
        /// <summary>
        /// Registry constants
        /// </summary>
        public const string registryRoot = @"software\Identity Experts\365Drive";
        public const string mappingregKey = @"mappings";
        public const string exeName = "365Drive.Office365.NotificationManager.exe";
        public const string regStartupAppName = "365Drive";
        public const string lServiceName = "IdentityExperts";
        public const string lLogName = "Application";
        public const int lLogeventId = 1702;
        public const string animationIconNamePrefix = "IE-ProgressAnimation";
        public const string animationIconWaitNamePrefix = "Wait";
        public static readonly string[] oneDriveIdentifier = { "odb", "onedrive", "onedrive for business", "mysite" };
        public static readonly string[] spDefaultDriveIdentifier = { "default", "sharepoint", "spo", "sponline" };
        public static readonly string[] deleteDriveIdentifier = { "x", "d", "r" };
    }
}
