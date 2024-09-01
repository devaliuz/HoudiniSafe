Sure, here’s a draft for a `README.md` file that introduces your project, describes its capabilities, and provides usage instructions. Feel free to adjust the content to better fit the specifics of your project.

---

# HoudiniSafe

**HoudiniSafe** is a secure file encryption and decryption tool designed to help users protect their sensitive files and folders. With a user-friendly interface and powerful encryption algorithms, HoudiniSafe ensures that your data remains confidential and secure.

## Features

- **Encryption**: Securely encrypt individual files or entire folders using advanced encryption techniques.
- **Decryption**: Decrypt previously encrypted files with ease, provided you have the correct password.
- **File Management**: Easily add, remove, or clear files from the list of items to be processed.
- **Progress Tracking**: Visual progress indicators to monitor the status of encryption or decryption operations.
- **Password Protection**: Prompts for passwords to ensure that only authorized users can perform encryption or decryption.
- **Folder Selection**: Allows users to select folders for encryption or decryption, providing flexibility in managing multiple files.

## Getting Started

To get started with HoudiniSafe, follow these instructions:

### Prerequisites

- .NET Framework 4.6.1 or later
- Visual Studio 2019 or later (for development and debugging)

### Installation

1. **Clone the Repository**

   ```sh
   git clone https://github.com/yourusername/houdinisafe.git
   ```

2. **Open the Project**

   Open the solution file (`HoudiniSafe.sln`) in Visual Studio.

3. **Build the Project**

   Build the solution using Visual Studio. This will compile the application and generate the executable file.

### Usage

1. **Running the Application**

   Run the application by starting the compiled executable or by pressing `F5` in Visual Studio.

2. **Adding Files**

   Use the "Open File" command to select files or folders for encryption or decryption. Drag and drop files directly onto the application window to add them to the list.

3. **Encrypting Files**

   - Select the files you want to encrypt.
   - Choose whether to replace the original file or save the encrypted file to a new location.
   - Enter a password when prompted.
   - Click the "Encrypt" button to start the encryption process.

4. **Decrypting Files**

   - Select the encrypted file you want to decrypt.
   - Enter the password used for encryption.
   - Choose the destination for the decrypted file.
   - Click the "Decrypt" button to start the decryption process.

5. **Removing Files**

   - To remove a specific file from the list, select it and click the "Remove File" button.
   - To clear all files from the list, click the "Remove All Files" button.

### Commands

- **Open File**: Opens a file dialog to select files or folders.
- **Encrypt**: Encrypts the selected files or folders.
- **Decrypt**: Decrypts the selected encrypted file.
- **Remove File**: Removes a specific file from the list.
- **Remove All Files**: Clears all files from the list.
- **Exit**: Closes the application.
- **About**: Displays information about the application.

## Acknowledgements

- [Microsoft .NET Framework](https://dotnet.microsoft.com/) - For providing the platform for development.
- [Visual Studio](https://visualstudio.microsoft.com/) - For development and debugging tools.
