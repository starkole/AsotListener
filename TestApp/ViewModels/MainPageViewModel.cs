namespace AsotListener.ViewModels
{
    using Services;
    using Models;
    using System.Collections.ObjectModel;
    using System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public class MainPageViewModel
    {
        private readonly ObservableCollection<Episode> episodes = new ObservableCollection<Episode>();
        //private readonly PlayerViewModel playerViewModel = new PlayerViewModel();

        public ObservableCollection<Episode> Episodes
        {
            get { return episodes; }
        }

        public MainPageViewModel()
        {
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        public async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            throw new NotImplementedException();

        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        public void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
            throw new NotImplementedException();
        }

        public void Download_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Remove_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Play_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads the content for the second pivot item when it is scrolled into view.
        /// </summary>
        public async void SecondPivot_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
