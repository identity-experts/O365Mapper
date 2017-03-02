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

    public enum FedType
    { Cloud, AAD, AADP, ADFS }
}
