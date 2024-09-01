using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;

namespace HoudiniSafe.ViewModel.Services
{
    public class EncryptionService
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 100000;
        private const int BufferSize = 81920; // 80 KB
        private const string EncryptedExtension = ".enc";

        public async Task EncryptFileAsync(string inputFile, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempOutputFile = Path.GetTempFileName();
            string inputFileName = Path.GetFileName(inputFile);
            string finalOutputFile;

            if (replaceOriginal)
            {
                finalOutputFile = inputFile + EncryptedExtension;
            }
            else
            {
                finalOutputFile = Path.Combine(outputFolder, inputFileName + EncryptedExtension);
            }

            try
            {
                byte[] salt = GenerateRandomSalt();
                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;

                var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
                using var outputFileStream = new FileStream(tempOutputFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

                // Write salt
                await outputFileStream.WriteAsync(salt, 0, salt.Length);

                // Write metadata
                string originalFilename = Path.GetFileName(inputFile);
                string originalExtension = Path.GetExtension(inputFile);
                string metadata = $"{originalFilename}|{originalExtension}";
                byte[] metadataBytes = Encoding.UTF8.GetBytes(metadata);
                await outputFileStream.WriteAsync(BitConverter.GetBytes(metadataBytes.Length), 0, sizeof(int));
                await outputFileStream.WriteAsync(metadataBytes, 0, metadataBytes.Length);

                using var cryptoStream = new CryptoStream(outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

                long totalBytes = new FileInfo(inputFile).Length;
                long bytesWritten = 0;
                byte[] buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = await inputFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await cryptoStream.WriteAsync(buffer, 0, bytesRead);
                    bytesWritten += bytesRead;
                    progress?.Report((double)bytesWritten / totalBytes);
                }

                await cryptoStream.FlushFinalBlockAsync();
            }
            catch
            {
                File.Delete(tempOutputFile);
                throw;
            }

            if (replaceOriginal)
            {
                File.Delete(inputFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(finalOutputFile));
            File.Move(tempOutputFile, finalOutputFile, true);
        }

        public async Task DecryptFileAsync(string inputFile, string outputFile, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempOutputFile = Path.GetTempFileName();
            string finalOutputFile;

            try
            {
                byte[] salt = new byte[32];
                using var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
                await inputFileStream.ReadAsync(salt, 0, salt.Length);

                // Read metadata
                byte[] metadataLengthBytes = new byte[sizeof(int)];
                await inputFileStream.ReadAsync(metadataLengthBytes, 0, sizeof(int));
                int metadataLength = BitConverter.ToInt32(metadataLengthBytes, 0);
                byte[] metadataBytes = new byte[metadataLength];
                await inputFileStream.ReadAsync(metadataBytes, 0, metadataLength);
                string metadata = Encoding.UTF8.GetString(metadataBytes);
                string[] parts = metadata.Split('|');
                string originalFilename = parts[0];
                string originalExtension = parts[1];

                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;

                var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                // Determine finalOutputFile without changing the name
                finalOutputFile = replaceOriginal
                    ? Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile))
                    : Path.Combine(outputFile, originalFilename);

                using var outputFileStream = new FileStream(tempOutputFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);
                using var cryptoStream = new CryptoStream(inputFileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

                long totalBytes = new FileInfo(inputFile).Length - salt.Length - sizeof(int) - metadataLength;
                long bytesRead = 0;
                byte[] buffer = new byte[BufferSize];
                int read;

                while ((read = await cryptoStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await outputFileStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;
                    progress?.Report((double)bytesRead / totalBytes);
                }
            }
            catch
            {
                File.Delete(tempOutputFile);
                throw;
            }

            if (replaceOriginal)
            {
                File.Delete(inputFile);
            }

            File.Move(tempOutputFile, finalOutputFile, true);
        }


        public async Task EncryptFolderAsync(string inputFolder, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Der Ordner {inputFolder} wurde nicht gefunden.");
            }

            string[] files = Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories);
            int totalFiles = files.Length;
            int processedFiles = 0;

            foreach (string file in files)
            {
                string relativePath = Path.GetRelativePath(inputFolder, file);
                string outputFile = Path.Combine(outputFolder, relativePath);
                string outputFileFolder = Path.GetDirectoryName(outputFile);

                var fileProgress = new Progress<double>(p =>
                {
                    double overallProgress = (processedFiles + p) / totalFiles;
                    progress?.Report(overallProgress);
                });

                await EncryptFileAsync(file, outputFileFolder, password, fileProgress, replaceOriginal);

                processedFiles++;
            }

            if (replaceOriginal)
            {
                Directory.Delete(inputFolder, true);
            }
        }

        public async Task EncryptMultipleFilesAsync(string[] inputFiles, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            int totalFiles = inputFiles.Length;
            int processedFiles = 0;

            foreach (string file in inputFiles)
            {
                if (!File.Exists(file))
                {
                    throw new FileNotFoundException($"Die Datei {file} wurde nicht gefunden.");
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

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // If copying subdirectories, copy them and their contents to new location
            foreach (string subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subdir));
                CopyDirectory(subdir, destSubDir);
            }
        }


        private byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}