// GenericPopupViewModel.cs
using HoudiniSafe.ViewModel.Commands;
using System.Windows.Input;
using System.Windows.Media;

namespace HoudiniSafe.ViewModel
{
    /// <summary>
    /// ViewModel for a generic popup window.
    /// Manages the message, icon, and close command of the popup.
    /// </summary>
    public class GenericPopupViewModel : ViewModelBase
    {
        #region Fields

        private string _message;
        private ICommand _okCommand;
        private ImageSource _icon;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the message to be displayed in the popup.
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// Gets or sets the icon to be displayed in the popup.
        /// </summary>
        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// Command that executes when the OK button is pressed.
        /// </summary>
        public ICommand OKCommand => _okCommand ??= new RelayCommand(OnOK);

        #endregion

        #region Methods

        /// <summary>
        /// Executes the OK command, which triggers the CloseAction.
        /// </summary>
        private void OnOK()
        {
            CloseAction?.Invoke();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Action to be invoked when the OK button is pressed.
        /// Typically used to close the popup window.
        /// </summary>
        public Action CloseAction { get; set; }

        #endregion
    }
}
