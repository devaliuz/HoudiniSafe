using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using HoudiniSafe.ViewModel.Commands;
using HoudiniSafe.Viewmodel.Services;
using HoudiniSafe.View;

namespace HoudiniSafe.ViewModel
{
    public class MainViewViewModel : ViewModelBase
    {
        private readonly EncryptionService _encryptionService;

        private ObservableCollection<string> _droppedFiles;
        public ObservableCollection<string> DroppedFiles
        {
            get => _droppedFiles;
            set => SetProperty(ref _droppedFiles, value);
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private Visibility _progressVisibility = Visibility.Collapsed;
        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set => SetProperty(ref _progressVisibility, value);
        }

        public ICommand OpenFileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand DropCommand { get; }
        public ICommand EncryptCommand { get; }
        public ICommand DecryptCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand RemoveAllFilesCommand { get; }

        public MainViewViewModel()
        {
            _encryptionService = new EncryptionService();
            DroppedFiles = new ObservableCollection<string>();
            RemoveFileCommand = new RelayCommand<string>(RemoveFile);
            RemoveAllFilesCommand = new RelayCommand(RemoveAllFiles);
            OpenFileCommand = new RelayCommand(OpenFile);
            ExitCommand = new RelayCommand(Exit);
            AboutCommand = new RelayCommand(ShowAbout);
            DropCommand = new RelayCommand<DragEventArgs>(HandleDrop);
            EncryptCommand = new AsyncRelayCommand(EncryptAsync);
            DecryptCommand = new AsyncRelayCommand(DecryptAsync);
        }

        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                AddDroppedFile(openFileDialog.FileName);
            }
        }

        private void Exit()
        {
            Application.Current.Shutdown();
        }

        private void ShowAbout()
        {
            MessageBox.Show("HoudiniSafe - Sicheres Verschlüsselungstool", "Über", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HandleDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    AddDroppedFile(file);
                }
            }
        }

        public void AddDroppedFile(string filePath)
        {
            if (!DroppedFiles.Contains(filePath))
            {
                DroppedFiles.Add(filePath);
            }
        }

        private void RemoveFile(string file)
        {
            if (DroppedFiles.Contains(file))
            {
                DroppedFiles.Remove(file);
            }
        }

        private void RemoveAllFiles()
        {
            DroppedFiles.Clear();
        }

        private async Task EncryptAsync()
        {
            if (DroppedFiles.Count == 0)
            {
                MessageBox.Show("Bitte wählen Sie mindestens eine Datei oder einen Ordner aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string password = ShowPasswordDialog("Passwort für Verschlüsselung eingeben");
            if (string.IsNullOrEmpty(password)) return;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Verschlüsselte Dateien (*.enc)|*.enc",
                Title = "Speicherort für verschlüsselte Datei wählen"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            ProgressVisibility = Visibility.Visible;
            ProgressValue = 0;

            var progress = new Progress<double>(value => ProgressValue = value * 100);

            try
            {
                if (DroppedFiles.Count == 1)
                {
                    string file = DroppedFiles[0];
                    if (Directory.Exists(file))
                    {
                        await _encryptionService.EncryptFolderAsync(file, saveFileDialog.FileName, password, progress);
                    }
                    else
                    {
                        await _encryptionService.EncryptFileAsync(file, saveFileDialog.FileName, password, progress);
                    }
                }
                else
                {
                    await _encryptionService.EncryptMultipleFilesAsync(DroppedFiles.ToArray(), saveFileDialog.FileName, password, progress);
                }

                MessageBox.Show("Verschlüsselung erfolgreich abgeschlossen.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                DroppedFiles.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Verschlüsselung: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressVisibility = Visibility.Collapsed;
            }
        }

        private async Task DecryptAsync()
        {
            if (DroppedFiles.Count != 1)
            {
                MessageBox.Show("Bitte wählen Sie genau eine verschlüsselte Datei aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string password = ShowPasswordDialog("Passwort für Entschlüsselung eingeben");
            if (string.IsNullOrEmpty(password)) return;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Alle Dateien (*.*)|*.*",
                Title = "Speicherort für entschlüsselte Datei wählen"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            ProgressVisibility = Visibility.Visible;
            ProgressValue = 0;

            var progress = new Progress<double>(value => ProgressValue = value * 100);

            try
            {
                await _encryptionService.DecryptFileAsync(DroppedFiles[0], saveFileDialog.FileName, password, progress);
                MessageBox.Show("Entschlüsselung erfolgreich abgeschlossen.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                DroppedFiles.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Entschlüsselung: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressVisibility = Visibility.Collapsed;
            }
        }

        private string ShowPasswordDialog(string title)
        {
            var viewModel = new PasswordDialogViewModel(title);
            var dialog = new PasswordDialogView
            {
                DataContext = viewModel
            };

            if (dialog.ShowDialog() == true)
            {
                return viewModel.Password;
            }

            return null;
        }
    }
}