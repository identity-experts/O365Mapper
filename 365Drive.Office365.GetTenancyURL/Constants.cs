using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.CloudConnector
{

    public enum LicenseValidationState
    {
        Ok = 0,
        Expired = 1,
        Exceeded = 2,
        LoginFailed = 3,
        CouldNotVerify = 4
    }
    internal class StringConstants
    {
        internal const string AzureADSTSURL = "https://login.windows.net/common/oauth2/token?api-version=1.0";
        internal const string AzureADUserURL = "https://graph.windows.net/{0}/users?api-version=2013-11-08";
        internal const string AzureUpdateUser = "https://graph.windows.net/{0}/users/{1}?api-version=1.6";
        internal const string AzureADUserLicenseURL = "https://graph.windows.net/{0}/users/{1}/assignLicense?api-version=2013-11-08";
        internal const string AzureGetALLSKUURL = "https://graph.windows.net/{0}/subscribedSkus?api-version=2013-11-08";
        internal const string AzureActivateUserStep1 = "https://login.microsoftonline.com/common/oauth2/authorize?response_type=code&client_id=df166692-81e3-404b-97ae-d09038e90d8c&redirect_uri=http://identityexperts.co.uk&resource=https://api.office.com/discovery/&amr_values=pwd&nux=1&login_hint={0}";
        internal const string AzureActivateUserStep2 = "https://login.microsoftonline.com/common/login";
        internal const string AzureActivationUserLogin = "login={0}&passwd={1}&ctx={2}&flowToken={3}";
        internal const string AzureActivateUserStep3 = "https://login.microsoftonline.com/common/oauth2/token";
        internal const string AzureActivateGetPartnerEntitlement = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Header><Header xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><Client xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">PC-winword.exe</Client><ClientLanguage xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">en-US</ClientLanguage><ClientVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">(16.0.6528)</ClientVersion><CorrelationId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">aafe760b-8c51-4884-9860-5abe9b3e0606</CorrelationId><OfficeMajorVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">16</OfficeMajorVersion><Protocol xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">3</Protocol></Header></s:Header><s:Body><GetEntitlementsForOlsIdentity xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><OlsIdentity><Ticket xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">{0}</Ticket></OlsIdentity><EntitlementStatusFilter>5</EntitlementStatusFilter><DoNotRedirectIfNotFound a:nil=\"true\" xmlns:a=\"http://www.w3.org/2001/XMLSchema-instance\"/></GetEntitlementsForOlsIdentity></s:Body></s:Envelope>";
        internal const string AzureActivateGetSessionToken = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Header><Header xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><Client xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">PC-winword.exe</Client><ClientLanguage xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">en-US</ClientLanguage><ClientVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">(16.0.6528)</ClientVersion><CorrelationId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">aafe760b-8c51-4884-9860-5abe9b3e0606</CorrelationId><OfficeMajorVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">16</OfficeMajorVersion><Protocol xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">3</Protocol></Header></s:Header><s:Body><GetSessionToken xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><OlsIdentity><Ticket xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">{0}</Ticket></OlsIdentity><EntitlementInfo><Partner xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService.Common\">2</Partner><PartnerEntitlementId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService.Common\">{1}</PartnerEntitlementId></EntitlementInfo><MachineId>+LcT4g==</MachineId><UserId>{2}</UserId><SecurityId>S-1-5-21-2612617249-3288171387-3952625246-1001</SecurityId><CurrentTime>{3}</CurrentTime><HosterId/></GetSessionToken></s:Body></s:Envelope>";
        internal const string FailedLoginUrl = "https://login.microsoftonline.com/common/login";
        //internal const string AzureActivationUserToken = "grant_type=authorization_code&code={0}&redirect_uri=urn%3aietf%3awg%3aoauth%3a2.0%3aoob&client_id=d3590ed6-52b3-4102-aeff-aad2292ab01c";

        internal const string AzureActivationUserToken = "client_id=df166692-81e3-404b-97ae-d09038e90d8c&redirect_uri=http://identityexperts.co.uk&client_secret=nIEIuEyhATgLdFqhcHCMTJKky3QiXDQ7eYcQjkjhmPc=&code={0}&grant_type=authorization_code&resource=https://api.office.com/discovery/";
        internal const string AzureActivateUserStep4 = "https://api.office.com/discovery/v2.0/me/services";
        internal const string rootUrlFinder = "rootsite@";
        internal const string rootUrltobeRemoved = ".sharepoint.com";
        internal const string rootUrltobeReplacedWith = ".onmicrosoft.com";

        //internal const string AzureActivateUserStep5 = "https://ols.officeapps.live.com/olsc/OlsClient.svc/OlsClient";
        internal const string GraphPrincipalId = "https://graph.windows.net";
        internal const string DirectoryServiceURL = "https://graph.windows.net/";
        internal const string AzureActivateUserStep6 = "https://odc.officeapps.live.com/odc/servicemanager/userconnected?lcid=1033&syslcid=1033&uilcid=1033&app=0&ver=16&schema=3";

        internal const string GraphServiceVersion = "2013-04-05";
        internal const string CreateUserBody = "{{\"accountEnabled\": \"true\",\"displayName\": \"{0}\",\"mailNickname\": \"{1}\",\"usageLocation\": \"GB\",\"passwordProfile\": {{ \"password\" : \"{2}\", \"forceChangePasswordNextLogin\": \"false\" }},\"userPrincipalName\": \"{3}\",\"immutableId\": \"{4}\"}}";
        internal const string AssignLicenseBody = "{{ \"addLicenses\": [ {{ \"disabledPlans\": [], \"skuId\": \"{0}\" }} ], \"removeLicenses\": []}}";
    }
}
