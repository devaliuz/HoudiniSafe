using System.Windows.Input;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using System.IO;
using HoudiniSafe.ViewModel.Commands;
using HoudiniSafe.ViewModel.Services;

namespace HoudiniSafe.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly GoogleAuthenticator _googleAuthenticator;

        private const string HoudiniFolderName = "Houdini";
        private string _houdiniFolderId;


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

        private DriveService _driveService;

        public SettingsViewModel()
        {
            _googleAuthenticator = new GoogleAuthenticator();
            ConnectToGoogleDriveCommand = new AsyncRelayCommand(ConnectToGoogleDrive);
            DisconnectFromGoogleDriveCommand = new RelayCommand(DisconnectFromGoogleDrive);
        }

        private async Task ConnectToGoogleDrive()
        {
            try
            {
                UserCredential credential = await _googleAuthenticator.AuthenticateAsync();
                _driveService = _googleAuthenticator.CreateDriveService(credential);

                IsGoogleDriveConnected = true;
                GoogleDriveConnectionStatus = "Verbunden";
                EnsureHoudiniFolderExistsAsync();
            }
            catch (Exception ex)
            {
                GoogleDriveConnectionStatus = $"Verbindungsfehler: {ex.Message}";
            }
        }

        /// <summary>
        /// Ensures that the "Houdini" folder exists in Google Drive, creating it if necessary.
        /// </summary>
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

        private void DisconnectFromGoogleDrive()
        {
            _driveService = null;
            IsGoogleDriveConnected = false;
            GoogleDriveConnectionStatus = "Nicht verbunden";

            // Remove stored credentials
            if (File.Exists("token.json"))
            {
                File.Delete("token.json");
            }
        }
    }
}