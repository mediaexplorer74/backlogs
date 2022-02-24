﻿using Backlogs.Logging;
using Backlogs.Models;
using Backlogs.Saving;
using Backlogs.Utils;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Backlogs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CompletedBacklogsPage : Page
    {
        private ObservableCollection<Backlog> FinishedBacklogs;
        private ObservableCollection<Backlog> FinishedFilmBacklogs;
        private ObservableCollection<Backlog> FinishedTVBacklogs;
        private ObservableCollection<Backlog> FinishedMusicBacklogs;
        private ObservableCollection<Backlog> FinishedGameBacklogs;
        private ObservableCollection<Backlog> FinishedBookBacklogs;
        private ObservableCollection<Backlog> Backlogs;
        private Backlog SelectedBacklog;

        public CompletedBacklogsPage()
        {
            this.InitializeComponent();
            Task.Run(async () => { await SaveData.GetInstance().ReadDataAsync(); }).Wait();
            Backlogs = SaveData.GetInstance().GetBacklogs();
            FinishedBacklogs = new ObservableCollection<Backlog>(Backlogs.Where(b => b.IsComplete));
            FinishedBookBacklogs = new ObservableCollection<Backlog>(FinishedBacklogs.Where(b => b.Type == BacklogType.Book.ToString()));
            FinishedFilmBacklogs = new ObservableCollection<Backlog>(FinishedBacklogs.Where(b => b.Type == BacklogType.Film.ToString()));
            FinishedGameBacklogs = new ObservableCollection<Backlog>(FinishedBacklogs.Where(b => b.Type == BacklogType.Game.ToString()));
            FinishedMusicBacklogs = new ObservableCollection<Backlog>(FinishedBacklogs.Where(b => b.Type == BacklogType.Album.ToString()));
            FinishedTVBacklogs = new ObservableCollection<Backlog>(FinishedBacklogs.Where(b => b.Type == BacklogType.TV.ToString()));
            if (FinishedBacklogs.Count < 1)
            {
                EmptyText.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Collapsed; 
            }
            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            view.BackRequested += View_BackRequested;
        }

        private void View_BackRequested(object sender, BackRequestedEventArgs e)
        {
            PageStackEntry prevPage = Frame.BackStack.Last();
#if NETFX_CORE
            try
            {
                Frame.Navigate(prevPage?.SourcePageType, null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
            }
            catch
            {
                Frame.Navigate(prevPage?.SourcePageType);
            }
#else
            Frame.Navigate(prevPage?.SourcePageType);
#endif
            e.Handled = true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            foreach(var backlog in Backlogs)
            {
                if(backlog.id == SelectedBacklog.id)
                {
                    backlog.UserRating = PopupRating.Value;
                }
            }
            foreach (var backlog in FinishedBacklogs)
            {
                if (backlog.id == SelectedBacklog.id)
                {
                    backlog.UserRating = PopupRating.Value;
                }
            }
            SaveData.GetInstance().SaveSettings(Backlogs);
            await SaveData.GetInstance().WriteDataAsync(Settings.IsSignedIn);
            ProgRing.IsActive = false;
            CloseButton_Click(sender, e);
        }

        private async void IncompleteButton_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            foreach (var backlog in Backlogs)
            {
                if (backlog.id == SelectedBacklog.id)
                {
                    backlog.IsComplete = false;
                    backlog.CompletedDate = null;
                }
            }
            SaveData.GetInstance().SaveSettings(Backlogs);
            await SaveData.GetInstance().WriteDataAsync(Settings.IsSignedIn);
            PopupOverlay.Hide();
            Frame.Navigate(typeof(CompletedBacklogsPage));
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
#if NETFX_CORE
            try
            {
                ConnectedAnimation connectedAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backwardsAnimation", destinationGrid);
                PopupOverlay.Hide();
                connectedAnimation.Configuration = new DirectConnectedAnimationConfiguration();
                await MainGrid.TryStartConnectedAnimationAsync(connectedAnimation, SelectedBacklog, "connectedElement");
            }
            catch
            {
                PopupOverlay.Hide();
            }
#else
            PopupOverlay.Hide();
#endif
        }

        private async void MainGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedBacklog = e.ClickedItem as Backlog;
            SelectedBacklog = selectedBacklog;
#if NETFX_CORE
            try
            {
                ConnectedAnimation connectedAnimation = MainGrid.PrepareConnectedAnimation("forwardAnimation", SelectedBacklog, "connectedElement");
                connectedAnimation.Configuration = new DirectConnectedAnimationConfiguration();
                connectedAnimation.TryStart(destinationGrid);
            }
            catch (Exception ex)
            {
                await Logger.Error("Error with connected animation", ex);
                // ; )
            }

#endif
            PopupImage.Source = new BitmapImage(new Uri(selectedBacklog.ImageURL));
            PopupTitle.Text = selectedBacklog.Name;
            PopupDirector.Text = selectedBacklog.Director;
            PopupRating.Value = selectedBacklog.UserRating;
            PopupSlider.Value = selectedBacklog.UserRating;

            await PopupOverlay.ShowAsync();
        }

        private void PopupSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            PopupRating.Value = e.NewValue;
        }
    }
}
