﻿// MainPage

using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using backlog.Models;
using backlog.Saving;
using backlog.Utils;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Net.NetworkInformation;
using Microsoft.Graph;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Globalization;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Animation;
using backlog.Logging;
using Windows.Storage;
using backlog.Auth;
using System.Collections.Generic;
using System.Diagnostics;


// backlog.Views namespace
namespace backlog.Views
{
    // MainPage class 
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Backlog> allBacklogs { get; set; }
        private ObservableCollection<Backlog> backlogs { get; set; }

        private ObservableCollection<Backlog> recentlyAdded { get; set; }
        private ObservableCollection <Backlog> recentlyCompleted { get; set; }

        ObservableCollection<Backlog> completedBacklogs;
        ObservableCollection<Backlog> incompleteBacklogs;

        private int backlogCount;
        private int completedBacklogsCount;
        private int incompleteBacklogsCount;
        private double completedPercent;

        GraphServiceClient graphServiceClient;

        bool isNetworkAvailable = false;
        bool signedIn;
        int backlogIndex = -1;
        bool sync = false;

        Guid randomBacklogId = new Guid();

        // MainPage
        public MainPage()
        {
            this.InitializeComponent();
            isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
            WelcomeText.Text = Settings.IsSignedIn ? $"Welcome to Backlogs, {Settings.UserName}!" : "Welcome to Backlogs, stranger!";
            Task.Run(async () => { await SaveData.GetInstance().ReadDataAsync(); }).Wait();
            recentlyAdded = new ObservableCollection<Backlog>();
            recentlyCompleted = new ObservableCollection<Backlog>();
            completedBacklogs = new ObservableCollection<Backlog>();
            incompleteBacklogs = new ObservableCollection<Backlog>();
            LoadBacklogs();
            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Disabled;

        }//MainPage



        /// <summary>
        /// Shows the teaching tips on fresh install
        /// </summary>
        
        // ShowTeachingTips
        private void ShowTeachingTips()
        {
            if(Settings.IsFirstRun)
            {
                NavigationTeachingTip.IsOpen = true;
                Settings.IsFirstRun = false;
            }
            if(!Settings.IsSignedIn)
            {
                if(TopAppBar.Visibility == Visibility.Visible)
                {
                    TopSigninTeachingTip.IsOpen=true;
                }
                else
                {
                    BottomSigninTeachingTip.IsOpen = true;
                }
            }
        }//ShowTeachingTips


