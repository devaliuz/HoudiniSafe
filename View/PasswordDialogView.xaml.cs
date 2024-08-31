using System.Windows;
using System.Windows.Controls;
using HoudiniSafe.ViewModel;

namespace HoudiniSafe.View
{
    public partial class PasswordDialogView : Window
    {
        public PasswordDialogView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is PasswordDialogViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}