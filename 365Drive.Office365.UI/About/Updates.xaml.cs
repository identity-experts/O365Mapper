using _365Drive.Office365.CloudConnector;
using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using _365Drive.Office365.UpdateManager;
using System.Reflection;

namespace _365Drive.Office365.UI.About
{
    /// <summary>
    /// Interaction logic for Updates.xaml
    /// </summary>
    public partial class Updates : ModernDialog
    {
        public Updates()
        {
            InitializeComponent();

            this.Buttons = new Button[] { this.OkButton, this.CancelButton };

            this.OkButton.Content = Globalization.Globalization.UpdateButton;
            this.OkButton.Click += UpdateButton_Click;

            this.CancelButton.Content = Globalization.Globalization.Cancel;
            this.CancelButton.Click += CancelButton_Click;

            if (LicenseManager.isitPartnerManaged)
            {
                //change logo
                logo.Source = LicenseManager.partnerLogoBM;
            }

        }

        /// <summary>
        /// No action, close current form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (dontAskCheckbox.IsChecked == true)
            {
                RegistryManager.Set(RegistryKeys.DontAskForUpdates, "1");
            }
            this.Close();
        }

        /// <summary>
        /// No action, close current form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dontAskCheckbox.IsChecked == true)
                {
                    RegistryManager.Set(RegistryKeys.DontAskForUpdates, "1");
                }

                UpdateProgress.Visibility = Visibility.Visible;

                //check for updates
                VersionResponse version = Versions.LatestVersion();

                //if its 64bit process, lets download 64bit. Otherwise 32bit
                if (Environment.Is64BitProcess)
                {
                    LogManager.Info("64bit");
                    BeginDownload(version.data.x64, version.data.version, "365mapper.msi");
                }
                else
                {
                    LogManager.Info("32bit");
                    BeginDownload(version.data.x86, version.data.version, "365mapper.msi");
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                this.Close();
            }
        }



        private void BeginDownload(string remoteURL, string version, string executeTarget)
        {
            try
            {
                string filePath = System.IO.Path.GetTempPath() + @"365mapper_" + version;

                Uri remoteURI = new Uri(remoteURL);
                System.Net.WebClient downloader = new System.Net.WebClient();

                downloader.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(downloader_DownloadFileCompleted);

                downloader.DownloadFileAsync(remoteURI, filePath + ".zip",
                    new string[] { version, filePath, executeTarget });
            }
            catch (Exception ex)
            {
                UpdateProgress.Visibility = Visibility.Hidden;
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                this.Close();
            }
        }


        void downloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                string[] us = (string[])e.UserState;
                string currentVersion = us[0];
                string downloadToPath = us[1];
                string executeTarget = us[2];

                string zipName = downloadToPath + ".zip"; // Download folder + zip file
                string exePath = downloadToPath + "\\" + executeTarget; // Download folder\version\ + executable

                if (new System.IO.FileInfo(zipName).Exists)
                {
                    using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile(zipName))
                    {
                        zip.ExtractAll(downloadToPath,
                            Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
                    }
                    if (new System.IO.FileInfo(exePath).Exists)
                    {
                        //ApplicationUpdate.Versions.CreateLocalVersionFile(downloadToPath, "version.txt", currentVersion);
                        System.Diagnostics.Process proc = System.Diagnostics.Process.Start(exePath);
                        this.Close();
                    }
                    else
                    {
                        this.Close();
                        //MessageBox.Show("Problem with download. File does not exist.");
                    }
                }
                else
                {
                    this.Close();
                    //MessageBox.Show("Problem with download. File does not exist.");
                }
            }
            catch (Exception ex)
            {
                string method = string.Format("{0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.FullName, MethodBase.GetCurrentMethod().Name);
                LogManager.Exception(method, ex);
                this.Close();
            }
            UpdateProgress.Visibility = Visibility.Hidden;

        }
    }
}
