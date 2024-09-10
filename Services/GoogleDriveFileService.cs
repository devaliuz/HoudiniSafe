using Google.Apis.Drive.v3;
using HoudiniSafe.Interfaces;
using HoudiniSafe.Models;
using HoudiniSafe.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GoogleDriveFileService : IFileService
{
    private static readonly string[] Scopes = { DriveService.Scope.DriveReadonly };
    private const string ApplicationName = "HoudiniSafe";
    private DriveService _driveService;
    private static readonly Lazy<GoogleDriveFileService> _instance =
        new Lazy<GoogleDriveFileService>(() => new GoogleDriveFileService(GoogleAuthenticator.Instance));

    private bool _isInitialized = false;
    private readonly GoogleAuthenticator _googleAuthenticator;

    private GoogleDriveFileService(GoogleAuthenticator googleAuthenticator)
    {
        _googleAuthenticator = googleAuthenticator;
        InitializeDriveServiceAsync().GetAwaiter().GetResult();
    }

    public static GoogleDriveFileService Instance => _instance.Value;

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

    private async Task<string> GetHoudiniFolderIdAsync()
    {
        try
        {
            // List files and folders in the root directory to find the "Houdini" folder
            var listRequest = _driveService.Files.List();
            listRequest.PageSize = 100;
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.Q = "name = 'Houdini' and mimeType = 'application/vnd.google-apps.folder'";

            var result = await listRequest.ExecuteAsync();
            var houdiniFolder = result.Files.FirstOrDefault();
            return houdiniFolder?.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding the Houdini folder: {ex.Message}");
            return null;
        }
    }

    public async Task<List<CloudItem>> GetFolderContentsAsync(string folderId = null)
    {
        if (!_isInitialized)
        {
            return new List<CloudItem>(); // Return an empty list if not initialized
        }

        // If folderId is null, find the Houdini folder ID
        if (folderId == null)
        {
            folderId = await GetHoudiniFolderIdAsync();
            if (folderId == null)
            {
                return new List<CloudItem>(); // Return an empty list if Houdini folder is not found
            }
        }

        var folderContents = new List<CloudItem>();
        try
        {
            var listRequest = _driveService.Files.List();
            listRequest.PageSize = 100;
            listRequest.Fields = "nextPageToken, files(id, name, mimeType, parents)";
            // Filter for .enc files and folders
            listRequest.Q = $"('{folderId}' in parents) and (mimeType = 'application/vnd.google-apps.folder' or name contains '.enc')";

            var files = await listRequest.ExecuteAsync();
            if (files?.Files != null)
            {
                foreach (var file in files.Files)
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
            return new List<CloudItem>();
        }
        return folderContents;
    }
}
