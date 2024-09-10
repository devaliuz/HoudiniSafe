using System.Windows;
using System.Windows.Input;

namespace HoudiniSafe.Services
{
    public static class DragAndDropService
    {
        #region Attached Properties

        // DropCommand attached property definition
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached(
                "DropCommand",
                typeof(ICommand),
                typeof(DragAndDropService),
                new PropertyMetadata(null, OnDropCommandChanged));

        // DragCommand attached property definition
        public static readonly DependencyProperty DragCommandProperty =
            DependencyProperty.RegisterAttached(
                "DragCommand",
                typeof(ICommand),
                typeof(DragAndDropService),
                new PropertyMetadata(null, OnDragCommandChanged));

        public static void SetDropCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(DropCommandProperty, value);
        }

        public static ICommand GetDropCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(DropCommandProperty);
        }

        public static void SetDragCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(DragCommandProperty, value);
        }

        public static ICommand GetDragCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(DragCommandProperty);
        }

        #endregion

        #region Property Changed Callbacks

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

        private static void OnDragCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.NewValue != null)
                {
                    element.MouseMove += Element_MouseMove;
                }
                else
                {
                    element.MouseMove -= Element_MouseMove;
                }
            }
        }

        #endregion

        #region Event Handlers

        private static void Element_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.Move;
            e.Handled = true;
        }

        private static void Element_Drop(object sender, DragEventArgs e)
        {
            var command = GetDropCommand((DependencyObject)sender);
            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }

        private static void Element_MouseMove(object sender, MouseEventArgs e)
        {
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
