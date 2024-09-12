// IAuthenticator.cs
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;

namespace HoudiniSafe.Interfaces
{
    /// <summary>
    /// Interface defining methods for authenticating a user and creating a Google Drive service.
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>
        /// Authenticates the user asynchronously using OAuth 2.0.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains a <see cref="UserCredential"/> object for the authenticated user.
        /// </returns>
        Task<UserCredential> AuthenticateAsync();

        /// <summary>
        /// Creates a new instance of <see cref="DriveService"/> using the specified user credentials.
        /// </summary>
        /// <param name="credential">The <see cref="UserCredential"/> used to authenticate the user.</param>
        /// <returns>
        /// A new instance of <see cref="DriveService"/> configured with the provided credentials.
        /// </returns>
        DriveService CreateDriveService(UserCredential credential);
    }
}
