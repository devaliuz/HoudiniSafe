//IFileService.cs
using HoudiniSafe.Models;
using System.Collections.ObjectModel;

namespace HoudiniSafe.Interfaces
{
    public interface IFileService
    {
        public Task<List<CloudItem>> GetFolderContentsAsync(string folderId = null);
    }
}
