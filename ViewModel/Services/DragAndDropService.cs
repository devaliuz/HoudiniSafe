using System.Windows;
using System.Windows.Input;

namespace HoudiniSafe.ViewModel.Services
{
    public static class DragAndDropService
    {
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached("DropCommand", typeof(ICommand), typeof(DragAndDropService), new PropertyMetadata(null, OnDropCommandChanged));

        public static void SetDropCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(DropCommandProperty, value);
        }

        public static ICommand GetDropCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(DropCommandProperty);
        }

        private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.NewValue != null)
                {
                    element.AllowDrop = true;
                    element.Drop += Element_Drop;
                    element.PreviewDragOver += Element_PreviewDragOver;
                }
                else
                {
                    element.AllowDrop = false;
                    element.Drop -= Element_Drop;
                    element.PreviewDragOver -= Element_PreviewDragOver;
                }
            }
        }

        private static void Element_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
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
    }
}