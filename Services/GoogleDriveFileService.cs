using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using HoudiniSafe.Interfaces;
using HoudiniSafe.Models;
using HoudiniSafe.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class GoogleDriveFileService : IFileService
{
    private static readonly string[] Scopes = { DriveService.Scope.Drive };  // Changed to full access
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
            Console.WriteLine("Google Drive service initialized successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Google Drive service: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<string> GetHoudiniFolderIdAsync()
    {
        try
        {
            Console.WriteLine("Searching for Houdini folder...");
            var listRequest = _driveService.Files.List();
            listRequest.Q = "name = 'Houdini' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listRequest.Fields = "files(id, name)";

            var result = await listRequest.ExecuteAsync();
            var houdiniFolder = result.Files.FirstOrDefault();

            if (houdiniFolder != null)
            {
                Console.WriteLine($"Found Houdini folder. ID: {houdiniFolder.Id}");
                return houdiniFolder.Id;
            }
            else
            {
                Console.WriteLine("Houdini folder not found. Creating it...");
                var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = "Houdini",
                    MimeType = "application/vnd.google-apps.folder"
                };

                var request = _driveService.Files.Create(folderMetadata);
                request.Fields = "id";
                var folder = await request.ExecuteAsync();

                Console.WriteLine($"Houdini folder created. Folder ID: {folder.Id}");
                return folder.Id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding or creating the Houdini folder: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<List<CloudItem>> GetFolderContentsAsync(string folderId = null)
    {
        if (!_isInitialized)
        {
            Console.WriteLine("Google Drive service is not initialized.");
            return new List<CloudItem>();
        }

        try
        {
            if (folderId == null)
            {
                folderId = await GetHoudiniFolderIdAsync();
                if (folderId == null)
                {
                    Console.WriteLine("Houdini folder not found.");
                    return new List<CloudItem>();
                }
            }

            Console.WriteLine($"Searching for contents in folder with ID: {folderId}");

            var folderContents = new List<CloudItem>();
            string pageToken = null;

            do
            {
                var listRequest = _driveService.Files.List();
                listRequest.Q = $"'{folderId}' in parents and trashed = false";
                listRequest.Fields = "nextPageToken, files(id, name, mimeType)";
                listRequest.PageToken = pageToken;
                listRequest.PageSize = 1000;

                var result = await listRequest.ExecuteAsync();

                Console.WriteLine($"Retrieved {result.Files.Count} items from Google Drive");

                foreach (var file in result.Files)
                {
                    Console.WriteLine($"Found item: {file.Name}, Type: {file.MimeType}");

                    if (file.MimeType == "application/vnd.google-apps.folder" || file.Name.EndsWith(".enc"))
                    {
                        var cloudItem = new CloudItem
                        {
                            Name = file.Name,
                            Id = file.Id,
                            IsFolder = file.MimeType == "application/vnd.google-apps.folder"
                        };
                        folderContents.Add(cloudItem);
                        Console.WriteLine($"Added to list: {cloudItem.Name}, IsFolder: {cloudItem.IsFolder}");
                    }
                }

                pageToken = result.NextPageToken;
            } while (pageToken != null);

            Console.WriteLine($"Total items added to folderContents: {folderContents.Count}");

            return folderContents.OrderBy(item => !item.IsFolder).ThenBy(item => item.Name).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching folder contents: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new List<CloudItem>();
        }
    }

    public async Task<bool> UploadFileAsync(string filePath, IProgress<double> progress)
    {
        if (!_isInitialized)
        {
            Console.WriteLine("Google Drive service is not initialized.");
            return false;
        }

        try
        {
            string houdiniFolder = await GetHoudiniFolderIdAsync();
            if (string.IsNullOrEmpty(houdiniFolder))
            {
                Console.WriteLine("Failed to get or create Houdini folder.");
                return false;
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(filePath),
                Parents = new List<string> { houdiniFolder }
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                request = _driveService.Files.Create(fileMetadata, stream, "application/octet-stream");
                request.Fields = "id";

                request.ProgressChanged += (IUploadProgress uploadProgress) =>
                {
                    switch (uploadProgress.Status)
                    {
                        case UploadStatus.Uploading:
                            {
                                double percentComplete = (double)uploadProgress.BytesSent / stream.Length * 100;
                                progress.Report(percentComplete);
                                break;
                            }
                        case UploadStatus.Failed:
                            {
                                Console.WriteLine($"Upload failed: {uploadProgress.Exception?.Message}");
                                break;
                            }
                    }
                };

                var result = await request.UploadAsync();

                if (result.Status == UploadStatus.Failed)
                {
                    Console.WriteLine($"Error uploading file: {result.Exception.Message}");
                    return false;
                }
            }

            Console.WriteLine($"File uploaded successfully. File ID: {request.ResponseBody?.Id}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while uploading the file: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<string> DownloadFileAsync(string fileId)
    {
        try
        {
            var request = _driveService.Files.Get(fileId);
            var file = await request.ExecuteAsync();

            if (file == null)
            {
                throw new Exception($"File with ID {fileId} not found.");
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), file.Name);

            using (var memoryStream = new MemoryStream())
            {
                var downloadRequest = _driveService.Files.Get(fileId);
                await downloadRequest.DownloadAsync(memoryStream);

                memoryStream.Position = 0;

                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await memoryStream.CopyToAsync(fileStream);
                }
            }

            Console.WriteLine($"File downloaded successfully: {tempFilePath}");
            return tempFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while downloading the file: {ex.Message}");
            throw;
        }

    }
}