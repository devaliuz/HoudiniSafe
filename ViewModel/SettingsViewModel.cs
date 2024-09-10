using System.Windows.Input;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using System.IO;
using HoudiniSafe.ViewModel.Commands;
using HoudiniSafe.Services;
using Google.Apis.Services;

namespace HoudiniSafe.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly GoogleAuthenticator _googleAuthenticator;

        public const string HoudiniFolderName = "Houdini";
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

        private DriveService _driveService;

        public GoogleAuthenticator GoogleAuthenticator { get ; }
        public GoogleDriveFileService GoogleDriveFileService { get; set; }

        public SettingsViewModel()
        {
            GoogleAuthenticator = GoogleAuthenticator.Instance;
            ConnectToGoogleDriveCommand = new AsyncRelayCommand(ConnectToGoogleDrive);
            DisconnectFromGoogleDriveCommand = new RelayCommand(DisconnectFromGoogleDrive);

        }


        private async Task ConnectToGoogleDrive()
        {
            try
            {
                UserCredential credential = await GoogleAuthenticator.AuthenticateAsync();
                _driveService = GoogleAuthenticator.CreateDriveService(credential);
                IsGoogleDriveConnected = true;

                // Hole die tatsächliche E-Mail-Adresse des Benutzers
                var aboutRequest = _driveService.About.Get();
                aboutRequest.Fields = "user";
                var about = await aboutRequest.ExecuteAsync();
                string userEmail = about.User.EmailAddress;
                string displayName = about.User.DisplayName;

                GoogleDriveConnectionStatus = $"Verbunden als : {displayName}\n{userEmail}";
                await EnsureHoudiniFolderExistsAsync();
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