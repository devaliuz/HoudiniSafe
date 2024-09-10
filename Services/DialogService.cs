//DialogService.cs
using System.Windows;
using System.Windows.Media;
using HoudiniSafe.View;
using HoudiniSafe.ViewModel;

namespace HoudiniSafe.Services
{
    /// <summary>
    /// Provides methods for displaying dialogs and popups within the application.
    /// </summary>
    public class DialogService
    {
        /// <summary>
        /// Shows a popup dialog with the specified message, title, and optional icon.
        /// </summary>
        /// <param name="message">The message to display in the popup.</param>
        /// <param name="title">The title of the popup. Defaults to "Information".</param>
        /// <param name="iconRessourceKey">The resource key for the icon to display in the popup. Can be null.</param>
        public void ShowPopup(string message, string title = "Information", string iconRessourceKey = null)
        {
            // Retrieve the icon from the application resources if an icon resource key is provided.
            // Otherwise, the icon is set to null.
            var icon = iconRessourceKey != null
                ? (ImageSource)Application.Current.Resources[iconRessourceKey]
                : null;

            // Create and configure the ViewModel for the popup dialog.
            var viewModel = new GenericPopupViewModel
            {
                Message = message,       // Set the message to be displayed in the popup.
                Icon = icon,             // Set the icon if provided.
                CloseAction = () =>
                {
                    // Action to be performed when the popup is closed.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Find the active GenericPopup window and close it.
                        var window = Application.Current.Windows
                            .OfType<GenericPopup>()
                            .SingleOrDefault(w => w.IsActive);

                        window?.Close();
                    });
                }
            };

            // Create and configure the GenericPopup window.
            var popup = new GenericPopup
            {
                DataContext = viewModel // Set the DataContext to the configured ViewModel.
            };

            // Display the popup as a modal dialog.
            popup.ShowDialog();
        }
    }
}
