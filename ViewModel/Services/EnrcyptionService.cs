using System;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace HoudiniSafe.ViewModel.Services
{
    /// <summary>
    /// Provides encryption and decryption services for files and folders.
    /// </summary>
    public class EncryptionService
    {
        #region Constants

        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 100000;
        private const int BufferSize = 81920; // 80 KB
        private const string EncryptedExtension = ".enc";

        #endregion

        #region Public Methods

        /// <summary>
        /// Encrypts a file asynchronously.
        /// </summary>
        /// <param name="inputFile">Path to the input file.</param>
        /// <param name="outputFolder">Folder to save the encrypted file.</param>
        /// <param name="password">Encryption password.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="replaceOriginal">Whether to replace the original file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EncryptFileAsync(string inputFile, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempOutputFile = Path.GetTempFileName();
            string inputFileName = Path.GetFileName(inputFile);
            string finalOutputFile = Path.Combine(outputFolder, inputFileName + EncryptedExtension);

            try
            {
                byte[] salt = GenerateRandomBytes(32);
                byte[] iv = GenerateRandomBytes(16);

                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = deriveBytes.GetBytes(KeySize / 8);

                using var aes = CreateAesInstance();
                using var inputFileStream = CreateFileStream(inputFile, FileMode.Open, FileAccess.Read);
                using var outputFileStream = CreateFileStream(tempOutputFile, FileMode.Create, FileAccess.Write);

                await WriteEncryptionHeaderAsync(outputFileStream, salt, iv, inputFileName);

                using var encryptor = aes.CreateEncryptor(key, iv);
                using var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write);

                await EncryptContentAsync(inputFileStream, cryptoStream, progress);
            }
            catch (Exception ex)
            {
                File.Delete(tempOutputFile);
                throw new Exception($"Encryption error: {ex.Message}", ex);
            }

            FinalizeEncryption(inputFile, finalOutputFile, tempOutputFile, replaceOriginal);
        }

        /// <summary>
        /// Decrypts a file asynchronously.
        /// </summary>
        /// <param name="inputFile">Path to the encrypted file.</param>
        /// <param name="outputFolder">Folder to save the decrypted file.</param>
        /// <param name="password">Decryption password.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="replaceOriginal">Whether to replace the original encrypted file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DecryptFileAsync(string inputFile, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempOutputFile = Path.GetTempFileName();
            string finalOutputFile;

            try
            {
                using var inputFileStream = CreateFileStream(inputFile, FileMode.Open, FileAccess.Read);

                // Read salt and IV
                byte[] salt = new byte[32];
                byte[] iv = new byte[16];
                await inputFileStream.ReadAsync(salt, 0, salt.Length);
                await inputFileStream.ReadAsync(iv, 0, iv.Length);

                // Read metadata
                string metadata = await ReadMetadataAsync(inputFileStream);
                string[] parts = metadata.Split('|');
                if (parts.Length != 2)
                {
                    throw new Exception("Invalid metadata format.");
                }
                string originalFilename = parts[0];
                finalOutputFile = Path.Combine(outputFolder, originalFilename);

                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = deriveBytes.GetBytes(KeySize / 8);

                using var aes = CreateAesInstance();
                using var decryptor = aes.CreateDecryptor(key, iv);
                using var cryptoStream = new CryptoStream(inputFileStream, decryptor, CryptoStreamMode.Read);
                using var outputFileStream = CreateFileStream(tempOutputFile, FileMode.Create, FileAccess.Write);

                await DecryptContentAsync(cryptoStream, outputFileStream, inputFile, progress);
            }
            catch (CryptographicException ex)
            {
                File.Delete(tempOutputFile);
                throw new Exception("Decryption error: Incorrect password or corrupted file.", ex);
            }
            catch (Exception ex)
            {
                File.Delete(tempOutputFile);
                throw new Exception($"Decryption error: {ex.Message}", ex);
            }

            FinalizeDecryption(inputFile, finalOutputFile, tempOutputFile, replaceOriginal);

            await HandleZipFileAsync(finalOutputFile, progress);
        }

        /// <summary>
        /// Encrypts a folder asynchronously.
        /// </summary>
        /// <param name="inputFolder">Path to the input folder.</param>
        /// <param name="outputFolder">Folder to save the encrypted file.</param>
        /// <param name="password">Encryption password.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="replaceOriginal">Whether to replace the original folder.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EncryptFolderAsync(string inputFolder, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"The folder {inputFolder} was not found.");
            }

            string folderName = new DirectoryInfo(inputFolder).Name;
            string tempZipFile = Path.Combine(Path.GetTempPath(), $"{folderName}.zip");
            string finalOutputFile = Path.Combine(outputFolder, $"{folderName}.zip{EncryptedExtension}");

            try
            {
                // Step 1: Compress folder to ZIP file
                ZipFile.CreateFromDirectory(inputFolder, tempZipFile);
                progress?.Report(0.5); // 50% progress after zipping

                // Step 2: Encrypt ZIP file
                var encryptProgress = new Progress<double>(p => progress?.Report(0.5 + p * 0.5)); // Remaining 50% for encryption
                await EncryptFileAsync(tempZipFile, outputFolder, password, encryptProgress, false);

                if (replaceOriginal)
                {
                    Directory.Delete(inputFolder, true);
                }
            }
            finally
            {
                // Cleanup: Delete temporary ZIP file
                if (File.Exists(tempZipFile))
                {
                    File.Delete(tempZipFile);
                }
            }
        }

        /// <summary>
        /// Encrypts multiple files asynchronously.
        /// </summary>
        /// <param name="inputFiles">Array of input file paths.</param>
        /// <param name="outputFolder">Folder to save the encrypted files.</param>
        /// <param name="password">Encryption password.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="replaceOriginal">Whether to replace the original files.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EncryptMultipleFilesAsync(string[] inputFiles, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            int totalFiles = inputFiles.Length;
            int processedFiles = 0;

            foreach (string file in inputFiles)
            {
                if (!File.Exists(file))
                {
                    throw new FileNotFoundException($"The file {file} was not found.");
                }

                var fileProgress = new Progress<double>(p =>
                {
                    double overallProgress = (processedFiles + p) / totalFiles;
                    progress?.Report(overallProgress);
                });

                await EncryptFileAsync(file, outputFolder, password, fileProgress, replaceOriginal);

                processedFiles++;
            }

            if (replaceOriginal)
            {
                foreach (string file in inputFiles)
                {
                    File.Delete(file);
                }
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generates random bytes for cryptographic operations.
        /// </summary>
        /// <param name="length">Number of bytes to generate.</param>
        /// <returns>Array of random bytes.</returns>
        private byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// Creates an instance of AES with predefined settings.
        /// </summary>
        /// <returns>Configured AES instance.</returns>
        private Aes CreateAesInstance()
        {
            var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        /// <summary>
        /// Creates a FileStream with specified parameters.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="mode">File mode.</param>
        /// <param name="access">File access.</param>
        /// <returns>Configured FileStream.</returns>
        private FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileStream(path, mode, access, FileShare.None, BufferSize, true);
        }

        /// <summary>
        /// Writes encryption header to the output stream.
        /// </summary>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="salt">Salt bytes.</param>
        /// <param name="iv">IV bytes.</param>
        /// <param name="fileName">Original file name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task WriteEncryptionHeaderAsync(Stream outputStream, byte[] salt, byte[] iv, string fileName)
        {
            await outputStream.WriteAsync(salt, 0, salt.Length);
            await outputStream.WriteAsync(iv, 0, iv.Length);

            string metadata = $"{fileName}|{Path.GetExtension(fileName)}";
            byte[] metadataBytes = Encoding.UTF8.GetBytes(metadata);
            await outputStream.WriteAsync(BitConverter.GetBytes(metadataBytes.Length), 0, sizeof(int));
            await outputStream.WriteAsync(metadataBytes, 0, metadataBytes.Length);
        }

        /// <summary>
        /// Encrypts the content of the input stream.
        /// </summary>
        /// <param name="inputStream">Input stream.</param>
        /// <param name="cryptoStream">Crypto stream for encryption.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task EncryptContentAsync(Stream inputStream, CryptoStream cryptoStream, IProgress<double> progress)
        {
            long totalBytes = inputStream.Length;
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await cryptoStream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
                progress?.Report((double)totalBytesRead / totalBytes);
            }

            await cryptoStream.FlushFinalBlockAsync();
        }

        /// <summary>
        /// Finalizes the encryption process.
        /// </summary>
        /// <param name="inputFile">Input file path.</param>
        /// <param name="finalOutputFile">Final output file path.</param>
        /// <param name="tempOutputFile">Temporary output file path.</param>
        /// <param name="replaceOriginal">Whether to replace the original file.</param>
        private void FinalizeEncryption(string inputFile, string finalOutputFile, string tempOutputFile, bool replaceOriginal)
        {
            if (replaceOriginal)
            {
                File.Delete(inputFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(finalOutputFile));
            File.Move(tempOutputFile, finalOutputFile, true);
        }

        /// <summary>
        /// Prepares for decryption by reading the encryption header.
        /// </summary>
        /// <param name="inputFile">Input file path.</param>
        /// <param name="outputFolder">Output folder path.</param>
        /// <param name="password">Decryption password.</param>
        /// <returns>A tuple containing the final output file path, key, and IV.</returns>
        private async Task<(string finalOutputFile, byte[] key, byte[] iv)> PrepareDecryptionAsync(string inputFile, string outputFolder, string password)
        {
            using var inputFileStream = CreateFileStream(inputFile, FileMode.Open, FileAccess.Read);

            byte[] salt = new byte[32];
            byte[] iv = new byte[16];
            await inputFileStream.ReadAsync(salt, 0, salt.Length);
            await inputFileStream.ReadAsync(iv, 0, iv.Length);

            string metadata = await ReadMetadataAsync(inputFileStream);
            string[] parts = metadata.Split('|');
            if (parts.Length != 2)
            {
                throw new Exception("Invalid metadata format.");
            }

            string originalFilename = parts[0];
            string finalOutputFile = Path.Combine(outputFolder, originalFilename);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(KeySize / 8);

            return (finalOutputFile, key, iv);
        }

        /// <summary>
        /// Reads metadata from the input stream.
        /// </summary>
        /// <param name="inputStream">Input stream.</param>
        /// <returns>Metadata string.</returns>
        private async Task<string> ReadMetadataAsync(Stream inputStream)
        {
            byte[] metadataLengthBytes = new byte[sizeof(int)];
            await inputStream.ReadAsync(metadataLengthBytes, 0, sizeof(int));
            int metadataLength = BitConverter.ToInt32(metadataLengthBytes, 0);
            byte[] metadataBytes = new byte[metadataLength];
            await inputStream.ReadAsync(metadataBytes, 0, metadataLength);
            return Encoding.UTF8.GetString(metadataBytes);
        }

        /// <summary>
        /// Decrypts the content of the input stream.
        /// </summary>
        /// <param name="cryptoStream">Crypto stream for decryption.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="inputFile">Input file path.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DecryptContentAsync(CryptoStream cryptoStream, Stream outputStream, string inputFile, IProgress<double> progress)
        {
            long totalBytes = new FileInfo(inputFile).Length - 32 - 16 - sizeof(int) - 4; // Adjust for salt, IV, and metadata
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await cryptoStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
                progress?.Report((double)totalBytesRead / totalBytes);
            }
        }

        /// <summary>
        /// Finalizes the decryption process.
        /// </summary>
        /// <param name="inputFile">Input file path.</param>
        /// <param name="finalOutputFile">Final output file path.</param>
        /// <param name="tempOutputFile">Temporary output file path.</param>
        /// <param name="replaceOriginal">Whether to replace the original encrypted file.</param>
        private void FinalizeDecryption(string inputFile, string finalOutputFile, string tempOutputFile, bool replaceOriginal)
        {
            if (replaceOriginal)
            {
                File.Delete(inputFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(finalOutputFile));
            File.Move(tempOutputFile, finalOutputFile, true);
        }

        /// <summary>
        /// Handles the case where the decrypted file is a ZIP file.
        /// </summary>
        /// <param name="filePath">Path to the potentially ZIP file.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleZipFileAsync(string filePath, IProgress<double> progress)
        {
            if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                string extractPath = Path.Combine(
                    Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath)
                );
                Directory.CreateDirectory(extractPath);

                try
                {
                    ZipFile.ExtractToDirectory(filePath, extractPath, true);
                    progress?.Report(1.0); // 100% complete

                    // Delete the ZIP file after extraction
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // If extraction fails, we keep the ZIP file and report the error
                    throw new Exception($"The file was successfully decrypted, but extraction failed: {ex.Message}");
                }
            }
            else
            {
                progress?.Report(1.0); // 100% complete for non-ZIP files
            }
        }

        /// <summary>
        /// Copies a directory and its contents to a new location.
        /// </summary>
        /// <param name="sourceDir">Source directory path.</param>
        /// <param name="destinationDir">Destination directory path.</param>
        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Copy files to the destination directory
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Copy subdirectories recursively
            foreach (string subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subdir));
                CopyDirectory(subdir, destSubDir);
            }
        }

        /// <summary>
        /// Generates a random salt for cryptographic operations.
        /// </summary>
        /// <returns>A byte array containing the random salt.</returns>
        private byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        #endregion
    }
}