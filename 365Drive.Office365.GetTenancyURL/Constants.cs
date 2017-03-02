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
        internal const string UserrealMrequest = "https://login.microsoftonline.com/GetUserRealm.srf?login={0}&handler=1&extended=1";
        internal const string AzureADSTSURL = "https://login.windows.net/common/oauth2/token?api-version=1.0";
        internal const string AzureADUserURL = "https://graph.windows.net/{0}/users?api-version=2013-11-08";
        internal const string AzureUpdateUser = "https://graph.windows.net/{0}/users/{1}?api-version=1.6";
        internal const string AzureADUserLicenseURL = "https://graph.windows.net/{0}/users/{1}/assignLicense?api-version=2013-11-08";
        internal const string AzureGetALLSKUURL = "https://graph.windows.net/{0}/subscribedSkus?api-version=2013-11-08";
        internal const string AzureActivateUserStep1 = "https://login.microsoftonline.com/common/oauth2/authorize?response_type=code&client_id=df166692-81e3-404b-97ae-d09038e90d8c&redirect_uri=http://identityexperts.co.uk&resource=https://api.office.com/discovery/&amr_values=pwd&nux=1&login_hint={0}";
        internal const string AzureActivateUserStep2 = "https://login.microsoftonline.com/common/login";
        internal const string AzureActivationUserLogin = "login={0}&passwd={1}&ctx={2}&flowToken={3}";
        internal const string AzureActivateUserStep3 = "https://login.microsoftonline.com/common/oauth2/token";
        internal const string AADPoll = "https://login.microsoftonline.com/common/onpremvalidation/Poll";
        internal const string dssoPoll = "https://login.microsoftonline.com/common/instrumentation/dssostatus";
        internal const string loginPost = "https://login.microsoftonline.com/common/login";
        internal const string loginPostData = "login={0}&passwd={1}&ctx={2}&flowToken={3}&canary={4}&dssoToken=&n1=104502&n2=-1488344436000&n3=-1488344436000&n4=104502&n5=104502&n6=104502&n7=104502&n8=NaN&n9=104502&n10=104502&n11=104502&n12=104663&n13=104502&n14=104827&n15=60&n16=104897&n17=104898&n18=104904&n19=1109.2413112395688&n20=1&n21=0&n22=0&n23=1&n24=45.49044898147713&n25=2&n26=0&n27=0&n28=0&n29=23.89304877782797&n30=0.1526726045049145&n31=0&n32=1&n33=1&n34=48.88773517247546&n35=0&n36=0&n37=76.70878919505594&n38=1&n39=49.78367104484914&n40=0&n41=404.46821822517455&n42=352.2768531653051&n43=402.2914575615063&type=11&LoginOptions=2&NewUser=1&idsbho=1&PwdPad=&sso=&vv=&uiver=1&i12=1&i13=MSIE&i14=7.0&i15=604&i16=552&i20=";

        internal const string AADPollEnd = "https://login.microsoftonline.com/common/onpremvalidation/End";
        internal const string AADPollEndBody = "flowToken={0}&ctx={1}";

        internal const string dssoPollBody = "{\"resultCode\":  0,\"ssoDelay\":  117,\"log\":  null}";
        internal const string AADPollBody = "{{\"flowToken\":\"{0}\",\"ctx\":\"{1}\"}}";
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
