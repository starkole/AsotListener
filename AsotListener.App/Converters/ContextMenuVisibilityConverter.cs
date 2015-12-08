namespace AsotListener.App.Converters
{
    using System;
    using Models;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Data;

    public class ContextMenuVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object itemType, string language)
        {
            Visibility result = Visibility.Collapsed;
            Episode episode = value as Episode;
            ContextMenuItem menuItem;
            bool isItemTypeValid = Enum.TryParse(itemType.ToString(), out menuItem);
            if(episode == null || !isItemTypeValid)
            {
                return result;
            }

            
            switch (menuItem)
            {   
                case ContextMenuItem.Download:
                    if (episode.Status == EpisodeStatus.CanBeLoaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case ContextMenuItem.CancelDownload:
                    if (episode.Status == EpisodeStatus.Downloading)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case ContextMenuItem.Delete:
                    if (episode.Status == EpisodeStatus.Loaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case ContextMenuItem.Play:
                    if (episode.Status == EpisodeStatus.Loaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case ContextMenuItem.AddToPlaylist:
                    if (episode.Status == EpisodeStatus.Loaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}
