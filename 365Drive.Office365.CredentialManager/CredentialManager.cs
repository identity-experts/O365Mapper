using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365
{


    public enum CredentialState
    {
        Notpresent = 0,
        Present = 1
    }

    public class CredentialManager
    {
        public const string _credentialStoreName = "365drive.credential";

        /// <summary>
        /// Make sure if the credential exist, set them
        /// </summary>
        /// <returns></returns>
        public static CredentialState ensureCredentials()
        {
            if (GetCredential() == null)
            {
                return CredentialState.Notpresent;
            }
            else
            {
                return CredentialState.Present;
            }
        }


        public static Cred GetCredential()
        {
            var cm = new Credential { Target = _credentialStoreName };
            if (!cm.Load())
            {
                return null;
            }

            //Cred is just a class with two string properties for user and pass
            return new Cred(cm.Username, cm.Password);
        }

        public static bool SetCredentials(
              string username, string password)
        {

            return new Credential
            {
                Target = _credentialStoreName,
                Username = username,
                Password = password,
                PersistanceType = PersistanceType.LocalComputer
            }.Save();
        }

        public static bool RemoveCredentials()
        {
            return new Credential { Target = _credentialStoreName }.Delete();
        }
    }


    public class Cred
    {

        public string UserName { get; set; }
        public string Password { get; set; }

        public Cred(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;

        }
    }
}
