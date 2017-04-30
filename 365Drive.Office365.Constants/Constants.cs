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
        public const string registryRoot = @"software\Identity Experts\365mapper";

        public const string fileUploadLimitKey = @"SYSTEM\CurrentControlSet\Services\WebClient\Parameters";
        public const string enableLinkedConnectionsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

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

        #region licensing server
        public const string licensingBaseDomain = "http://admin.identityexperts.co.uk";
        public const string statusCheckUrl = "{0}?wc-api={1}&email={2}&licence_key={3}&request={4}&product_id={5}&instance={6}&platform={7}&software_version={8}";
        public const string activationUrl = "{0}?wc-api={1}&email={2}&licence_key={3}&request={4}&product_id={5}&instance={6}&platform={7}&software_version={8}&tenancy={9}";
        public const string activationApiCode = "am-software-api";
        public const string ieUserMappingApiCode = "user-key-mapping-api";
        public const string ieDriveDetailsApiCode = "ie-drive-mapping-details";
        public const string activationRequestName = "activation";
        public const string statusRequestName = "status";
        public const string ie365MapperProductName = "365Mapper";
        public const string licenseuserMappingUrl = "{0}?wc-api={1}&tenancy={2}&user={3}";
        public const string retrieveDriveMappingsUrl = "{0}?wc-api={1}&tenancy={2}";

        //beta
        public const string softwareVersion = "0.1.0";

        public const int licenseCheckInterval = 120;
        public const int localLicenseCheckLimit = 24;
        public const int localDriveFetchLimit = 24;
        #endregion

    }
}
