// CloudItem.cs
using HoudiniSafe.ViewModel;
using System.Collections.ObjectModel;

namespace HoudiniSafe.Models
{
    /// <summary>
    /// Represents an item in cloud storage, such as a file or folder, with support for hierarchical structures.
    /// </summary>
    public class CloudItem : ViewModelBase
    {
        #region Private Fields

        private string _name; // The name of the cloud item (file or folder)
        private string _id; // The unique identifier for the cloud item
        private bool _isFolder; // Indicates whether the item is a folder
        private ObservableCollection<CloudItem> _children; // The collection of child items, if the item is a folder

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the cloud item.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Gets or sets the unique identifier of the cloud item.
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the cloud item is a folder.
        /// </summary>
        public bool IsFolder
        {
            get => _isFolder;
            set => SetProperty(ref _isFolder, value);
        }

        /// <summary>
        /// Gets or sets the collection of child items. This property is applicable if the cloud item is a folder.
        /// </summary>
        public ObservableCollection<CloudItem> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudItem"/> class with an empty collection of children.
        /// </summary>
        public CloudItem()
        {
            // Initialize the Children collection to support hierarchical structure
            Children = new ObservableCollection<CloudItem>();
        }

        #endregion
    }
}