        // OnNavigatedTo
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.Parameter != null && e.Parameter.ToString() != "")
            {
                if(e.Parameter.ToString() == "sync")
                {
                    sync = true;
                }
                else
                {
                    // for backward connected animation
                    backlogIndex = int.Parse(e.Parameter.ToString());
                }
            }
            ProgBar.Visibility = Visibility.Visible;
            signedIn = Settings.IsSignedIn;
            if (isNetworkAvailable && signedIn)
            {
                await Logger.Info("Signing in user....");
                graphServiceClient = await MSAL.GetGraphServiceClient();

                try
                {
                    await SetUserPhotoAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ex] Exception: " + ex.Message);
                }

                TopSigninButton.Visibility = Visibility.Collapsed;
                BottomSigninButton.Visibility = Visibility.Collapsed;
                TopProfileButton.Visibility = Visibility.Visible;
                BottomProfileButton.Visibility = Visibility.Visible;
                await SaveData.GetInstance().ReadDataAsync(sync);
                LoadBacklogs();
                BuildNotifactionQueue();
            }
            ShowTeachingTips();
            ProgBar.Visibility = Visibility.Collapsed;

        }//OnNavigatedTo


        // LoadBacklogs
        private void LoadBacklogs()
        {
            recentlyAdded.Clear();
            recentlyCompleted.Clear();
            completedBacklogs.Clear();
            incompleteBacklogs.Clear();
            completedBacklogsCount = 0;
            incompleteBacklogsCount = 0;
            completedPercent = 0.0f;
            backlogs = SaveData.GetInstance().GetBacklogs();
            if(backlogs != null && backlogs.Count > 0)
            {
                foreach (var backlog in backlogs)
                {
                    if (!backlog.IsComplete)
                    {
                        if (backlog.CreatedDate == "None" || backlog.CreatedDate == null)
                        {
                            backlog.CreatedDate = DateTimeOffset.MinValue.ToString("d", CultureInfo.InvariantCulture);
                        }
                        incompleteBacklogs.Add(backlog);
                    }
                    else
                    {
                        if (backlog.CompletedDate == null)
                        {
                            backlog.CompletedDate = DateTimeOffset.MinValue.ToString("d", CultureInfo.InvariantCulture);
                        }
                        completedBacklogs.Add(backlog);
                    }
                }
                foreach (var backlog in incompleteBacklogs.OrderByDescending(b => DateTimeOffset.Parse(b.CreatedDate, CultureInfo.InvariantCulture)).Skip(0).Take(6))
                {
                    recentlyAdded.Add(backlog);
                }
                foreach (var backlog in completedBacklogs.OrderByDescending(b => DateTimeOffset.Parse(b.CompletedDate, CultureInfo.InvariantCulture)).Skip(0).Take(6))
                {
                    recentlyCompleted.Add(backlog);
                }
                if (completedBacklogs.Count <= 0)
                {
                    EmptyCompletedText.Visibility = Visibility.Visible;
                    CompletedBacklogsGrid.Visibility = Visibility.Collapsed;
                }
                completedBacklogsCount = backlogs.Where(b => b.IsComplete).Count();
                incompleteBacklogsCount = backlogs.Where(b => !b.IsComplete).Count();
                backlogCount = backlogs.Count;
                completedPercent = (Convert.ToDouble(completedBacklogsCount) / backlogCount) * 100;
                GenerateRandomBacklog();
            }

            else
            {
                EmptyBackogsText.Visibility = Visibility.Visible;
                EmptySuggestionsText.Visibility = Visibility.Visible;
                EmptyCompletedText.Visibility = Visibility.Visible;
                AddedBacklogsGrid.Visibility = Visibility.Collapsed;
                CompletedBacklogsGrid.Visibility = Visibility.Collapsed;
                suggestionsGrid.Visibility = Visibility.Collapsed;
                InputPanel.Visibility = Visibility.Collapsed;
            }

        }//LoadBacklogs



        /// <summary>
        /// Set the user photo in the command bar
        /// </summary>
        /// <returns></returns>
        
        // SetUserPhotoAsync
        private async Task SetUserPhotoAsync()
        {
            await Logger.Info("Setting user photo....");
            Debug.WriteLine("[i] Setting user photo....");

            string userName = Settings.UserName;

            // TODO / RnD / TEMP
            if (userName == null)
            {
                userName = "ME";
            }

            TopProfileButton.Label = userName;
            BottomProfileButton.Label = userName;
            var cacheFolder = ApplicationData.Current.LocalCacheFolder;
            try
            {
                var accountPicFile = await cacheFolder.GetFileAsync("profile.png");
                using (IRandomAccessStream stream = await accountPicFile.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage image = new BitmapImage();
                    stream.Seek(0);
                    await image.SetSourceAsync(stream);
                    TopAccountPic.ProfilePicture = image;
                    BottomAccountPic.ProfilePicture = image;
                }
            }
            catch (Exception ex)
            {
                await Logger.Error("Error settings", ex);
                Debug.WriteLine("[ex] Error settings : " + ex.Message);
            }

        }//SetUserPhotoAsync


        /// <summary>
        /// Signs the user in if connected to the internet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        // SigninButton_Click
        private async void SigninButton_Click(object sender, RoutedEventArgs e)
        {
            ProgBar.Visibility = Visibility.Visible;
            TopSigninTeachingTip.IsOpen = false;
            BottomSigninTeachingTip.IsOpen = false;
            signedIn = Settings.IsSignedIn;
            if (isNetworkAvailable)
            {
                if (!signedIn)
                {
                     await SaveData.GetInstance().DeleteLocalFileAsync();
                    graphServiceClient = await MSAL.GetGraphServiceClient();
                    Settings.IsSignedIn = true;
                    Frame.Navigate(typeof(MainPage), "sync");
                }
            }
            else
            {
                ContentDialog contentDialog = new ContentDialog
                {
                    Title = "No Internet",
                    Content = "You need to be connected to sign-in",
                    CloseButtonText = "Ok"
                };
                _ = await contentDialog.ShowAsync();
            }
        }//


        /// <summary>
        /// Opens the Create page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        // CreateButton_Click
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Frame.Navigate(typeof(CreatePage), null, new SlideNavigationTransitionInfo() 
                { 
                    Effect = SlideNavigationTransitionEffect.FromBottom}
                );
            }
            catch
            {
                Frame.Navigate(typeof(CreatePage));
            }
        }//CreateButton_Click


        /// <summary>
        /// Opens the Setting page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }//SettingsButton_Click

        /// <summary>
        /// Launches the Store rating page for the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        // RateButton_Click
        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            var ratingUri = new Uri(@"ms-windows-store://review/?ProductId=9N2H8CM2KWVZ");
            await Windows.System.Launcher.LaunchUriAsync(ratingUri);
        }//RateButton_Click



        /// <summary>
        /// Build the notif queue based on whether backlogs have notif time
        /// </summary>

        // BuildNotifactionQueue
        private void BuildNotifactionQueue()
        {
            if(backlogs != null)
            {
                foreach (var b in new ObservableCollection<Backlog>(backlogs.OrderByDescending(b => b.TargetDate)))
                {
                    if (b.TargetDate != "None")
                    {
                        var savedNotifTime = Settings.GetNotifTime(b.id.ToString());
                        if (savedNotifTime == "" || savedNotifTime != b.NotifTime.ToString())
                        {
                            DateTimeOffset date = DateTimeOffset.Parse(b.TargetDate, CultureInfo.InvariantCulture).Add(b.NotifTime);
                            int result = DateTimeOffset.Compare(date, DateTimeOffset.Now);
                            if (result > 0)
                            {
                                var builder = new ToastContentBuilder()
                                .AddText($"Hey there!", hintMaxLines: 1)
                                .AddText($"You wanted to check out {b.Name} by {b.Director} today. Here's your reminder!", hintMaxLines: 2)
                                .AddHeroImage(new Uri(b.ImageURL));

                                ScheduledToastNotification toastNotification = 
                                    new ScheduledToastNotification(builder.GetXml(), date);

                                ToastNotificationManager.CreateToastNotifier().AddToSchedule(toastNotification);
                            }
                            Settings.SetNotifTime(b.id.ToString(), b.NotifTime.ToString());
                        }
                    }
                    bool showLiveTile = Settings.ShowLiveTile;
                    if (showLiveTile)
                        GenerateLiveTiles(b);
                }
            }
        }//BuildNotifactionQueue


        // GenerateLiveTiles
        private void GenerateLiveTiles(Backlog b)
        {
            var tileContent = new TileContent()
            {
                Visual = new TileVisual()
                {

                    TileMedium = new TileBinding()
                    {
                        Branding = TileBranding.Name,
                        DisplayName = "Backlogs",
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = b.ImageURL,
                            },
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = b.Name,
                                    HintWrap = true,
                                    HintMaxLines = 2
                                },
                                new AdaptiveText()
                                {
                                    Text = b.Type,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },
                                new AdaptiveText()
                                {
                                    Text = b.TargetDate,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    },
                    TileWide = new TileBinding()
                    {
                        Branding = TileBranding.NameAndLogo,
                        DisplayName = "Backlogs (Beta)",
                        Content = new TileBindingContentAdaptive()
                        {
                            PeekImage = new TilePeekImage()
                            {
                                Source = b.ImageURL
                            },
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = b.Name
                                },
                                new AdaptiveText()
                                {
                                    Text = $"{b.Type} - {b.Director}",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },
                                new AdaptiveText()
                                {
                                    Text = b.Description,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                }
                            }
                        }
                    },
                }
            };

            // Create the tile notification
            var tileNotif = new TileNotification(tileContent.GetXml());

            // And send the notification to the primary tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);

        }//GenerateLiveTiles


        // ShareButton_Click
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }//ShareButton_Click

        // DataTransferManager_DataRequested
        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.SetText("https://www.microsoft.com/store/apps/9N2H8CM2KWVZ");
            request.Data.Properties.Title = "https://www.microsoft.com/store/apps/9N2H8CM2KWVZ";
            request.Data.Properties.Description = "Share this app with your contacts";
        }//DataTransferManager_DataRequested


        // SupportButton_Click
        private async void SupportButton_Click(object sender, RoutedEventArgs e)
        {
            var ratingUri = new Uri(@"https://paypal.me/surya4822?locale.x=en_US");
            await Windows.System.Launcher.LaunchUriAsync(ratingUri);
        }//SupportButton_Click


        /// <summary>
        /// Sync backlogs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        // SyncButton_Click
        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), "sync");
        }//SyncButton_Click


        // CompletedBacklogsButton_Click
        private void CompletedBacklogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Frame.Navigate(typeof(CompletedBacklogsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight});
            }
            catch
            {
                Frame.Navigate(typeof(CompletedBacklogsPage));
            }

        }//CompletedBacklogsButton_Click end


        // BacklogsButton_Click
        private void BacklogsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BacklogsPage), "sync");

        }//BacklogsButton_Click


        // AddedBacklogsGrid_ItemClick
        private void AddedBacklogsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedBacklog = (Backlog)e.ClickedItem;
            AddedBacklogsGrid.PrepareConnectedAnimation("cover", selectedBacklog, "coverImage");

            Frame.Navigate(typeof(BacklogPage), selectedBacklog.id, new SuppressNavigationTransitionInfo());
        
        }//AddedBacklogsGrid_ItemClick


        // AddedBacklogsGrid_Loaded
        private async void AddedBacklogsGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (backlogIndex != -1)
            {
                ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("backAnimation");
                try
                {
                    await AddedBacklogsGrid.TryStartConnectedAnimationAsync(animation, backlogs[backlogIndex], "coverImage");
                }
                catch
                {
                    // : )
                }
            }
        }//AddedBacklogsGrid_Loaded

        private void AllAddedButton_Click(object sender, RoutedEventArgs e)
        {
            BacklogsButton_Click(sender, e);
        }


        // AllCompletedButton_Click
        private void AllCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            CompletedBacklogsButton_Click(sender, e);
        }//AllCompletedButton_Click


        // GoButton_Click
        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateRandomBacklog();
        }//GoButton_Click


        // GenerateRandomBacklog
        private async void GenerateRandomBacklog()
        {
            var type = TypeComoBox.SelectedItem.ToString();
            Random random = new Random();
            Backlog randomBacklog = new Backlog();
            bool error = false;
            switch(type.ToLower())
            {
                case "any":
                    randomBacklog = incompleteBacklogs[random.Next(0, incompleteBacklogsCount)];
                    break;
                case "film":
                    var filmBacklogs = new ObservableCollection<Backlog>(incompleteBacklogs.Where(b => b.Type == "Film"));
                    if(filmBacklogs.Count <= 0)
                    {
                        await ShowErrorMessage("Add more films to see suggestions");
                        error = true;
                        break;
                    }
                    randomBacklog = filmBacklogs[random.Next(0, filmBacklogs.Count)];
                    break;
                case "album":
                    var musicBacklogs = new ObservableCollection<Backlog>(incompleteBacklogs.Where(b => b.Type == "Album"));
                    if(musicBacklogs.Count <= 0)
                    {
                        await ShowErrorMessage("Add more albums to see suggestions");
                        error = true;
                        break;
                    }
                    randomBacklog = musicBacklogs[random.Next(0, musicBacklogs.Count)];
                    break;
                case "game":
                    var gameBacklogs = new ObservableCollection<Backlog>(incompleteBacklogs.Where(b => b.Type == "Game"));
                    if(gameBacklogs.Count <= 0)
                    {
                        await ShowErrorMessage("Add more games to see suggestions");
                        error = true;
                        break;
                    }    
                    randomBacklog = gameBacklogs[random.Next(0, gameBacklogs.Count)];
                    break;
                case "book":
                    var bookBacklogs = new ObservableCollection<Backlog>(incompleteBacklogs.Where(b => b.Type == "Book"));
                    if(bookBacklogs.Count <= 0)
                    {
                        await ShowErrorMessage("Add more books to see suggestions");
                        error = true;
                        break;
                    }
                    randomBacklog = bookBacklogs[random.Next(0, bookBacklogs.Count)];
                    break;
                case "tv":
                    var tvBacklogs = new ObservableCollection<Backlog>(incompleteBacklogs.Where(b => b.Type == "TV"));
                    if(tvBacklogs.Count <= 0)
                    {
                        await ShowErrorMessage("Add more series to see suggestions");
                        error = true;
                        break;
                    }
                    randomBacklog = tvBacklogs[random.Next(0, tvBacklogs.Count)];
                    break;
            }
            if (!error)
            {
                RunName.Text = randomBacklog.Name;
                suggestionCover.Source = new BitmapImage(new Uri(randomBacklog.ImageURL));
                randomBacklogId = randomBacklog.id;
            }

        }//GenerateRandomBacklog


        // Hyperlink_Click
        private void Hyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            Frame.Navigate(typeof(BacklogPage), randomBacklogId, null);
        }//Hyperlink_Click


        // ShowErrorMessage
        private async Task ShowErrorMessage(string message)
        {
            ContentDialog contentDialog = new ContentDialog()
            {
                Title = "Not enough Backlogs",
                Content = message,
                CloseButtonText = "Ok"
            };

            await contentDialog.ShowAsync();

        }//ShowErrorMessage


        // ImportButton_Click
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".bklg");

            StorageFile file = await picker.PickSingleFileAsync();
            
            // hadle Cancel mode 
            if (file != null)
            {
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                await tempFolder.CreateFileAsync(file.Name, CreationCollisionOption.ReplaceExisting);
                string json = await FileIO.ReadTextAsync(file);
                var stFile = await tempFolder.GetFileAsync(file.Name);
                await FileIO.WriteTextAsync(stFile, json);

                Frame.Navigate(typeof(ImportBacklog), stFile.Name, null);
            }
                        

        }//ImportButton_Click

    }//class end

}//namespace end
