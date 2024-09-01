// MainViewViewModel.cs
using HoudiniSafe.ViewModel.Services;
using HoudiniSafe.ViewModel.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using HoudiniSafe.View;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace HoudiniSafe.ViewModel
{
    /// <summary>
    /// ViewModel for the main view, handling file encryption and decryption operations.
    /// </summary>
    public class MainViewViewModel : ViewModelBase
    {
        #region Fields

        // Service used for encryption and decryption operations
        private readonly EncryptionService _encryptionService;

        // Service used for showing dialogs
        private readonly DialogService _dialogService;

        // Collection of files or folders that have been dropped into the view
        private ObservableCollection<string> _droppedFiles;

        // Progress value for the encryption or decryption process
        private double _progressValue;

        // Visibility of the progress bar
        private Visibility _progressVisibility = Visibility.Collapsed;

        // Whether to replace the original file or not
        private bool _replaceOriginal;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the collection of dropped files.
        /// </summary>
        public ObservableCollection<string> DroppedFiles
        {
            get => _droppedFiles;
            set
            {
                if (SetProperty(ref _droppedFiles, value))
                {
                    // Update dependent properties when DroppedFiles changes
                    OnPropertyChanged(nameof(CanEncrypt));
                    OnPropertyChanged(nameof(CanDecrypt));
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress value for the encryption or decryption process.
        /// </summary>
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the progress bar.
        /// </summary>
        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set => SetProperty(ref _progressVisibility, value);
        }

        /// <summary>
        /// Gets or sets whether to replace the original file or not.
        /// </summary>
        public bool ReplaceOriginal
        {
            get => _replaceOriginal;
            set => SetProperty(ref _replaceOriginal, value);
        }

        /// <summary>
        /// Command for opening files.
        /// </summary>
        public ICommand OpenFileCommand { get; }

        /// <summary>
        /// Command for exiting the application.
        /// </summary>
        public ICommand ExitCommand { get; }

        /// <summary>
        /// Command for showing the About dialog.
        /// </summary>
        public ICommand AboutCommand { get; }

        /// <summary>
        /// Command for handling file drop operations.
        /// </summary>
        public ICommand DropCommand { get; }

        /// <summary>
        /// Command for encrypting files.
        /// </summary>
        public ICommand EncryptCommand { get; }

        /// <summary>
        /// Command for decrypting files.
        /// </summary>
        public ICommand DecryptCommand { get; }

        /// <summary>
        /// Command for removing a specific file from the list.
        /// </summary>
        public ICommand RemoveFileCommand { get; }

        /// <summary>
        /// Command for removing all files from the list.
        /// </summary>
        public ICommand RemoveAllFilesCommand { get; }

        /// <summary>
        /// Determines if files can be encrypted based on the state of DroppedFiles.
        /// </summary>
        public bool CanEncrypt => DroppedFiles != null && DroppedFiles.Count > 0 && DroppedFiles.All(file => !file.EndsWith(".enc"));

        /// <summary>
        /// Determines if files can be decrypted based on the state of DroppedFiles.
        /// </summary>
        public bool CanDecrypt => DroppedFiles != null && DroppedFiles.Count == 1 && DroppedFiles[0].EndsWith(".enc");

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewViewModel class.
        /// </summary>
        public MainViewViewModel()
        {
            _encryptionService = new EncryptionService();
            _dialogService = new DialogService();
            DroppedFiles = new ObservableCollection<string>();

            // Initialize commands
            RemoveFileCommand = new RelayCommand<string>(RemoveFile);
            RemoveAllFilesCommand = new RelayCommand(RemoveAllFiles);
            OpenFileCommand = new RelayCommand(OpenFile);
            ExitCommand = new RelayCommand(Exit);
            AboutCommand = new RelayCommand(ShowAbout);
            DropCommand = new RelayCommand<DragEventArgs>(HandleDrop);
            EncryptCommand = new AsyncRelayCommand(EncryptAsync);
            DecryptCommand = new AsyncRelayCommand(DecryptAsync);
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// Opens a file dialog to select a file and adds it to DroppedFiles.
        /// </summary>
        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                AddDroppedFile(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void Exit() => Application.Current.Shutdown();

        /// <summary>
        /// Shows the About dialog.
        /// </summary>
        private void ShowAbout() => _dialogService.ShowPopup("HoudiniSafe - Sicheres Verschlüsselungstool", "Über");

        /// <summary>
        /// Handles file drop operations and adds dropped files to DroppedFiles.
        /// </summary>
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

        /// <summary>
        /// Adds a file path to the DroppedFiles collection if it is not already present.
        /// </summary>
        /// <param name="filePath">The path of the file to add.</param>
        public void AddDroppedFile(string filePath)
        {
            if (!DroppedFiles.Contains(filePath))
            {
                DroppedFiles.Add(filePath);
                OnPropertyChanged(nameof(CanEncrypt));
                OnPropertyChanged(nameof(CanDecrypt));
            }
        }

        /// <summary>
        /// Removes a specific file from the DroppedFiles collection.
        /// </summary>
        /// <param name="file">The file path to remove.</param>
        private void RemoveFile(string file)
        {
            if (DroppedFiles.Contains(file))
            {
                DroppedFiles.Remove(file);
                OnPropertyChanged(nameof(CanEncrypt));
                OnPropertyChanged(nameof(CanDecrypt));
            }
        }

        /// <summary>
        /// Removes all files from the DroppedFiles collection.
        /// </summary>
        private void RemoveAllFiles()
        {
            DroppedFiles.Clear();
            OnPropertyChanged(nameof(CanEncrypt));
            OnPropertyChanged(nameof(CanDecrypt));
        }

        #endregion

        #region Encryption/Decryption

        /// <summary>
        /// Starts the encryption process asynchronously.
        /// </summary>
        private async Task EncryptAsync()
        {
            if (!CanEncryptFiles()) return;

            string password = ShowPasswordDialog("Passwort für Verschlüsselung eingeben");
            if (string.IsNullOrEmpty(password)) return;

            string outputFile = GetOutputFilePath();
            if (string.IsNullOrEmpty(outputFile)) return;

            await PerformEncryptionAsync(password, outputFile);
        }

        /// <summary>
        /// Checks if there are files available for encryption.
        /// </summary>
        /// <returns>True if files can be encrypted; otherwise, false.</returns>
        private bool CanEncryptFiles()
        {
            if (DroppedFiles.Count == 0)
            {
                _dialogService.ShowPopup("Bitte wählen Sie mindestens eine Datei oder einen Ordner aus.", "Fehler");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Performs the encryption process and updates the progress.
        /// </summary>
        /// <param name="password">The password for encryption.</param>
        /// <param name="outputFile">The path where the encrypted file will be saved.</param>
        private async Task PerformEncryptionAsync(string password, string outputFile)
        {
            ProgressVisibility = Visibility.Visible;
            ProgressValue = 0;
            var progress = new Progress<double>(value => ProgressValue = value * 100);

            try
            {
                await EncryptFilesAsync(password, outputFile, progress);
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

        /// <summary>
        /// Encrypts files or folders based on the DroppedFiles collection.
        /// </summary>
        /// <param name="password">The password for encryption.</param>
        /// <param name="outputFile">The path where the encrypted file will be saved.</param>
        /// <param name="progress">Progress reporting for the encryption process.</param>
        private async Task EncryptFilesAsync(string password, string outputFile, IProgress<double> progress)
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
        }

        /// <summary>
        /// Starts the decryption process asynchronously.
        /// </summary>
        private async Task DecryptAsync()
        {
            if (!CanDecryptFiles()) return;

            string password = ShowPasswordDialog("Passwort für Entschlüsselung eingeben");
            if (string.IsNullOrEmpty(password)) return;

            string outputFile = GetOutputFilePath();
            if (string.IsNullOrEmpty(outputFile)) return;

            await PerformDecryptionAsync(password, outputFile);
        }

        /// <summary>
        /// Checks if the files are valid for decryption.
        /// </summary>
        /// <returns>True if exactly one file is selected for decryption; otherwise, false.</returns>
        private bool CanDecryptFiles()
        {
            if (DroppedFiles.Count != 1)
            {
                _dialogService.ShowPopup("Bitte wählen Sie genau eine verschlüsselte Datei aus.", "Fehler");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Performs the decryption process and updates the progress.
        /// </summary>
        /// <param name="password">The password for decryption.</param>
        /// <param name="outputFile">The path where the decrypted file will be saved.</param>
        private async Task PerformDecryptionAsync(string password, string outputFile)
        {
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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the output file path based on the ReplaceOriginal property.
        /// </summary>
        /// <returns>The path where the file will be saved.</returns>
        private string GetOutputFilePath()
        {
            return ReplaceOriginal ? DroppedFiles[0] : GetSaveFilePath();
        }

        /// <summary>
        /// Shows a folder picker dialog and returns the selected folder path.
        /// </summary>
        /// <returns>The path of the selected folder, or null if canceled.</returns>
        private string GetSaveFilePath()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Wählen Sie den Speicherort aus"
            };

            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
        }

        /// <summary>
        /// Shows a password input dialog and returns the entered password.
        /// </summary>
        /// <param name="title">The title of the password dialog.</param>
        /// <returns>The entered password, or null if canceled.</returns>
        private string ShowPasswordDialog(string title)
        {
            var viewModel = new PasswordDialogViewModel(title);
            var dialog = new PasswordDialogView
            {
                DataContext = viewModel
            };

            return dialog.ShowDialog() == true ? viewModel.Password : null;
        }

        #endregion
    }
}
