// DragAndDropService.cs
using System.Windows;
using System.Windows.Input;

namespace HoudiniSafe.ViewModel.Services
{
    /// <summary>
    /// Provides methods and attached properties for implementing drag-and-drop functionality in WPF.
    /// </summary>
    public static class DragAndDropService
    {
        #region Attached Properties

        /// <summary>
        /// Identifies the DropCommand attached property.
        /// </summary>
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached(
                "DropCommand",
                typeof(ICommand),
                typeof(DragAndDropService),
                new PropertyMetadata(null, OnDropCommandChanged));

        /// <summary>
        /// Sets the DropCommand attached property on the specified DependencyObject.
        /// </summary>
        /// <param name="d">The DependencyObject to set the property on.</param>
        /// <param name="value">The ICommand to set as the value of the DropCommand property.</param>
        public static void SetDropCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(DropCommandProperty, value);
        }

        /// <summary>
        /// Gets the DropCommand attached property from the specified DependencyObject.
        /// </summary>
        /// <param name="d">The DependencyObject to get the property value from.</param>
        /// <returns>The ICommand value of the DropCommand property.</returns>
        public static ICommand GetDropCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(DropCommandProperty);
        }

        #endregion

        #region Property Changed Callback

        /// <summary>
        /// Called when the DropCommand property changes. Updates the UIElement's drag-and-drop
        /// event handlers based on whether a command is set.
        /// </summary>
        /// <param name="d">The DependencyObject whose property changed.</param>
        /// <param name="e">Event data containing information about the property change.</param>
        private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.NewValue != null)
                {
                    // Enable drag-and-drop and register event handlers
                    element.AllowDrop = true;
                    element.Drop += Element_Drop;
                    element.PreviewDragOver += Element_PreviewDragOver;
                }
                else
                {
                    // Disable drag-and-drop and unregister event handlers
                    element.AllowDrop = false;
                    element.Drop -= Element_Drop;
                    element.PreviewDragOver -= Element_PreviewDragOver;
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the PreviewDragOver event to specify the effects of a drag operation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing information about the drag operation.</param>
        private static void Element_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Set the drag effect based on whether files are being dragged
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// Handles the Drop event to execute the command associated with the dropped data.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing information about the drop operation.</param>
        private static void Element_Drop(object sender, DragEventArgs e)
        {
            var command = GetDropCommand((DependencyObject)sender);
            if (command != null && command.CanExecute(e))
            {
                // Execute the command with the DragEventArgs as parameter
                command.Execute(e);
            }
        }

        #endregion
    }
}
