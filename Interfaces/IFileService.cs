// IFileService.cs
using HoudiniSafe.Models;

namespace HoudiniSafe.Interfaces
{
    /// <summary>
    /// Interface defining methods for interacting with files and folders in a cloud storage service.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Retrieves the contents of a specified folder asynchronously from the cloud storage.
        /// </summary>
        /// <param name="folderId">The ID of the folder to retrieve contents from. If <c>null</c>, retrieves the contents of the root folder.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains a list of <see cref="CloudItem"/> representing the contents of the folder.
        /// </returns>
        Task<List<CloudItem>> GetFolderContentsAsync(string folderId = null);
    }
}
