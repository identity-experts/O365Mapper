
using Microsoft.Win32;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace _365Drive.Office365
{
    public static class Utility
    {

         

        /// <summary>
        /// Make sure internet is up and working
        /// </summary>
        public static bool ensureInternet()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    ////put here your logic
                    //StartAfterWaiting();
                    return true;
                }
                else
                {
                    //put here your logic
                    return false;
                }
            }
            catch (NetworkInformationException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, ex.GetType().ToString(),
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, ex.GetType().ToString(),
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            finally
            {
            }
            return true;
        }

        /// <summary>
        /// ensure webClient service is running or not
        /// </summary>
        /// <returns></returns>
        public static bool webClientServiceRunning()
        {
            bool webClientRunning = false;
            try
            {
                using (ServiceController sc = new ServiceController("WebClient"))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        webClientRunning = false;
                    }
                    else
                    {
                        webClientRunning = true;
                    }
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }
            return webClientRunning;
        }

        /// <summary>
        /// Make sure internet is up and working
        /// </summary>
        public static bool ensurePowerMode(PowerModeChangedEventArgs e)
        {
            try
            {
                if (e.Mode == PowerModes.Resume)
                {
                    Thread.Sleep(10000);

                }

                else
                {
                    //put here your logic
                    return true;
                }
            }
            catch (NetworkInformationException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, ex.GetType().ToString(),
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, ex.GetType().ToString(),
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            finally
            {
            }
            return true;
        }

        /// <summary>
        /// Chekc whther .NET 4.5 is installed or not
        /// </summary>
        /// <returns></returns>
        public static bool checkFx45()
        {
            bool frameworkInstalled = true;

            try
            {
                using (
                    RegistryKey fx45 =
                        RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                            .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
                {
                    int releaseKey = Convert.ToInt32(fx45.GetValue("Release"));
                    //MessageBox.Show(releaseKey.ToString() + "  378389");
                    if (releaseKey >= 378389)
                        frameworkInstalled = true;
                    else
                        frameworkInstalled = false;
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
            }

            return frameworkInstalled; 
        }

        /// <summary>
        /// Make sure everything is OK
        /// </summary>
        /// <returns>boolean indicating all OK or not</returns>
        public static bool ready()
        {
            bool ready = false;

            //Ensuring internet
            if (ensureInternet())
            {
                ready = true;
            }
            else
            {
                ready = false;
                LogManager.Verbose("We are NOT ready");
            }

            return ready;
        }
    }
}
