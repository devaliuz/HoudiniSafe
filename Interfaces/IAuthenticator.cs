//IAuthenticator.cs
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;

namespace HoudiniSafe.Interfaces
{
    public interface IAuthenticator
    {
        Task<UserCredential> AuthenticateAsync();
        DriveService CreateDriveService(UserCredential credential);
    }
}
