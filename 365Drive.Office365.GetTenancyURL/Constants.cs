using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace _365Drive.Office365.CloudConnector
{

    public enum LicenseValidationState
    {
        MFARemindLater = 8,
        Ok = 0,
        Expired = 1,
        Exceeded = 2,
        LoginFailed = 3,
        CouldNotVerify = 4,
        TenancyNotExist = 5,
        ActivatedFirstTime = 6,
        ActivationFailed = 7
    }
    internal class StringConstants
    {
        internal const string UserrealMrequest = "https://login.microsoftonline.com/common/userrealm?user={0}&api-version=2.1&checkForMicrosoftAccount=true";
        internal const string AzureADSTSURL = "https://login.windows.net/common/oauth2/token?api-version=1.0";
        internal const string AzureADUserURL = "https://graph.windows.net/{0}/users?api-version=2013-11-08";
        internal const string AzureUpdateUser = "https://graph.windows.net/{0}/users/{1}?api-version=1.6";
        internal const string AzureADUserLicenseURL = "https://graph.windows.net/{0}/users/{1}/assignLicense?api-version=2013-11-08";
        internal const string AzureGetALLSKUURL = "https://graph.windows.net/{0}/subscribedSkus?api-version=2013-11-08";
        internal const string AuthenticateRequestUrl = "{0}_forms/default.aspx?ReturnUrl=%2f_layouts%2f15%2fAuthenticate.aspx%3fSource%3d%252F&Source=cookie";
        internal const string getAADCookieStep1 = "https://login.microsoftonline.com/common/oauth2/authorize?client_id=00000003-0000-0ff1-ce00-000000000000&response_mode=form_post&response_type=code id_token&scope=openid&nonce={1}&redirect_uri={0}_forms/default.aspx";
        internal const string AzureActivateUserStep1 = "https://login.microsoftonline.com/common/oauth2/authorize?response_type=code&client_id={1}&redirect_uri={2}&resource={3}&nux=1&login_hint2={0}";
        internal const string AzureActivateUserStep2 = "https://login.microsoftonline.com/common/login";
        internal const string loginKMSI = "https://login.microsoftonline.com/kmsi";
        internal const string KMSIPost = "LoginOptions=0&ctx={0}&flowToken={1}&canary={2}&i2=&i17=&i18=&i19=2519";
        internal const string AzureActivationUserLogin = "login={0}&passwd={1}&ctx={2}&flowToken={3}&canary={4}";
        internal const string newMSloginPost = "i13=0&login={0}&loginfmt={0}&type=11&LoginOptions=3&lrt=&lrtPartition=&hisRegion=&hisScaleUnit=&passwd={1}&ps=2&psRNGCDefaultType=&psRNGCEntropy=&psRNGCSLK=&psFidoAllowList=&canary={2}&ctx={3}&flowToken={4}&NewUser=1&FoundMSAs=&fspost=0&i21=0&CookieDisclosure=0&i2=1&i17=&i18=&i19=33065";
        internal const string AzureActivateUserStep3 = "https://login.microsoftonline.com/common/oauth2/token";
        internal const string AADPoll = "https://login.microsoftonline.com/common/onpremvalidation/Poll";
        internal const string dssoPoll = "https://login.microsoftonline.com/common/instrumentation/dssostatus";
        internal const string loginPost = "https://login.microsoftonline.com/common/login";
        internal const string AADConnectCookieloginPost = "https://login.microsoftonline.com/common/login";
        internal const string loginPostData = "login={0}&passwd={1}&ctx={2}&flowToken={3}&canary={4}&dssoToken={5}&n1=104502&n2=-1488344436000&n3=-1488344436000&n4=104502&n5=104502&n6=104502&n7=104502&n8=NaN&n9=104502&n10=104502&n11=104502&n12=104663&n13=104502&n14=104827&n15=60&n16=104897&n17=104898&n18=104904&n19=1109.2413112395688&n20=1&n21=0&n22=0&n23=1&n24=45.49044898147713&n25=2&n26=0&n27=0&n28=0&n29=23.89304877782797&n30=0.1526726045049145&n31=0&n32=1&n33=1&n34=48.88773517247546&n35=0&n36=0&n37=76.70878919505594&n38=1&n39=49.78367104484914&n40=0&n41=404.46821822517455&n42=352.2768531653051&n43=402.2914575615063&type=11&LoginOptions=2&NewUser=1&idsbho=1&PwdPad=&sso=&vv=&uiver=1&i12=1&i13=MSIE&i14=7.0&i15=604&i16=552&i20=";
        


        internal const string AdfsPost = "{0}?wauth=http%3a%2f%2fschemas.microsoft.com%2fws%2f2008%2f06%2fidentity%2fauthenticationmethod%2fpassword&username={1}&wa=wsignin1.0&wtrealm=urn%3afederation%3aMicrosoftOnline&wctx=estsredirect%3d2%26estsrequest%3d{2}&popupui=";
        internal const string AdfsPostBody = "UserName={0}&Password={1}&AuthMethod=FormsAuthentication";
        internal const string AdfsPhoneMFAPostBody = "AuthMethod=WindowsAzureMultiFactorAuthentication";
        internal const string AdfsPhoneMFAPostDoneBody = "AuthMethod=WindowsAzureMultiFactorAuthentication&Context={0}";
        internal const string ADFSrstPost = "https://login.microsoftonline.com/login.srf";
        internal const string ADFSrstPostBody = "wa=wsignin1.0&wresult={0}&wctx=estsredirect%3D2%26estsrequest%3D{1}";
        internal const string MSADFSrstPostBody = "wa=wsignin1.0&wresult={0}&wctx={1}";

        //get the RST
        internal const string MSADFSGetRST = "wa=wsignin1.0&rpsnv=4&ct=1500043152&rver=6.7.6640.0&wp=MCMBI&wreply=https%3a%2f%2fportal.office.com%2flanding.aspx%3ftarget%3d%252fdefault.aspx&lc=1033&id=501392&msafed=0&client-request-id={0}";

        //internal const string AdfsloginPostBody = "wa=wsignin1.0&wresult={0}";

        internal const string AADPollEnd = "https://login.microsoftonline.com/common/onpremvalidation/End";
        internal const string AADPollEndBody = "flowToken={0}&ctx={1}";

        internal const string dssoPollBody = "{\"resultCode\":  0,\"ssoDelay\":  117,\"log\":  null}";
        internal const string AADPollBody = "{{\"flowToken\":\"{0}\",\"ctx\":\"{1}\"}}";
        internal const string AzureActivateGetPartnerEntitlement = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Header><Header xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><Client xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">PC-winword.exe</Client><ClientLanguage xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">en-US</ClientLanguage><ClientVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">(16.0.6528)</ClientVersion><CorrelationId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">aafe760b-8c51-4884-9860-5abe9b3e0606</CorrelationId><OfficeMajorVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">16</OfficeMajorVersion><Protocol xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">3</Protocol></Header></s:Header><s:Body><GetEntitlementsForOlsIdentity xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><OlsIdentity><Ticket xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">{0}</Ticket></OlsIdentity><EntitlementStatusFilter>5</EntitlementStatusFilter><DoNotRedirectIfNotFound a:nil=\"true\" xmlns:a=\"http://www.w3.org/2001/XMLSchema-instance\"/></GetEntitlementsForOlsIdentity></s:Body></s:Envelope>";
        internal const string AzureActivateGetSessionToken = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Header><Header xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><Client xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">PC-winword.exe</Client><ClientLanguage xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">en-US</ClientLanguage><ClientVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">(16.0.6528)</ClientVersion><CorrelationId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">aafe760b-8c51-4884-9860-5abe9b3e0606</CorrelationId><OfficeMajorVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">16</OfficeMajorVersion><Protocol xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">3</Protocol></Header></s:Header><s:Body><GetSessionToken xmlns=\"http://schemas.microsoft.com/office/licensingservice/API/2012/01/ClientApi\"><OlsIdentity><Ticket xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService\">{0}</Ticket></OlsIdentity><EntitlementInfo><Partner xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService.Common\">2</Partner><PartnerEntitlementId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Office.LicensingService.Common\">{1}</PartnerEntitlementId></EntitlementInfo><MachineId>+LcT4g==</MachineId><UserId>{2}</UserId><SecurityId>S-1-5-21-2612617249-3288171387-3952625246-1001</SecurityId><CurrentTime>{3}</CurrentTime><HosterId/></GetSessionToken></s:Body></s:Envelope>";
        internal const string FailedLoginUrl = "https://login.microsoftonline.com/kmsi";
        internal const string AADFailedLoginUrl = "https://login.microsoftonline.com/common/onpremvalidation/End";
        internal const string ADFSailedLoginUrl = "https://login.microsoftonline.com/login.srf";
        //internal const string AzureActivationUserToken = "grant_type=authorization_code&code={0}&redirect_uri=urn%3aietf%3awg%3aoauth%3a2.0%3aoob&client_id=d3590ed6-52b3-4102-aeff-aad2292ab01c";
        internal const string postCodeUrl = "{0}_forms/default.aspx";
        internal const string postCodeBody = "code={0}&id_token={1}&session_state={2}";
        internal const string AzureActivationUserToken = "client_id={1}&redirect_uri={3}&client_secret={2}&code={0}&grant_type=authorization_code&resource={4}";
        internal const string AzureActivateUserStep4 = "https://api.office.com/discovery/v2.0/me/services";
        internal const string rootUrlFinder = "rootsite@";
        internal const string rootUrltobeRemoved = ".sharepoint.com";
        internal const string rootUrltobeReplacedWith = ".onmicrosoft.com";
        internal const string sharepointClientID = "00000003-0000-0ff1-ce00-000000000000";
        //internal const string clientID = "df166692-81e3-404b-97ae-d09038e90d8c";
        //internal const string clientSecret = "nIEIuEyhATgLdFqhcHCMTJKky3QiXDQ7eYcQjkjhmPc=";

        internal const string clientID = "925aa176-2ab1-4806-aeff-dc98260f23a3";
        internal const string clientSecret = "UVVTEHBosIRqiIsOswtzKK/L+QQx9H0gfYIdmrQUpwI=";
        //internal const string appRedirectURL = "http://identityexperts.co.uk";
        internal const string appRedirectURL = "https://store.identityexperts.co.uk/365mapperar";
        internal const string appResourceUri = "https://api.office.com/discovery/";

        //internal const string AzureActivateUserStep5 = "https://ols.officeapps.live.com/olsc/OlsClient.svc/OlsClient";
        internal const string GraphPrincipalId = "https://graph.windows.net";
        internal const string DirectoryServiceURL = "https://graph.windows.net/";
        internal const string AzureActivateUserStep6 = "https://odc.officeapps.live.com/odc/servicemanager/userconnected?lcid=1033&syslcid=1033&uilcid=1033&app=0&ver=16&schema=3";

        internal const string GraphServiceVersion = "2013-04-05";
        internal const string CreateUserBody = "{{\"accountEnabled\": \"true\",\"displayName\": \"{0}\",\"mailNickname\": \"{1}\",\"usageLocation\": \"GB\",\"passwordProfile\": {{ \"password\" : \"{2}\", \"forceChangePasswordNextLogin\": \"false\" }},\"userPrincipalName\": \"{3}\",\"immutableId\": \"{4}\"}}";
        internal const string AssignLicenseBody = "{{ \"addLicenses\": [ {{ \"disabledPlans\": [], \"skuId\": \"{0}\" }} ], \"removeLicenses\": []}}";

        ////////////////////////////Cloud Identity Cookie////////////////////////////
        //https://login.windows.net/common/oauth2/authorize?client%5Fid=00000003%2D0000%2D0ff1%2Dce00%2D000000000000&response%5Fmode=form%5Fpost&response%5Ftype=code%20id%5Ftoken&scope=openid&nonce=09C0632AF08417E5E70CEA1F33C96FCB24C7EB0874E87004%2DD56B8D2C8A3E860CCB667ADB9D1DD555B55C57E21559C0CD225C020C29445980&redirect%5Furi={0}&state=0&client%2Drequest%2Did=bb27f19d%2D3022%2D3000%2Db8cd%2Db535a27a2569

        internal const string getCloudCookieStep0 = "https://login.windows.net/common/oauth2/authorize?client%5Fid=00000003%2D0000%2D0ff1%2Dce00%2D000000000000&response%5Fmode=form%5Fpost&response%5Ftype=code%20id%5Ftoken&scope=openid&nonce={1}&redirect%5Furi={0}&state=0&client%2Drequest%2Did={2}";
        internal const string getCloudCookieStep1 = "https://login.microsoftonline.com/common/oauth2/authorize?client%5Fid=00000003%2D0000%2D0ff1%2Dce00%2D000000000000&response%5Fmode=form%5Fpost&response%5Ftype=code%20id%5Ftoken&scope=openid&nonce={1}&redirect%5Furi={0}&state=0&client%2Drequest%2Did={2}";
        internal const string CloudloginPost = "https://login.microsoftonline.com/common/login";
        internal const string CloudloginPostData = "login={0}&passwd={1}&ctx={2}&flowToken={3}&canary={4}&n1=104502&n2=-1488344436000&n3=-1488344436000&n4=104502&n5=104502&n6=104502&n7=104502&n8=NaN&n9=104502&n10=104502&n11=104502&n12=104663&n13=104502&n14=104827&n15=60&n16=104897&n17=104898&n18=104904&n19=1109.2413112395688&n20=1&n21=0&n22=0&n23=1&n24=45.49044898147713&n25=2&n26=0&n27=0&n28=0&n29=23.89304877782797&n30=0.1526726045049145&n31=0&n32=1&n33=1&n34=48.88773517247546&n35=0&n36=0&n37=76.70878919505594&n38=1&n39=49.78367104484914&n40=0&n41=404.46821822517455&n42=352.2768531653051&n43=402.2914575615063&type=11&LoginOptions=2&NewUser=1&idsbho=1&PwdPad=&sso=&vv=&uiver=1&i12=1&i13=MSIE&i14=7.0&i15=604&i16=552&i20=";


        internal const string MSADGetCodeandTokenCall = "https://login.microsoftonline.com/common/oauth2/authorize?client_id=4345a7b9-9a63-4910-a426-35363201d503&response_mode=form_post&response_type=code+id_token&scope=openid+profile&state=OpenIdConnect.AuthenticationProperties%3dbWgWcJqkzkLvLNQ2M2KamFf9FBeawxpJNxtNRTapOPoBkN3ueBOcjWPllRnEU2dMC2qI_jML9tpO8WoP9F6NG0JxivalI_vw3HxaZOhX66GrqUdfCzfVNpl3E72PVPY2yg1CwZUJaachIJPpyThMh2hO5I8qi_Hgo_CWOoZOKikzgl4cMWbexLh8A4tiliRWJaJBHEmjlN7tu5s4YqYVGGQJwGDyH6YOMTv1mS3Pf7kkU70Q4z58ROsSlCwl3vsircR2zRPEtEuWLYUWCX5U-gQrCi6up0h_ARvdMVXvkWM&nonce=636356399976707733.NDIzM2E4MzMtZGQ3My00NGQyLTkxODAtMDc2NTBhZGZkZmExNWMzMDkyMjItZTQ4My00YWVkLTg5ZWItMzVlODg3YTYxZTM0&redirect_uri=https%3a%2f%2fwww.office.com%2flanding&ui_locales=en-US&mkt=en-US&client-request-id={0}&msafed=0";
        internal const string MSADGetMSAuthUrl = "https://login.microsoftonline.com/common/oauth2/authorize?client_id=00000003-0000-0ff1-ce00-000000000000&response_mode=form_post&response_type=code%20id_token&resource=00000003-0000-0ff1-ce00-000000000000&scope=openid&nonce={0}&redirect_uri=https:%2F%2Fmicrosoft.sharepoint.com%2F_forms%2Fdefault.aspx&domain_hint=microsoft.com&state=0&client-request-id={1}";
        ////////////////////////////ADFS AUTH////////////////////////////
        internal const string ADFSRealM = "https://login.microsoftonline.com/common/userrealm?user={0}&api-version=2.1&stsRequest={1}&checkForMicrosoftAccount=true";

        ////////////////////////////ADFS MS AUTH////////////////////////////
        internal const string ADFSMSRealM = "https://login.microsoftonline.com/common/userrealm?user={0}&api-version=2.1&stsRequest={1}&checkForMicrosoftAccount=true";

        ////////////////////////////MFA////////////////////////////
        //SMS
        internal const string SASBeginAuthPostBody = "{{\"Method\":\"BeginAuth\",\"flowToken\":\"{0}\",\"ctx\":\"{1}\",\"AuthMethodId\":\"{2}\"}}";
        internal const string SASEndAuthPostBody = "{{\"Method\":\"EndAuth\",\"FlowToken\":\"{0}\",\"SessionId\":\"{1}\",\"Ctx\":\"{2}\",\"AuthMethodId\":\"{6}\",\"AdditionalAuthData\":\"{3}\",\"LastPollStart\":\"{4}\",\"LastPollEnd\":\"{5}\"}}";
        internal const string SASCallEndAuthPostBody = "{{\"Method\":\"EndAuth\",\"FlowToken\":\"{0}\",\"SessionId\":\"{1}\",\"Ctx\":\"{2}\",\"PollCount\":\"1\",\"LastPollStart\":\"{3}\",\"LastPollEnd\":\"{4}\",\"AuthMethodId\":\"{5}\"}}";
        internal const string SASProcessAuthPostBody = "request={0}&flowToken={1}&canary={2}&mfaAuthMethod={6}&rememberMFA={5}&mfaLastPollStart={3}&mfaLastPollEnd={4}";
        internal const string SharePointFormPost = "code={0}&id_token={1}&state={2}&session_state={3}";
    }
}
