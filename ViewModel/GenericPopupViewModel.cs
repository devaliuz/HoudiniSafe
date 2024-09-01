// GenericPopupViewModel.cs
using HoudiniSafe.ViewModel.Commands;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media;

namespace HoudiniSafe.ViewModel
{
    public class GenericPopupViewModel : ViewModelBase
    {
        private string _message;
        private ICommand _okCommand;
        private ImageSource _icon;


        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public ICommand OKCommand => _okCommand ??= new RelayCommand(OnOK);

        private void OnOK()
        {
            CloseAction?.Invoke();
        }

        public Action CloseAction { get; set; }
    }
}
