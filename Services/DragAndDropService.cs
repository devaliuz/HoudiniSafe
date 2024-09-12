// DragAndDropService.cs
using System.Windows;
using System.Windows.Input;

namespace HoudiniSafe.Services
{
    /// <summary>
    /// A static service class that provides attached properties and methods to enable drag-and-drop functionality
    /// for UI elements in WPF using commands.
    /// </summary>
    public static class DragAndDropService
    {
        #region Attached Properties

        /// <summary>
        /// Identifies the DropCommand attached property. This property is used to bind a command that will be executed when a drop event occurs on the element.
        /// </summary>
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached(
                "DropCommand",
                typeof(ICommand),
                typeof(DragAndDropService),
                new PropertyMetadata(null, OnDropCommandChanged));

        /// <summary>
        /// Identifies the DragCommand attached property. This property is used to bind a command that will be executed when a drag event is initiated on the element.
        /// </summary>
        public static readonly DependencyProperty DragCommandProperty =
            DependencyProperty.RegisterAttached(
                "DragCommand",
                typeof(ICommand),
                typeof(DragAndDropService),
                new PropertyMetadata(null, OnDragCommandChanged));

        /// <summary>
        /// Sets the DropCommand attached property for the specified dependency object.
        /// </summary>
        /// <param name="d">The dependency object on which to set the property.</param>
        /// <param name="value">The ICommand to set.</param>
        public static void SetDropCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(DropCommandProperty, value);
        }

        /// <summary>
        /// Gets the DropCommand attached property from the specified dependency object.
        /// </summary>
        /// <param name="d">The dependency object from which to get the property.</param>
        /// <returns>The ICommand value.</returns>
        public static ICommand GetDropCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(DropCommandProperty);
        }

        /// <summary>
        /// Sets the DragCommand attached property for the specified dependency object.
        /// </summary>
        /// <param name="d">The dependency object on which to set the property.</param>
        /// <param name="value">The ICommand to set.</param>
        public static void SetDragCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(DragCommandProperty, value);
        }

        /// <summary>
        /// Gets the DragCommand attached property from the specified dependency object.
        /// </summary>
        /// <param name="d">The dependency object from which to get the property.</param>
        /// <returns>The ICommand value.</returns>
        public static ICommand GetDragCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(DragCommandProperty);
        }

        #endregion

        #region Property Changed Callbacks

        /// <summary>
        /// Called when the DropCommand attached property is changed. Registers or unregisters event handlers for drag-and-drop events.
        /// </summary>
        /// <param name="d">The dependency object on which the property is changed.</param>
        /// <param name="e">The event data for the property change.</param>
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

        /// <summary>
        /// Called when the DragCommand attached property is changed. Registers or unregisters event handlers for mouse movement.
        /// </summary>
        /// <param name="d">The dependency object on which the property is changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnDragCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.NewValue != null)
                {
                    // Register event handler to detect mouse movement for drag initiation
                    element.MouseMove += Element_MouseMove;
                }
                else
                {
                    // Unregister event handler when no drag command is set
                    element.MouseMove -= Element_MouseMove;
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the PreviewDragOver event to set the appropriate drag-and-drop effect and mark the event as handled.
        /// </summary>
        /// <param name="sender">The UIElement over which the drag operation is occurring.</param>
        /// <param name="e">The drag event arguments.</param>
        private static void Element_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Set drag effect based on the data format being dragged
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.Move;
            e.Handled = true;
        }

        /// <summary>
        /// Handles the Drop event to execute the bound command when a drop action occurs.
        /// </summary>
        /// <param name="sender">The UIElement on which the drop occurred.</param>
        /// <param name="e">The drag event arguments containing drop data.</param>
        private static void Element_Drop(object sender, DragEventArgs e)
        {
            var command = GetDropCommand((DependencyObject)sender);
            if (command != null && command.CanExecute(e))
            {
                // Execute the command with drag event data
                command.Execute(e);
            }
        }

        /// <summary>
        /// Handles the MouseMove event to detect dragging gestures and initiate a drag-and-drop operation.
        /// </summary>
        /// <param name="sender">The UIElement where the mouse is moving.</param>
        /// <param name="e">The mouse event arguments.</param>
        private static void Element_MouseMove(object sender, MouseEventArgs e)
        {
            // Check if the left mouse button is pressed
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var command = GetDragCommand((DependencyObject)sender);
                if (command != null && command.CanExecute(e))
                {
                    // Execute the command to initiate the drag operation
                    command.Execute(e);

                    // Start the drag-and-drop operation
                    DragDrop.DoDragDrop((DependencyObject)sender, ((FrameworkElement)sender).DataContext, DragDropEffects.Move);
                }
            }
        }

        #endregion
    }
}
