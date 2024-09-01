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
            string finalOutputFile = Path.Combine(outputFolder, inputFileName + EncryptedExtension);

            try
            {
                byte[] salt = GenerateRandomBytes(32);
                byte[] iv = GenerateRandomBytes(16);

                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = deriveBytes.GetBytes(KeySize / 8);

                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
                using var outputFileStream = new FileStream(tempOutputFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

                // Write salt and IV
                await outputFileStream.WriteAsync(salt, 0, salt.Length);
                await outputFileStream.WriteAsync(iv, 0, iv.Length);

                // Write metadata
                string metadata = $"{inputFileName}|{Path.GetExtension(inputFile)}";
                byte[] metadataBytes = Encoding.UTF8.GetBytes(metadata);
                await outputFileStream.WriteAsync(BitConverter.GetBytes(metadataBytes.Length), 0, sizeof(int));
                await outputFileStream.WriteAsync(metadataBytes, 0, metadataBytes.Length);

                using var encryptor = aes.CreateEncryptor(key, iv);
                using var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write);

                long totalBytes = inputFileStream.Length;
                byte[] buffer = new byte[BufferSize];
                int bytesRead;
                long totalBytesRead = 0;

                while ((bytesRead = await inputFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await cryptoStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    progress?.Report((double)totalBytesRead / totalBytes);
                }

                await cryptoStream.FlushFinalBlockAsync();
            }
            catch (Exception ex)
            {
                File.Delete(tempOutputFile);
                throw new Exception($"Verschlüsselungsfehler: {ex.Message}", ex);
            }

            if (replaceOriginal)
            {
                File.Delete(inputFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(finalOutputFile));
            File.Move(tempOutputFile, finalOutputFile, true);
        }

        private byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }

        private byte[] EncryptMetadata(string metadata, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                byte[] plaintextBytes = Encoding.UTF8.GetBytes(metadata);

                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plaintextBytes, 0, plaintextBytes.Length);
                    cryptoStream.FlushFinalBlock();

                    return memoryStream.ToArray();
                }
            }
        }
        public async Task DecryptFileAsync(string inputFile, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            string tempOutputFile = Path.GetTempFileName();
            string finalOutputFile;
            try { 
                try
                {
                    using var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);

                    // Read salt and IV
                    byte[] salt = new byte[32];
                    byte[] iv = new byte[16];
                    await inputFileStream.ReadAsync(salt, 0, salt.Length);
                    await inputFileStream.ReadAsync(iv, 0, iv.Length);

                    // Read metadata
                    byte[] metadataLengthBytes = new byte[sizeof(int)];
                    await inputFileStream.ReadAsync(metadataLengthBytes, 0, sizeof(int));
                    int metadataLength = BitConverter.ToInt32(metadataLengthBytes, 0);
                    byte[] metadataBytes = new byte[metadataLength];
                    await inputFileStream.ReadAsync(metadataBytes, 0, metadataLength);
                    string metadata = Encoding.UTF8.GetString(metadataBytes);

                    string[] parts = metadata.Split('|');
                    if (parts.Length != 2)
                    {
                        throw new Exception("Ungültiges Metadatenformat.");
                    }
                    string originalFilename = parts[0];
                    string originalExtension = parts[1];

                    finalOutputFile = Path.Combine(outputFolder, originalFilename);

                    using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                    byte[] key = deriveBytes.GetBytes(KeySize / 8);

                    using var aes = Aes.Create();
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var decryptor = aes.CreateDecryptor(key, iv);
                    using var cryptoStream = new CryptoStream(inputFileStream, decryptor, CryptoStreamMode.Read);
                    using var outputFileStream = new FileStream(tempOutputFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

                    long totalBytes = new FileInfo(inputFile).Length - salt.Length - iv.Length - sizeof(int) - metadataLength;
                    byte[] buffer = new byte[BufferSize];
                    int bytesRead;
                    long totalBytesRead = 0;

                    while ((bytesRead = await cryptoStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await outputFileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        progress?.Report((double)totalBytesRead / totalBytes);
                    }
                }
                catch (CryptographicException ex)
                {
                    File.Delete(tempOutputFile);
                    throw new Exception("Entschlüsselungsfehler: Falsches Passwort oder beschädigte Datei.", ex);
                }
                catch (Exception ex)
                {
                    File.Delete(tempOutputFile);
                    throw new Exception($"Entschlüsselungsfehler: {ex.Message}", ex);
                }

                if (replaceOriginal)
                {
                    File.Delete(inputFile);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(finalOutputFile));
                File.Move(tempOutputFile, finalOutputFile, true);

                // Check if the decrypted file is a ZIP and extract if necessary
                if (Path.GetExtension(finalOutputFile).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string extractPath = Path.Combine(
                        Path.GetDirectoryName(finalOutputFile),
                        Path.GetFileNameWithoutExtension(finalOutputFile)
                    );
                    Directory.CreateDirectory(extractPath);

                    try
                    {
                        ZipFile.ExtractToDirectory(finalOutputFile, extractPath, true);
                        progress?.Report(1.0); // 100% complete

                        // Delete the ZIP file after extraction
                        File.Delete(finalOutputFile);
                    }
                    catch (Exception ex)
                    {
                        // If extraction fails, we keep the ZIP file and report the error
                        throw new Exception($"Die Datei wurde erfolgreich entschlüsselt, aber das Entpacken ist fehlgeschlagen: {ex.Message}");
                    }
                }
                else
                {
                    progress?.Report(1.0); // 100% complete for non-ZIP files
                } 
            }
            
            catch
            {
                if (File.Exists(tempOutputFile))
                {
                    File.Delete(tempOutputFile);
                }
                throw;
            }
        }


        public async Task EncryptFolderAsync(string inputFolder, string outputFolder, string password, IProgress<double> progress, bool replaceOriginal = false)
        {
            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Der Ordner {inputFolder} wurde nicht gefunden.");
            }

            string folderName = new DirectoryInfo(inputFolder).Name;
            string tempZipFile = Path.Combine(Path.GetTempPath(), $"{folderName}.zip");
            string finalOutputFile = Path.Combine(outputFolder, $"{folderName}.zip{EncryptedExtension}");

            try
            {
                // Schritt 1: Ordner zu ZIP-Datei komprimieren
                ZipFile.CreateFromDirectory(inputFolder, tempZipFile);
                progress?.Report(0.5); // 50% Fortschritt nach dem Zippen

                // Schritt 2: ZIP-Datei verschlüsseln
                var encryptProgress = new Progress<double>(p => progress?.Report(0.5 + p * 0.5)); // Restliche 50% für die Verschlüsselung
                await EncryptFileAsync(tempZipFile, outputFolder, password, encryptProgress, false);

                if (replaceOriginal)
                {
                    Directory.Delete(inputFolder, true);
                }
            }
            finally
            {
                // Aufräumen: Temporäre ZIP-Datei löschen
                if (File.Exists(tempZipFile))
                {
                    File.Delete(tempZipFile);
                }
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