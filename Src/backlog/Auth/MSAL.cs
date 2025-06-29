// MSAL

using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using backlog.Logging;
using Logger = backlog.Logging.Logger;
using Microsoft.Graph;
using Windows.Security.Authentication.Web;
using System.Net.Http.Headers;
using Windows.Storage;
using System.IO;
using backlog.Utils;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using System.Threading;
using Windows.UI.Core;
using System.Diagnostics;
using backlog.Saving;

namespace backlog.Auth
{
    public class MSAL
    {
        private const string ClientId = "4a1aa1d5-c567-49d0-ad0b-cd957a47f842"; //"c81b068d-ab10-4c00-a24d-08c3a1a6b7c6";
        private static readonly string MSGraphURL = "https://graph.microsoft.com/v1.0/";
        private static GraphServiceClient graphServiceClient = null;
        
        private static IPublicClientApplication PublicClientApplication;
        private static AuthenticationResult authResult;

        private const string Tenant = "common"; // Alternatively "[Enter your tenant, as obtained from the azure portal, e.g. kko365.onmicrosoft.com]"
        private const string Authority = "https://login.microsoftonline.com/" + Tenant;

        private static string[] scopes = new string[]
        {
             "user.read",
             "Files.Read",
             "Files.Read.All",
             "Files.ReadWrite",
             "Files.ReadWrite.All"
        };

        static StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
        static string accountPicFile = "profile.png";

        public async static Task<string> SignInAndGetAuthResult()
        {
            string sid = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper();
            //string sid = "S-1-15-2-2566872105-1906516075-403359635-2971900813-1913047554-2806970718-2761120688"; 

            // the redirect uri you need to register
            string redirectUri =
               // $"ms-appx-web://microsoft.aad.brokerplugin/S-1-15-2-2566872105-1906516075-403359635-2971900813-1913047554-2806970718-2761120688";
               $"{Authority}";

            AuthenticationResult authResult;

            PublicClientApplication = PublicClientApplicationBuilder.Create(ClientId)
                            .WithBroker(true)
                            .WithRedirectUri(redirectUri)
                            .Build();

            IEnumerable<IAccount> accounts = await PublicClientApplication.GetAccountsAsync();
            IAccount accountToLogin = accounts.FirstOrDefault();
            
            try
            {

                // * AcquireTokenSilent case *
                authResult = await PublicClientApplication.AcquireTokenSilent(scopes, accountToLogin)
                                          .ExecuteAsync();

                // * AcquireTokenInteractive case *
                //authResult = await PublicClientApplication.AcquireTokenInteractive(scopes)
                //.WithAccount(accountToLogin)  // this already exists in MSAL, but it is more important for WAM
                //.ExecuteAsync();
            }
            catch (MsalUiRequiredException) // no change in the pattern
            {
                authResult = await PublicClientApplication.AcquireTokenInteractive(scopes)
                 .WithAccount(accountToLogin)  // this already exists in MSAL, but it is more important for WAM
                 .ExecuteAsync();
            }

            if(authResult != null)
            {
                Settings.IsSignedIn = true;
            }
            return authResult.AccessToken;
        }

        private async static Task<GraphServiceClient> SignInAndInitializeGraphServiceClient(string[] scopes)
        {
            /*
            GraphServiceClient graphClient = new GraphServiceClient
            (
                MSGraphURL,
                new DelegateAuthenticationProvider
                (
                    async (requestMessage) => 
                    {
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue
                        (
                            "bearer", 
                            await SignInAndGetAuthResult()
                        );
                    }
                )
            );

            return await Task.FromResult(graphClient);
            */

            GraphServiceClient graphClient = new GraphServiceClient
            (
                MSGraphURL,
                new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue
                    ("bearer", await SignInUserAndGetTokenUsingMSAL(scopes));
                }));

