using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Backlogs.Logging;
using Logger = Backlogs.Logging.Logger;
using Microsoft.Graph;
using Windows.Security.Authentication.Web;
using System.Net.Http.Headers;
using Windows.Storage;
using System.IO;
using Backlogs.Utils;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using System.Threading;
using Windows.UI.Core;
using System.Diagnostics;
using Backlogs.Saving;

namespace Backlogs.Auth
{
    public class MSAL
    {
        private const string ClientId = "7ff23ca6-8307-4cd1-8930-ce4ebd0418b7";
        private static readonly string MSGraphURL = "https://graph.microsoft.com/v1.0/";
        private static GraphServiceClient graphServiceClient = null;
        private static IPublicClientApplication PublicClientApplication;

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

            // the redirect uri you need to register
            string redirectUri = $"ms-appx-web://microsoft.aad.brokerplugin/S-1-15-2-4253267031-4257092517-3713903359-2216172251-2905670859-2851875097-3927788056";

            AuthenticationResult authResult;

            PublicClientApplication = PublicClientApplicationBuilder.Create(ClientId)
                            .WithBroker(true)
                            .WithRedirectUri(redirectUri)
                            .Build();
            var accounts = await PublicClientApplication.GetAccountsAsync();
            var accountToLogin = accounts.FirstOrDefault();
            try
            {
                // 4. AcquireTokenSilent 
                authResult = await PublicClientApplication.AcquireTokenSilent(scopes, accountToLogin)
                                          .ExecuteAsync();
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

        private async static Task<GraphServiceClient> SignInAndInitializeGraphServiceClient()
        {
            GraphServiceClient graphClient = new GraphServiceClient(MSGraphURL,
            new DelegateAuthenticationProvider(async (requestMessage) => {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", await SignInAndGetAuthResult());
            }));

            return await Task.FromResult(graphClient);
        }

        /// <summary>
        /// Returns the service client, and signs the user in if they haven't
        /// </summary>
        /// <returns></returns>
        public static async Task<GraphServiceClient> GetGraphServiceClient()
        {
            if (graphServiceClient == null)
            {
                graphServiceClient = await SignInAndInitializeGraphServiceClient().ConfigureAwait(false);
                try
                {
                    await Logger.Info("Fetching graph service client.....");
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
                    }
                }
                catch (Exception ex)
                {
                    await Logger.Error("Failed to sign-in user or get user photo and name", ex);
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
                await PublicClientApplication.RemoveAsync(firstAccount).ConfigureAwait(false);
                Settings.IsSignedIn = false;
                try
                {
                    await SaveData.GetInstance().DeleteLocalFileAsync();
                }
                catch
                {
                    // : )
                }
            }
            catch (Exception ex)
            {
                await Logger.Error("Failed to sign out user.", ex);
            }
        }

    }
}
