//GoogleAuthenticator.cs
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using HoudiniSafe.Interfaces;

namespace HoudiniSafe.Services
{
    public class GoogleAuthenticator : IAuthenticator
    {
        private static readonly string[] Scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveFile };

        public async Task<UserCredential> AuthenticateAsync()
        {
            try
            {
                using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
                {
                    var secretsJson = Google.Apis.Json.NewtonsoftJsonSerializer.Instance.Deserialize<dynamic>(stream);
                    var secrets = new ClientSecrets
                    {
                        ClientId = secretsJson.installed.client_id,
                        ClientSecret = secretsJson.installed.client_secret
                    };

                    string credPath = "token.json";
                    return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception StackTrace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        public DriveService CreateDriveService(UserCredential credential)
        {
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "HoudiniSafe",
            });
        }
    }
}
