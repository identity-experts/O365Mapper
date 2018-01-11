using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.CloudConnector
{
    [DataContract]
    public class AADJWTToken
    {
        [DataMember(Name = "token_type")]
        public string TokenType { get; set; }

        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "not_before")]
        public ulong NotBefore { get; set; }

        [DataMember(Name = "expires_on")]
        public ulong ExpiresOn { get; set; }

        [DataMember(Name = "expires_in")]
        public ulong ExpiresIn { get; set; }

        /// <summary>
        /// Returns true if the token is expired and false otherwise.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return WillExpireIn(0);
            }
        }

        /// <summary>
        /// Returns true if the token will expire in the number of minutes passed in to the method.
        /// </summary>
        /// <param name="minutes">minutes in which the token is checked for expiration.</param>
        /// <returns></returns>
        public bool WillExpireIn(int minutes)
        {
            return GenerateTimeStamp(minutes) > ExpiresOn;
        }

        /// <summary>
        /// Generates the timestap value for the number of minutes passed in.
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        private static ulong GenerateTimeStamp(int minutes)
        {
            // Default implementation of epoch time
            TimeSpan ts = DateTime.UtcNow.AddMinutes(minutes) - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToUInt64(ts.TotalSeconds);
        }
    }


    public class MySiteMetadata
    {
        public string capability { get; set; }
        public string entityKey { get; set; }
        public string providerId { get; set; }
        public string providerName { get; set; }
        public int serviceAccountType { get; set; }
        public string serviceApiVersion { get; set; }
        public string serviceEndpointUri { get; set; }
        public string serviceId { get; set; }
        public string serviceName { get; set; }
        public string serviceResourceId { get; set; }
    }


    public class AuthResponse
    {
        public string Success { get; set; }
        public string ResultValue { get; set; }
        public object Message { get; set; }
        public string AuthMethodId { get; set; }
        public int ErrCode { get; set; }
        public string Retry { get; set; }
        public string FlowToken { get; set; }
        public string Ctx { get; set; }
        public string SessionId { get; set; }
        public string CorrelationId { get; set; }
    }

    public class ArrUserProof
    {
        public string authMethodId { get; set; }
        public string data { get; set; }
        public string display { get; set; }
        public bool isDefault { get; set; }
    }

    public class MFAConfig
    {


        public List<ArrUserProof> arrUserProofs { get; set; }
        public string urlBeginAuth { get; set; }
        public string urlEndAuth { get; set; }
        public string urlPost { get; set; }
        public string sFT { get; set; }
        public string sFTName { get; set; }
        public string sCtx { get; set; }
        public int hpgact { get; set; }
        public int hpgid { get; set; }
        public string pgid { get; set; }
        public string canary { get; set; }
        public string defaultAuthMethod
        {
            get { return arrUserProofs.Any(a => a.isDefault) ? arrUserProofs.Where(a => a.isDefault).FirstOrDefault().authMethodId : string.Empty; }
        }

    }

    public class StrongAuthConstantResponse
    {
        public string SASControllerBeginAuthUrl { get; set; }
        public string SASControllerProcessAuthUrl { get; set; }
        public string SASControllerEndAuthUrl { get; set; }
        public string FlowToken { get; set; }
        public string Ctx { get; set; }
    }

    public class StrongAuthContextResponse
    {
        public string Success { get; set; }
        public DefaultMethod DefaultMethod { get; set; }
        //public List<Method> Methods { get; set; }
    }

    public class DefaultMethod
    {
        public string AuthMethodId { get; set; }
        public string AuthMethodDeviceId { get; set; }
        public string MethodDisplayString { get; set; }
    }

    public class MysiteResponse
    {
        public List<MySiteMetadata> value { get; set; }
    }

    public class apiCanaryResponse
    {
        public string apiCanary { get; set; }
    }

    public class pollResponse
    {
        public string apiCanary { get; set; }
        public string flowToken { get; set; }
        public string ctx { get; set; }
    }

    public class RealM
    {
        public int MicrosoftAccount { get; set; }
        public string NameSpaceType { get; set; }
        public string Login { get; set; }
        public string DomainName { get; set; }
        public string FederationBrandName { get; set; }
        public string cloud_instance_name { get; set; }
        public bool? is_dsso_enabled { get; set; }
        public string AuthURL { get; set; }
        public string federation_protocol { get; set; }

    }

    public class OnPremPasswordValidationConfig
    {
        public bool isUserRealmPrecheckEnabled { get; set; }
    }

    public class OAppCobranding
    {
    }

    public class Locale
    {
        public string mkt { get; set; }
        public int lcid { get; set; }
    }

    public class Desktopsso
    {
        public string authenticatingmessage { get; set; }
    }

    public class Strings
    {
        public Desktopsso desktopsso { get; set; }
    }

    public class ClientMetricsModes
    {
        public int None { get; set; }
        public int SubmitOnPost { get; set; }
        public int SubmitOnRedirect { get; set; }
        public int InstrumentPlt { get; set; }
    }

    public class Enums
    {
        public ClientMetricsModes ClientMetricsModes { get; set; }
    }

    public class Instr
    {
        public string pageload { get; set; }
        public string dssostatus { get; set; }
    }

    public class Urls
    {
        public Instr instr { get; set; }
    }

    public class B
    {
        public string name { get; set; }
        public int major { get; set; }
        public int minor { get; set; }
    }

    public class Os
    {
        public string name { get; set; }
        public string version { get; set; }
    }

    public class Browser
    {
        public int ltr { get; set; }
        public int _Other { get; set; }
        public int Full { get; set; }
        public int RE_Other { get; set; }
        public B b { get; set; }
        public Os os { get; set; }
        public int V { get; set; }
    }

    public class Watson
    {
        public string url { get; set; }
        public string bundle { get; set; }
        public string sbundle { get; set; }
        public string fbundle { get; set; }
        public int resetErrorPeriod { get; set; }
        public int maxCorsErrors { get; set; }
        public int maxInjectErrors { get; set; }
        public int maxErrors { get; set; }
        public int maxTotalErrors { get; set; }
        public List<string> expSrcs { get; set; }
        public bool envErrorRedirect { get; set; }
        public string envErrorUrl { get; set; }
    }

    public class Ver
    {
        public List<int> v { get; set; }
    }

    public class ServerDetails
    {
        public string slc { get; set; }
        public string dc { get; set; }
        public string ri { get; set; }
        public Ver ver { get; set; }
        public DateTime rt { get; set; }
        public int et { get; set; }
    }

    public class Bsso
    {
        public string type { get; set; }
        public string reason { get; set; }
    }

    public class dSSO
    {
        public string iwaEndpointUrlFormat { get; set; }
        public int iwaRequestTimeoutInMs { get; set; }
        public bool startDesktopSsoOnPageLoad { get; set; }
        public int progressAnimationTimeout { get; set; }
        public bool isEdgeAllowed { get; set; }
        public bool isSafariAllowed { get; set; }
        public string redirectUri { get; set; }
    }

    public class dSSOConfig
    {
        public dSSO desktopSsoConfig;
    }

    public class LoginConfig
    {
        public string sCtx { get; set; }
        public string sFT { get; set; }
        public string canary { get; set; }
        //public bool fShowPersistentCookiesWarning { get; set; }
        //public string urlMsaLogout { get; set; }
        //public string urlUxPreviewOptOut { get; set; }
        //public bool showCantAccessAccountLink { get; set; }
        //public bool fShowOptOutBanner { get; set; }
        //public string urlSessionState { get; set; }
        //public string urlResetPassword { get; set; }
        //public string urlMsaResetPassword { get; set; }
        //public string urlLogin { get; set; }
        //public string urlSignUp { get; set; }
        //public string urlGetCredentialType { get; set; }
        //public string urlGetOneTimeCode { get; set; }
        //public string urlLogout { get; set; }
        //public string urlForget { get; set; }
        //public string urlDisambigRename { get; set; }
        //public string urlGoToAADError { get; set; }
        //public string urlDssoStatus { get; set; }
        //public bool fCBShowSignUp { get; set; }
        //public bool fKMSIEnabled { get; set; }
        //public int iLoginMode { get; set; }
        //public bool fAllowPhoneSignIn { get; set; }
        //public string sConsumerDomains { get; set; }
        //public int iMaxPollErrors { get; set; }
        //public int iPollingTimeout { get; set; }
        //public bool srsSuccess { get; set; }
        //public bool fShowSwitchUser { get; set; }
        //public List<string> arrValErrs { get; set; }
        //public string sErrorCode { get; set; }
        //public string sErrTxt { get; set; }
        //public string sResetPasswordPrefillParam { get; set; }
        //public OnPremPasswordValidationConfig onPremPasswordValidationConfig { get; set; }
        //public bool fSwitchDisambig { get; set; }
        //public int iAllowedIdentities { get; set; }
        //public int iRemoteNgcPollingType { get; set; }
        //public bool isGlobalTenant { get; set; }
        //public int iMaxStackForKnockoutAsyncComponents { get; set; }
        //public string strCopyrightTxt { get; set; }
        //public bool fShowButtons { get; set; }
        //public bool fShowDogfoodBanner { get; set; }
        //public string urlCdn { get; set; }
        //public string urlFooterTOU { get; set; }
        //public string urlFooterPrivacy { get; set; }
        //public string urlPost { get; set; }
        //public string urlRefresh { get; set; }
        //public string urlCancel { get; set; }
        //public int iPawnIcon { get; set; }
        //public int iPollingInterval { get; set; }
        //public string sPOST_Username { get; set; }
        //public string sFT { get; set; }
        //public string sFTName { get; set; }
        //public string sSessionIdentifierName { get; set; }
        //public string sCtx { get; set; }
        //public int iProductIcon { get; set; }
        //public object dynamicTenantBranding { get; set; }
        //public object staticTenantBranding { get; set; }
        //public OAppCobranding oAppCobranding { get; set; }
        //public int iBackgroundImage { get; set; }
        //public List<object> arrSessions { get; set; }
        //public bool fUseConstantPolling { get; set; }
        //public int scid { get; set; }
        //public int hpgact { get; set; }
        //public int hpgid { get; set; }
        //public string pgid { get; set; }
        //public string apiCanary { get; set; }
        //public string canary { get; set; }
        //public string correlationId { get; set; }
        //public Locale locale { get; set; }
        //public int slMaxRetry { get; set; }
        //public bool slReportFailure { get; set; }
        //public Strings strings { get; set; }
        //public Enums enums { get; set; }
        //public Urls urls { get; set; }
        //public Browser browser { get; set; }
        //public Watson watson { get; set; }
        //public ServerDetails serverDetails { get; set; }
        //public string country { get; set; }
        //public Bsso bsso { get; set; }
    }

    public enum FedType
    { Cloud, AAD, AADP, ADFS, OCTA, IAC, PING, NA }
}
