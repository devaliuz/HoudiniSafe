using Google.Apis.Drive.v3;
using HoudiniSafe.Interfaces;
using HoudiniSafe.Models;
using HoudiniSafe.Services;

public class GoogleDriveFileService : IFileService
{
    private static readonly string[] Scopes = { DriveService.Scope.DriveReadonly };
    private const string ApplicationName = "HoudiniSafe";
    private DriveService _driveService;
    private readonly GoogleAuthenticator _googleAuthenticator;
    private bool _isInitialized = false;

    public GoogleDriveFileService(GoogleAuthenticator googleAuthenticator)
    {
        _googleAuthenticator = googleAuthenticator;
    }

    private async Task InitializeDriveServiceAsync()
    {
        if (_isInitialized) return;
        try
        {
            var credential = await _googleAuthenticator.AuthenticateAsync();
            _driveService = _googleAuthenticator.CreateDriveService(credential);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Google Drive service: {ex.Message}");
            throw;
        }
    }

    public async Task<List<CloudItem>> GetFolderContentsAsync(string folderId = null)
    {
        if (!_isInitialized)
        {
            return new List<CloudItem>(); // Return an empty list if not initialized
        }

        var folderContents = new List<CloudItem>();
        try
        {
            FilesResource.ListRequest listRequest = _driveService.Files.List();
            listRequest.PageSize = 100;
            listRequest.Fields = "nextPageToken, files(id, name, mimeType, parents)";

            if (folderId != null)
            {
                listRequest.Q = $"'{folderId}' in parents";
            }
            else
            {
                listRequest.Q = "'root' in parents";
            }

            var files = listRequest.Execute().Files;
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var cloudItem = new CloudItem
                    {
                        Name = file.Name,
                        Id = file.Id,
                        IsFolder = file.MimeType == "application/vnd.google-apps.folder"
                    };
                    folderContents.Add(cloudItem);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching folder contents: {ex.Message}");
            // Instead of throwing, return an empty list
            return new List<CloudItem>();
        }
        return folderContents;
    }
}