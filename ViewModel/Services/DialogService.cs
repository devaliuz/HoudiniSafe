// DialogService.cs
using System.Windows;
using System.Windows.Media;
using HoudiniSafe.View;

namespace HoudiniSafe.ViewModel.Services
{
    public class DialogService
    {
        public void ShowPopup(string message, string title = "Information", string iconRessourceKey = null)
        {
            var icon = iconRessourceKey != null ? (ImageSource)Application.Current.Resources[iconRessourceKey] : null;
            var viewModel = new GenericPopupViewModel
            {
                Message = message,
                Icon = icon,
                CloseAction = () =>
                {
                    // Hier wird das Popup geschlossen
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var window = Application.Current.Windows.OfType<GenericPopup>().SingleOrDefault(w => w.IsActive);
                        window?.Close();
                    });
                }
            };

            var popup = new GenericPopup
            {
                DataContext = viewModel
            };

            popup.ShowDialog();
        }

    }
}
