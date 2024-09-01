// PasswordDialogViewModel.cs
using System.Windows.Input;
using HoudiniSafe.ViewModel.Commands;

namespace HoudiniSafe.ViewModel
{
    /// <summary>
    /// ViewModel for the password input dialog.
    /// </summary>
    public class PasswordDialogViewModel : ViewModelBase
    {
        #region Fields

        // The title of the dialog
        private string _title;

        // The password entered by the user
        private string _password;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the title of the dialog.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Gets or sets the password entered by the user.
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// Command to execute when the OK button is clicked.
        /// </summary>
        public ICommand OkCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PasswordDialogViewModel class with the specified title.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        public PasswordDialogViewModel(string title)
        {
            Title = title;

            // Initialize the OK command with its execution logic and condition
            OkCommand = new RelayCommand(OnOk, CanExecuteOk);
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// Determines if the OK command can be executed.
        /// </summary>
        /// <returns>True if the password is not null or whitespace; otherwise, false.</returns>
        private bool CanExecuteOk() => !string.IsNullOrWhiteSpace(Password);

        /// <summary>
        /// Executes the OK command. 
        /// </summary>
        private void OnOk()
        {
            // This method will be called when the OK button is clicked.
            // The dialog will be closed in the view's code-behind.
        }

        #endregion
    }
}
