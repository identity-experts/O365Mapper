using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            CredentialState state = CredentialState.Notpresent;
            try
            {
                if (GetCredential() == null)
                {
                    state = CredentialState.Notpresent;
                }
                else
                {
                    state = CredentialState.Present;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return state;
        }


        public static Cred GetCredential()
        {
            var cm = new Credential { Target = _credentialStoreName };
            try
            {
                cm = new Credential { Target = _credentialStoreName };
                if (!cm.Load())
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            //Cred is just a class with two string properties for user and pass
            return new Cred(cm.Username, cm.Password);
        }

        public static bool SetCredentials(
              string username, string password)
        {
            bool blSetCredentialstate = false;

            try
            {
                blSetCredentialstate = new Credential
                {
                    Target = _credentialStoreName,
                    Username = username,
                    Password = password,
                    PersistanceType = PersistanceType.LocalComputer
                }.Save();
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return blSetCredentialstate;
        }

        public static bool RemoveCredentials()
        {
            bool blCredDeleted = false;

            try
            {
                blCredDeleted = new Credential { Target = _credentialStoreName }.Delete();
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return blCredDeleted;
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