            return await Task.FromResult(graphClient);
        }


        /// <summary>
        /// Signs in the user and obtains an Access token for MS Graph
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns> Access Token</returns>
        private static async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
        {
            // Initialize the MSAL library by building a public client application
            PublicClientApplication = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority(Authority)
                .WithUseCorporateNetwork(false)
                .WithRedirectUri(DefaultRedirectUri.Value)
                 .WithLogging((level, message, containsPii) =>
                 {
                     Debug.WriteLine($"MSAL: {level} {message} ");
                 }, LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
                .Build();

            // It's good practice to not do work on the UI thread, so use ConfigureAwait(false) whenever possible.
            IEnumerable<IAccount> accounts = 
                await PublicClientApplication.GetAccountsAsync().ConfigureAwait(false);

            IAccount firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await PublicClientApplication.AcquireTokenSilent(scopes, firstAccount)
                                                  .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                authResult = await PublicClientApplication.AcquireTokenInteractive(scopes)
                                                  .ExecuteAsync()
                                                  .ConfigureAwait(false);

            }
            return authResult.AccessToken;
        }

        /// <summary>
        /// Returns the service client, and signs the user in if they haven't
        /// </summary>
        /// <returns></returns>
        public static async Task<GraphServiceClient> GetGraphServiceClient()
        {
            if (graphServiceClient == null)
            {
                graphServiceClient = await 
                    SignInAndInitializeGraphServiceClient(MSAL.scopes).ConfigureAwait(false);
                
                try
                {
                    await Logger.Info("Fetching graph service client.....");
                    Debug.WriteLine("[i] Fetching graph service client.....");

                    var user = await graphServiceClient.Me.Request().GetAsync();
                    Settings.UserName = user.GivenName;
                    try
                    {
                        Stream photoresponse = await graphServiceClient.Me.Photo.Content.Request().GetAsync();
                        if (photoresponse != null)
                        {
                            using (var randomAccessStream = photoresponse.AsRandomAccessStream())
                            {
                                BitmapImage image = new BitmapImage();
                                randomAccessStream.Seek(0);
                                await image.SetSourceAsync(randomAccessStream);

                                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                                var storageFile = await cacheFolder.CreateFileAsync(accountPicFile, CreationCollisionOption.ReplaceExisting);
                                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, await storageFile.OpenAsync(FileAccessMode.ReadWrite));
                                encoder.SetSoftwareBitmap(softwareBitmap);
                                await encoder.FlushAsync();
                            }
                        }
                    }
                    catch (ServiceException ex)
                    {
                        await Logger.Error("Failed to fetch user photo", ex);
                        Debug.WriteLine("[ex] Failed to fetch user photo: " + ex.Message);
                    }
                }
                catch (Exception ex2)
                {
                    await Logger.Error("Failed to sign-in user or get user photo and name", ex2);
                    Debug.WriteLine("[ex] Failed to sign-in user or get user photo and name" +
                        ex2.Message);
                }
            }
            return graphServiceClient;
        }

        /// <summary>
        /// Sign out of MSA
        /// </summary>
        /// <returns></returns>
        public static async Task SignOut()
        {
            var accounts = await PublicClientApplication.GetAccountsAsync();
            IAccount firstAccount = accounts.FirstOrDefault();
            try
            {
                await Logger.Info("Signing out user...");
                Debug.WriteLine("[i] Signing out user...");

                await PublicClientApplication.RemoveAsync(firstAccount).ConfigureAwait(false);
                
                Settings.IsSignedIn = false;
                
                try
                {
                    await SaveData.GetInstance().DeleteLocalFileAsync();
                }
                catch (Exception ex)
                {
                    await Logger.Error("Failed to DeleteLocalFileAsync.", ex);
                    Debug.WriteLine("[ex] Failed to DeleteLocalFileAsync: " + ex.Message);
                }
            }
            catch (Exception ex2)
            {
                await Logger.Error("Failed to sign out user.", ex2);
                Debug.WriteLine("[ex] Failed to sign out user: " + ex2.Message);
            }
        }

    }
}
