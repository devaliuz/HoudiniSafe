using HoudiniSafe.ViewModel.Services;
using HoudiniSafe.ViewModel.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using HoudiniSafe.View;
using HoudiniSafe.Viewmodel.Services;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace HoudiniSafe.ViewModel
{
    public class MainViewViewModel : ViewModelBase
    {
        private readonly EncryptionService _encryptionService;
        private readonly DialogService _dialogService;
        private ObservableCollection<string> _droppedFiles;
        private double _progressValue;
        private Visibility _progressVisibility = Visibility.Collapsed;
        private bool _replaceOriginal;

        public ObservableCollection<string> DroppedFiles
        {
            get => _droppedFiles;
            set
            {
                if (SetProperty(ref _droppedFiles, value))
                {
                    // Aktualisiere abhängige Eigenschaften, wenn sich die DroppedFiles ändern
                    OnPropertyChanged(nameof(CanEncrypt));
                    OnPropertyChanged(nameof(CanDecrypt));
                }
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set => SetProperty(ref _progressVisibility, value);
        }

        public bool ReplaceOriginal
        {
            get => _replaceOriginal;
            set => SetProperty(ref _replaceOriginal, value);
        }

        public ICommand OpenFileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand DropCommand { get; }
        public ICommand EncryptCommand { get; }
        public ICommand DecryptCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand RemoveAllFilesCommand { get; }

        public bool CanEncrypt => DroppedFiles != null && DroppedFiles.Count > 0 && DroppedFiles.All(file => !file.EndsWith(".enc"));
        public bool CanDecrypt => DroppedFiles != null && DroppedFiles.Count > 0 && DroppedFiles.All(file => file.EndsWith(".enc"));

        public MainViewViewModel()
        {
            _encryptionService = new EncryptionService();
            _dialogService = new DialogService();
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
            _dialogService.ShowPopup("HoudiniSafe - Sicheres Verschlüsselungstool", "Über");
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
                OnPropertyChanged(nameof(CanEncrypt));
                OnPropertyChanged(nameof(CanDecrypt));
            }
        }

        private void RemoveFile(string file)
        {
            if (DroppedFiles.Contains(file))
            {
                DroppedFiles.Remove(file);
                OnPropertyChanged(nameof(CanEncrypt));
                OnPropertyChanged(nameof(CanDecrypt));
            }
        }

        private void RemoveAllFiles()
        {
            DroppedFiles.Clear();
            OnPropertyChanged(nameof(CanEncrypt));
            OnPropertyChanged(nameof(CanDecrypt));
        }

        private async Task EncryptAsync()
        {
            if (DroppedFiles.Count == 0)
            {
                _dialogService.ShowPopup("Bitte wählen Sie mindestens eine Datei oder einen Ordner aus.", "Fehler");
                return;
            }

            string password = ShowPasswordDialog("Passwort für Verschlüsselung eingeben");
            if (string.IsNullOrEmpty(password)) return;

            string outputFile = ReplaceOriginal ? DroppedFiles[0] : GetSaveFilePath();
            if (string.IsNullOrEmpty(outputFile)) return;

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
                        await _encryptionService.EncryptFolderAsync(file, outputFile, password, progress, ReplaceOriginal);
                    }
                    else
                    {
                        await _encryptionService.EncryptFileAsync(file, outputFile, password, progress, ReplaceOriginal);
                    }
                }
                else
                {
                    await _encryptionService.EncryptMultipleFilesAsync(DroppedFiles.ToArray(), outputFile, password, progress, ReplaceOriginal);
                }

                _dialogService.ShowPopup("Verschlüsselung erfolgreich abgeschlossen.", "Erfolg", "EncryptIcon");
                DroppedFiles.Clear();
            }
            catch (Exception ex)
            {
                _dialogService.ShowPopup($"Fehler bei der Verschlüsselung: {ex.Message}", "Fehler");
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
                _dialogService.ShowPopup("Bitte wählen Sie genau eine verschlüsselte Datei aus.", "Fehler");
                return;
            }

            string password = ShowPasswordDialog("Passwort für Entschlüsselung eingeben");
            if (string.IsNullOrEmpty(password)) return;

            string outputFile = ReplaceOriginal ? DroppedFiles[0] : GetSaveFilePath();
            if (string.IsNullOrEmpty(outputFile)) return;

            ProgressVisibility = Visibility.Visible;
            ProgressValue = 0;

            var progress = new Progress<double>(value => ProgressValue = value * 100);

            try
            {
                await _encryptionService.DecryptFileAsync(DroppedFiles[0], outputFile, password, progress, ReplaceOriginal);
                _dialogService.ShowPopup("Entschlüsselung erfolgreich abgeschlossen.", "Erfolg", "DecryptIcon");
                DroppedFiles.Clear();
            }
            catch (Exception ex)
            {
                _dialogService.ShowPopup($"Fehler bei der Entschlüsselung: {ex.Message}", "Fehler");
            }
            finally
            {
                ProgressVisibility = Visibility.Collapsed;
            }
        }

        private string GetSaveFilePath()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true, // Nur Ordner auswählen
                Title = "Wählen Sie den Speicherort aus"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName; // Gibt den ausgewählten Ordnerpfad zurück
            }

            return null;
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
