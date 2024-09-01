using System;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace HoudiniSafe.Viewmodel.Services
{
    public class EncryptionService
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 100000;
        private const int BufferSize = 81920; // 80 KB
        private const string EncryptedExtension = ".enc";

        public async Task EncryptFileAsync(string inputFile, string outputFile, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempOutputFile = Path.GetTempFileName();
            string finalOutputFile = outputFile + EncryptedExtension;

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
                File.Move(tempOutputFile, finalOutputFile);
            }
            else
            {
                File.Move(tempOutputFile, finalOutputFile, true);
            }
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

                if (replaceOriginal)
                {
                    finalOutputFile = Path.Combine(
                        Path.GetDirectoryName(inputFile),
                        Path.GetFileNameWithoutExtension(inputFile));

                    // Remove .enc if it exists
                    if (finalOutputFile.EndsWith(EncryptedExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        finalOutputFile = finalOutputFile.Substring(0, finalOutputFile.Length - EncryptedExtension.Length);
                    }
                }
                else
                {
                    finalOutputFile = Path.Combine(Path.GetDirectoryName(outputFile),
                                        Path.GetFileNameWithoutExtension(originalFilename));
                }

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
                File.Move(tempOutputFile, finalOutputFile);
            }
            else
            {
                File.Move(tempOutputFile, finalOutputFile, true);
            }
        }

        public async Task EncryptFolderAsync(string inputFolder, string outputFile, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempDir = CreateUniqueTemporaryDirectory();
            string tempZipFile = Path.Combine(tempDir, "temp.zip");
            string finalOutputFile = outputFile + EncryptedExtension;

            try
            {
                await Task.Run(() => ZipFile.CreateFromDirectory(inputFolder, tempZipFile));

                var zipProgress = new Progress<double>(p => progress?.Report(0.5 + p * 0.5)); // 50% progress for encryption
                await EncryptFileAsync(tempZipFile, outputFile, password, zipProgress, false);
            }
            finally
            {
                // Aufräumen: Temporäre Dateien und Verzeichnisse löschen
                try
                {
                    if (File.Exists(tempZipFile))
                    {
                        File.Delete(tempZipFile);
                    }
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (IOException ex)
                {
                    // Log the error or handle it as appropriate for your application
                    Console.WriteLine($"Error cleaning up temporary files: {ex.Message}");
                }
            }

            if (replaceOriginal)
            {
                Directory.Delete(inputFolder, true);
                File.Move(finalOutputFile, inputFolder + EncryptedExtension);
            }
        }

        public async Task EncryptMultipleFilesAsync(string[] inputFiles, string outputFile, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempDir = CreateUniqueTemporaryDirectory();
            string tempZipFile = Path.Combine(tempDir, "temp.zip");
            string finalOutputFile = outputFile + EncryptedExtension;

            try
            {
                using (var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create))
                {
                    int fileCount = inputFiles.Length;
                    for (int i = 0; i < fileCount; i++)
                    {
                        archive.CreateEntryFromFile(inputFiles[i], Path.GetFileName(inputFiles[i]));
                        progress?.Report((double)i / fileCount * 0.5); // 50% progress for zipping
                    }
                }

                var zipProgress = new Progress<double>(p => progress?.Report(0.5 + p * 0.5)); // 50% progress for encryption
                await EncryptFileAsync(tempZipFile, outputFile, password, zipProgress, false);
            }
            finally
            {
                // Aufräumen: Temporäre Dateien und Verzeichnisse löschen
                try
                {
                    if (File.Exists(tempZipFile))
                    {
                        File.Delete(tempZipFile);
                    }
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (IOException ex)
                {
                    // Log the error or handle it as appropriate for your application
                    Console.WriteLine($"Error cleaning up temporary files: {ex.Message}");
                }
            }

            if (replaceOriginal)
            {
                foreach (string file in inputFiles)
                {
                    File.Delete(file);
                }
            }
        }

        private string CreateUniqueTemporaryDirectory()
        {
            string tempDir;
            do
            {
                tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            } while (Directory.Exists(tempDir));

            Directory.CreateDirectory(tempDir);
            return tempDir;
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