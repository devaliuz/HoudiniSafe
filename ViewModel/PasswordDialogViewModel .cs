using System.Windows.Input;
using HoudiniSafe.ViewModel.Commands;

namespace HoudiniSafe.ViewModel
{
    public class PasswordDialogViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ICommand OkCommand { get; }

        public PasswordDialogViewModel(string title)
        {
            Title = title;
            OkCommand = new RelayCommand(OnOk, CanExecuteOk);
        }

        private bool CanExecuteOk()
        {
            return !string.IsNullOrWhiteSpace(Password);
        }

        private void OnOk()
        {
            // This method will be called when the OK button is clicked
            // The dialog will be closed in the view's code-behind
        }
    }
}