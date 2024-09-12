using System.Windows.Input;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using System.IO;
using System.Text.Json;
using HoudiniSafe.ViewModel.Commands;
using HoudiniSafe.Services;

namespace HoudiniSafe.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly GoogleAuthenticator _googleAuthenticator;

        public const string HoudiniFolderName = "Houdini";
        private const string ConnectionStatusFile = "connection_status.json";
        public string _houdiniFolderId;

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        private string _googleDriveConnectionStatus = "Nicht verbunden";
        public string GoogleDriveConnectionStatus
        {
            get => _googleDriveConnectionStatus;
            set => SetProperty(ref _googleDriveConnectionStatus, value);
        }

        private bool _isGoogleDriveConnected;
        public bool IsGoogleDriveConnected
        {
            get => _isGoogleDriveConnected;
            set => SetProperty(ref _isGoogleDriveConnected, value);
        }

        public ICommand ConnectToGoogleDriveCommand { get; }
        public ICommand DisconnectFromGoogleDriveCommand { get; }
        public ICommand DeleteCredentialsCommand { get; }

        private DriveService _driveService;

        public GoogleAuthenticator GoogleAuthenticator { get; }
        public GoogleDriveFileService GoogleDriveFileService { get; set; }

        public SettingsViewModel()
        {
            GoogleAuthenticator = GoogleAuthenticator.Instance;
            ConnectToGoogleDriveCommand = new AsyncRelayCommand(ConnectToGoogleDrive);
            DisconnectFromGoogleDriveCommand = new RelayCommand(DisconnectFromGoogleDrive);
            DeleteCredentialsCommand = new RelayCommand(DeleteCredentials);

            LoadConnectionStatus();
        }

        private async Task ConnectToGoogleDrive()
        {
            try
            {
                UserCredential credential = await GoogleAuthenticator.AuthenticateAsync();
                _driveService = GoogleAuthenticator.CreateDriveService(credential);
                IsGoogleDriveConnected = true;

                var aboutRequest = _driveService.About.Get();
                aboutRequest.Fields = "user";
                var about = await aboutRequest.ExecuteAsync();
                string userEmail = about.User.EmailAddress;
                string displayName = about.User.DisplayName;

                GoogleDriveConnectionStatus = $"Verbunden als : {displayName}\n{userEmail}";
                await EnsureHoudiniFolderExistsAsync();
                SaveConnectionStatus();
            }
            catch (Exception ex)
            {
                GoogleDriveConnectionStatus = $"Verbindungsfehler: {ex.Message}";
            }

            InitializeDriveService();
        }

        private void InitializeDriveService()
        {
            GoogleDriveFileService = GoogleDriveFileService.Instance;
        }

        private async Task EnsureHoudiniFolderExistsAsync()
        {
            var folderListRequest = _driveService.Files.List();
            folderListRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{HoudiniFolderName}' and trashed=false";
            folderListRequest.Spaces = "drive";
            folderListRequest.Fields = "files(id, name)";

            var folderList = await folderListRequest.ExecuteAsync();

            if (folderList.Files.Count == 0)
            {
                var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = HoudiniFolderName,
                    MimeType = "application/vnd.google-apps.folder"
                };

                var request = _driveService.Files.Create(folderMetadata);
                request.Fields = "id";
                var folder = await request.ExecuteAsync();
                _houdiniFolderId = folder.Id;
            }
            else
            {
                _houdiniFolderId = folderList.Files[0].Id;
            }
        }

        public void DisconnectFromGoogleDrive()
        {
            _driveService = null;
            IsGoogleDriveConnected = false;
            GoogleDriveConnectionStatus = "Nicht verbunden";

            if (File.Exists("token.json"))
            {
                File.Delete("token.json");
            }

            SaveConnectionStatus();
        }

        private void DeleteCredentials()
        {
            if (File.Exists("token.json"))
            {
                File.Delete("token.json");
            }
            GoogleDriveConnectionStatus = "Anmeldedaten gelöscht";
        }

        private void SaveConnectionStatus()
        {
            var status = new ConnectionStatus
            {
                IsConnected = IsGoogleDriveConnected,
                ConnectionStatusMessage = GoogleDriveConnectionStatus
            };

            string jsonString = JsonSerializer.Serialize(status);
            File.WriteAllText(ConnectionStatusFile, jsonString);
        }

        private void LoadConnectionStatus()
        {
            if (File.Exists(ConnectionStatusFile))
            {
                string jsonString = File.ReadAllText(ConnectionStatusFile);
                var status = JsonSerializer.Deserialize<ConnectionStatus>(jsonString);

                if (status != null)
                {
                    IsGoogleDriveConnected = status.IsConnected;
                    GoogleDriveConnectionStatus = status.ConnectionStatusMessage;

                    if (IsGoogleDriveConnected)
                    {
                        InitializeDriveService();
                    }
                }
            }
        }

        public async Task<bool> RestoreLastConnectionAsync()
        {
            LoadConnectionStatus();
            if (IsGoogleDriveConnected)
            {
                try
                {
                    await ConnectToGoogleDrive();
                    return true;
                }
                catch
                {
                    IsGoogleDriveConnected = false;
                    GoogleDriveConnectionStatus = "Verbindung fehlgeschlagen";
                    SaveConnectionStatus();
                    return false;
                }
            }
            return false;
        }

        private class ConnectionStatus
        {
            public bool IsConnected { get; set; }
            public string ConnectionStatusMessage { get; set; }
        }
    }
}