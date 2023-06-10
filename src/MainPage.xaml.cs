using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Utils.ExceptionHelper;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FloatingCastle_Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        DownloadOperation downloadOperation;
        CancellationTokenSource cancellationToken;
        BackgroundDownloader backgroundDownloader = new BackgroundDownloader();
        string GAME_DATA_VERSION;
        string INSTALLED_DATA_VERSION;
        string CRC_VALUE;
        string GAME_DATA_ADDRESS;
        string UPDATE_TEXT;
        string CLIENT_VERSION;
        string CLIENT_UPDATE_ADDRESS;
        string CURRENT_PKG_VERSION;
        string CLIENT_CRC;

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public MainPage()
        {
            this.InitializeComponent();
            RPGTest.Classes.WindowManager.EnterFullScreen(true);
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            App.globalWebView = GamePlayer;
            INSTALLED_DATA_VERSION = localSettings.Values["GameDataVersion"].ToString();
            Package pkg = Package.Current;
            PackageVersion pkgVersion = pkg.Id.Version;
            CURRENT_PKG_VERSION = string.Format("{0}.{1}.{2}.{3}", pkgVersion.Major, pkgVersion.Minor, pkgVersion.Build, pkgVersion.Revision); ;


            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                GameInfo.Text = $"Welcome to The Floating Castle Pre-Alpha by Empyreal96\n\n" +
               "[Controls]\n" +
                "Tap = Move/Activate/Select\n" +
                "Two Finger Tap = Open Menu/Go Back in Menus\n" +
                "Back Key = Open Client Menu\n\n\n" +
                "[Credits]\n" +
                "Empyreal96 - Game, Client\n" +
                "Bashar Astifan - RPG Save Manager Plugin\n\n" +
                $"Client Version: {CURRENT_PKG_VERSION} | Game Version: {INSTALLED_DATA_VERSION}";
            }
            else
            {
                GameInfo.Text = $"Welcome to The Floating Castle Pre-Alpha by Empyreal96\n\n" +
               "[Controls]\n" +
                "Left Click = Move/Activate/Select\n" +
                "Right Click/Esc = Open Menu/Go Back in Menus\n" +
                "Titlebar Back Button = Open Client Menu\n" +
                "Arrow Keys = Move\n" +
                "Hold Shift = Dash\n\n\n" +
                "[Credits]\n" +
                "Empyreal96 - Game, Client\n" +
                "Bashar Astifan - RPG Save Manager Plugin\n\n" +
                $"Client Version: {CURRENT_PKG_VERSION} | Game Version: {INSTALLED_DATA_VERSION}";
            }


            FileProgressInfo.Text = $"Checking for Game Files";

            CheckForFiles();
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            if (ClientUpdateGrid.Visibility != Visibility.Visible)
            {
                ClientMenu.Visibility = Visibility.Visible;
            }

        }

        public async void CheckForFiles()
        {
            FileProgressbar.IsIndeterminate = true;
            IStorageItem localGameFolder = await localFolder.TryGetItemAsync("FloatingCastleData");
            if (localGameFolder != null)
            {
                var localGameCache = await localFolder.GetFolderAsync("FloatingCastleData");
                var gameExecutable = await localGameCache.TryGetItemAsync("index.html");
                if (gameExecutable != null)
                {
                    bool isNetworkConnected = NetworkInterface.GetIsNetworkAvailable();
                    if (isNetworkConnected == true)
                    {
                        FileProgressInfo.Text = $"Game Data Version {INSTALLED_DATA_VERSION} found, checking for updates";
                        CheckForUpdate();
                    }
                    else
                    {
                        FileProgressInfo.Text = "Connect to the Internet to check for updates";
                        if (INSTALLED_DATA_VERSION != "null")
                        {
                            FileProgressInfo.Text = "Starting game";
                            StartGame();
                        }
                    }

                }
                else
                {
                    FileProgressInfo.Text = "Game Data Files ready to download";
                }
            }
            else
            {
                CheckForUpdate();
            }
        }


        bool isPackageValidationCheck;
        public async void CheckForUpdate()
        {
            isPackageValidationCheck = true;
            // https://github.com/Empyreal96/FloatingCastleGame/raw/main/package.csv
            var pkgDataCSV = await ApplicationData.Current.LocalFolder.CreateFileAsync("package.csv", CreationCollisionOption.ReplaceExisting);


            var downloadOperation = backgroundDownloader.CreateDownload(new Uri(@"https://github.com/Empyreal96/FloatingCastleGame/raw/main/package.csv"), pkgDataCSV);
            Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progressChanged);
            cancellationToken = new CancellationTokenSource();
            await downloadOperation.StartAsync().AsTask(cancellationToken.Token, progress);


        }

        public void StartGame()
        {
            ClientUpdateGrid.Visibility = Visibility.Collapsed;
            FileProgressBack.Visibility = Visibility.Collapsed;
            GameGrid.Visibility = Visibility.Visible;
            GamePlayer.Visibility = Visibility.Visible;
            GamePlayer.Navigate(new Uri(@"ms-appdata:///local/FloatingCastleData/index.html"));
        }

       


        private async void CheckPackageMetadata()
        {
            var pkgDataCSV = await ApplicationData.Current.LocalFolder.GetFileAsync("package.csv");
            var metadataString = await FileIO.ReadTextAsync(pkgDataCSV);
            var metadataArray = metadataString.Split(',');
            GAME_DATA_VERSION = metadataArray[0];
            CRC_VALUE = metadataArray[1];
            GAME_DATA_ADDRESS = metadataArray[2];
            UPDATE_TEXT = metadataArray[3];
            CLIENT_VERSION = metadataArray[4];
            CLIENT_CRC = metadataArray[5];
            CLIENT_UPDATE_ADDRESS = metadataArray[6];

            if (CURRENT_PKG_VERSION != CLIENT_VERSION)
            {
                FileProgressInfo.Text = $"[Client Update Required] Version: {CLIENT_VERSION}  CRC: {CLIENT_CRC}  Address: {CLIENT_UPDATE_ADDRESS}";
                PatchButton.IsEnabled = true;
                FileProgressbar.IsIndeterminate = false;
                FileProgressbar.Value = 0;
                StartClientUpdate();
            }

            if (INSTALLED_DATA_VERSION != GAME_DATA_VERSION)
            {
                FileProgressInfo.Text = $"[Update Required] Version: {GAME_DATA_VERSION}  CRC: {CRC_VALUE}  Address: {GAME_DATA_ADDRESS}";
                GameInfo.Text += $"\n\nUpdate: {UPDATE_TEXT}";
                PatchButton.IsEnabled = true;
                FileProgressbar.IsIndeterminate = false;
                FileProgressbar.Value = 0;
            }
            else
            {
                FileProgressInfo.Text = "No Updates Found, Starting Game";
                StartGame();
            }
        }
        StorageFile ClientFile;
        StorageFile gameDataPackage;
        private async void PatchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gameData = await localFolder.TryGetItemAsync("FloatingCastleData");
                if (gameData != null)
                {
                    await gameData.DeleteAsync();
                }

                isPackageValidationCheck = false;
                gameDataPackage = await ApplicationData.Current.LocalFolder.CreateFileAsync("FloatingCastleData.zip", CreationCollisionOption.ReplaceExisting);

                var downloadOperation = backgroundDownloader.CreateDownload(new Uri($"{GAME_DATA_ADDRESS}"), gameDataPackage);
                Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progressChanged);
                cancellationToken = new CancellationTokenSource();
                PatchButton.IsEnabled = false;
                await downloadOperation.StartAsync().AsTask(cancellationToken.Token, progress);
            } catch (Exception ex)
            {
                Exceptions.ThrownExceptionError(ex);
            }

        }
        bool isClientUpdate = false;
        private async void StartClientUpdate()
        {
            isPackageValidationCheck = false;
            isClientUpdate = true;
            ClientFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("ClientUpdate.zip", CreationCollisionOption.ReplaceExisting);
            var downloadOperation = backgroundDownloader.CreateDownload(new Uri($"{CLIENT_UPDATE_ADDRESS}"), ClientFile);
            Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progressChanged);
            cancellationToken = new CancellationTokenSource();
            PatchButton.IsEnabled = false;
            await downloadOperation.StartAsync().AsTask(cancellationToken.Token, progress);
        }


        public string NEW_CRC;
        public async Task<bool> CheckDataCRC(StorageFile GameData)
        {
            Stream dataStream = await GameData.OpenStreamForReadAsync();

            CRC32 crc32 = new CRC32();
            string crc = await crc32.ComputeHash(dataStream);
            NEW_CRC = crc;
            if (isClientUpdate == true)
            {
                if (crc.ToUpper() == CLIENT_CRC.ToUpper())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (crc.ToUpper() == CRC_VALUE.ToUpper())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        bool isGameFilesOK;

        public async void CheckGameFileCRC()
        {
            isGameFilesOK = true;
            GamePlayer.Navigate(new Uri("about:blank"));
            GamePlayer.Visibility = Visibility.Collapsed;
            ClientUpdateGrid.Visibility = Visibility.Visible;

            var gameFolder = await localFolder.GetFolderAsync("FloatingCastleData");
            var crcList = await gameFolder.GetFileAsync("CRC32.csv");
            var hashList = File.ReadAllLines(crcList.Path);
            CRC32 crc32 = new CRC32();
            var fileEnum = Directory.EnumerateFiles(gameFolder.Path, "*", SearchOption.AllDirectories);

            foreach (var item in fileEnum)
            {


                var fileName = Path.GetFileName(item);
                var storageFile = await StorageFile.GetFileFromPathAsync(item);
                var stream = await storageFile.OpenStreamForReadAsync();
                var fileCRC = await crc32.ComputeHash(stream);
                var CheckText = $"{fileName},{fileCRC}";
                Debug.WriteLine($"{fileName},{fileCRC}");
                if (hashList.Contains(CheckText))
                {
                    //file OK
                    stream.Dispose();
                    FileProgressInfo.Text = $"{fileName} is OK";
                    StartGameButton.IsEnabled = true;
                }
                else
                {
                    if (fileName != "CRC32.csv" && fileName != "credits.txt")
                    {
                        // file not OK
                        isGameFilesOK = false;
                        stream.Dispose();
                        FileProgressInfo.Text = $"{fileName} is corrupt";
                        GameInfo.Text += $"{fileName} is corrupt\n";

                        // Add delete files and redownload.
                    }
                }

            }

            FileProgressInfo.Text = "File verification finished";

            if (isGameFilesOK == false)
            {
                // redownload files
                FileProgressInfo.Text = "Error found in one or more files, you need to re-download the game files";
                localSettings.Values["GameDataVersion"] = "null";
                INSTALLED_DATA_VERSION = "null";

            }

        }


        public async void ExtractPackage()
        {
            FileProgressInfo.Text = "Extracting Data";
            Stream ExtractStream = await gameDataPackage.OpenStreamForReadAsync();

            ZipArchive archive = new ZipArchive(ExtractStream);
            archive.ExtractToDirectory(ApplicationData.Current.LocalFolder.Path);
            Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);
            FileProgressbar.IsIndeterminate = false;
            FileProgressbar.Value = 100;
            await gameDataPackage.DeleteAsync();
            FileProgressInfo.Text = "Extracting Complete, Starting Game";
            localSettings.Values["GameDataVersion"] = GAME_DATA_VERSION;
            await Task.Delay(500);
            StartGame();
        }


        public class CRC32
        {
            private readonly uint[] ChecksumTable;
            private readonly uint Polynomial = 0xEDB88320;

            public CRC32()
            {
                ChecksumTable = new uint[0x100];

                for (uint index = 0; index < 0x100; ++index)
                {
                    uint item = index;
                    for (int bit = 0; bit < 8; ++bit)
                        item = ((item & 1) != 0) ? (Polynomial ^ (item >> 1)) : (item >> 1);
                    ChecksumTable[index] = item;
                }
            }

            public async Task<string> ComputeHash(Stream stream)
            {
                uint result = 0xFFFFFFFF;

                int current;
                while ((current = stream.ReadByte()) != -1)
                    result = ChecksumTable[(result & 0xFF) ^ (byte)current] ^ (result >> 8);

                byte[] hash = BitConverter.GetBytes(~result);
                Array.Reverse(hash);

                String hashString = String.Empty;

                foreach (byte b in hash) hashString += b.ToString("x2").ToLower();

                return hashString;
            }

            /*public byte[] ComputeHash(byte[] data)
            {
                using (MemoryStream stream = new MemoryStream(data))
                    return ComputeHash(stream);
            }*/
        }

        private async void ExtractClientUpdate()
        {
            FileProgressInfo.Text = "Extracting Update";
            Stream ExtractStream = await ClientFile.OpenStreamForReadAsync();

            ZipArchive archive = new ZipArchive(ExtractStream);
            archive.ExtractToDirectory(ApplicationData.Current.LocalCacheFolder.Path);
            Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);
            FileProgressbar.IsIndeterminate = false;
            FileProgressbar.Value = 100;
            await ClientFile.DeleteAsync();
            FileProgressInfo.Text = "Extracting Complete, Starting Client Update";
            await Task.Delay(500);


            string AppxUpdateName = "FloatingCastleClient.appxbundle";
        var options = new Windows.System.LauncherOptions();
            options.PreferredApplicationPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";
            options.PreferredApplicationDisplayName = "App Installer";
            FileProgressInfo.Text = "Attempting to Install Update Package, Please Wait";
            await Windows.System.Launcher.LaunchFileAsync(await Windows.Storage.ApplicationData.Current.LocalCacheFolder.GetFileAsync(AppxUpdateName), options);

        }


        /// <summary>
        /// Progress for Download
        /// </summary>
        /// <param name="downloadOperation"></param>
        private async void progressChanged(DownloadOperation downloadOperation)
        {
            int progress = (int)(100 * ((double)downloadOperation.Progress.BytesReceived / (double)downloadOperation.Progress.TotalBytesToReceive));
            FileProgressbar.Value = progress;
            switch (downloadOperation.Progress.Status)
            {
                case BackgroundTransferStatus.Running:
                    {
                        if (isPackageValidationCheck == true)
                        {
                            FileProgressInfo.Text = "Downloading Package Metadata";
                        }
                        else
                        {
                            if (isClientUpdate == true)
                            {
                                FileProgressInfo.Text = $"Downloading Client Update {((long)downloadOperation.Progress.BytesReceived).ToFileSize()}/{((long)downloadOperation.Progress.TotalBytesToReceive).ToFileSize()}";

                            }
                            else
                            {
                                FileProgressInfo.Text = $"Downloading Game Data {((long)downloadOperation.Progress.BytesReceived).ToFileSize()}/{((long)downloadOperation.Progress.TotalBytesToReceive).ToFileSize()}";
                            }
                        }
                        break;
                    }
                case BackgroundTransferStatus.PausedByApplication:
                    {
                        FileProgressInfo.Text = "Download paused.";
                        break;
                    }
                case BackgroundTransferStatus.PausedCostedNetwork:
                    {
                        FileProgressInfo.Text = "Download paused because of metered connection.";
                        break;
                    }
                case BackgroundTransferStatus.PausedNoNetwork:
                    {
                        FileProgressInfo.Text = "No network detected. Please check your internet connection.";
                        break;
                    }
                case BackgroundTransferStatus.Error:
                    {
                        FileProgressInfo.Text = "An error occured while downloading.";
                        break;
                    }
            }
            if (progress >= 100)
            {
                if (isClientUpdate == true)
                {
                    FileProgressInfo.Text = "Client Update Downloaded";
                    await Task.Delay(500);
                    var CRCMatch = await CheckDataCRC(ClientFile);
                    if (CRCMatch == true)
                    {
                        FileProgressInfo.Text = "Package Verification Completed, PKG CRC: " + NEW_CRC;
                        downloadOperation = null;
                    ExtractClientUpdate();

                    }
                    else
                    {
                        FileProgressInfo.Text = "Integrity check of downloaded file failed";
                        downloadOperation = null;
                    }

                }
                else
                {
                    if (isPackageValidationCheck == true)
                    {
                        FileProgressInfo.Text = $"Metadata Fetched.";
                        downloadOperation = null;
                        await Task.Delay(500);
                        CheckPackageMetadata();
                    }
                    else
                    {
                        try
                        {
                            FileProgressInfo.Text = $"Download complete, Validating Package";
                            FileProgressbar.IsIndeterminate = true;
                            await Task.Delay(500);
                            //remove
                            //await gameDataPackage.DeleteAsync();

                            var CRCMatch = await CheckDataCRC(gameDataPackage);
                            if (CRCMatch == true)
                            {
                                FileProgressInfo.Text = "Package Verification Completed, PKG CRC: " + NEW_CRC;
                                downloadOperation = null;
                                ExtractPackage();
                            }
                            else
                            {
                                FileProgressInfo.Text = "Integrity check of downloaded file failed";
                                downloadOperation = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Exceptions.ThrownExceptionError(ex);
                        }

                    }
                    downloadOperation = null;
                }
            }

        }

        #region Client Menu Buttons

        private async void ReloadGameButton_Click(object sender, RoutedEventArgs e)
        {
            GamePlayer.Navigate(new Uri("about:blank"));
            await Task.Delay(500);
            GamePlayer.Navigate(new Uri(@"ms-appdata:///local/FloatingCastleData/index.html"));
        }

        private void CheckGameFilesButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide Webview and navigate to blank, check file CRCs
            ClientMenu.Visibility = Visibility.Collapsed;
            CheckGameFileCRC();
        }


        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ClientMenu.Visibility = Visibility.Collapsed;
        }

        private void ExitGameButton_Click(object sender, RoutedEventArgs e)
        {
            GamePlayer.Navigate(new Uri("about:blank"));
            Application.Current.Exit();
        }

        #endregion

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void GameInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {

                Exceptions.CustomException("[Controls]\n" +
                "Tap = Move/Activate/Select\n" +
                "Two Finger Tap = Open Menu/Go Back in Menus\n" +
                "Back Key = Open Client Menu\n\n\n" +
                "[Credits]\n" +
                "Empyreal96 - Game, Client\n" +
                "Bashar Astifan - RPG Save Manager Plugin");
            } else
            {
                Exceptions.CustomException("[Controls]\n" +
                "Left Click = Move/Activate/Select\n" +
                "Right Click/Esc = Open Menu/Go Back in Menus\n" +
                "Titlebar Back Button = Open Client Menu\n" +
                "Arrow Keys = Move\n" +
                "Hold Shift = Dash\n\n\n" +
                "[Credits]\n" +
                "Empyreal96 - Game, Client\n" +
                "Bashar Astifan - RPG Save Manager Plugin");
            }
        }
    }
}
