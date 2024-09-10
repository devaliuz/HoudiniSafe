// CloudItem.cs
using HoudiniSafe.ViewModel;
using System.Collections.ObjectModel;

namespace HoudiniSafe.Models
{
    /// <summary>
    /// Represents an item in the cloud storage.
    /// </summary>
    public class CloudItem : ViewModelBase
    {
        #region Fields
        private string _name;
        private string _id;
        private bool _isFolder;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the name of the cloud item.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Gets or sets the ID of the cloud item.
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this item is a folder.
        /// </summary>
        public bool IsFolder
        {
            get => _isFolder;
            set => SetProperty(ref _isFolder, value);
        }
        #endregion
    }
}