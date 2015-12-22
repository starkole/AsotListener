namespace AsotListener.App.Converters
{
    using System;
    using Models.Enums;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Data;
    using static Models.Enums.EpisodeStatus;
    using static Models.Enums.ContextMenuItem;

    /// <summary>
    /// Converter to determine if certain context menu items must be shown or hidden 
    /// depending on episode status
    /// </summary>
    public class ContextMenuVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target</param>
        /// <param name="targetType">The type of the target property, as a type reference</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic</param>
        /// <param name="language">The language of the conversion</param>
        /// <returns>The value to be passed to the target dependency property</returns>
        public object Convert(object value, Type targetType, object itemType, string language)
        {
            Visibility result = Visibility.Collapsed;
            ContextMenuItem menuItem;
            EpisodeStatus status;
            bool isItemTypeValid = Enum.TryParse(itemType.ToString(), out menuItem);
            bool isStatusValid = Enum.TryParse(value.ToString(), out status);
            if (!isStatusValid || !isItemTypeValid)
            {
                return result;
            }

            switch (menuItem)
            {
                case Download:
                    if (status == CanBeLoaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case CancelDownload:
                    if (status == Downloading)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case Delete:
                    if (status == Loaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case Play:
                    if (status == Loaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case AddToPlaylist:
                    if (status == Loaded)
                    {
                        result = Visibility.Visible;
                    }
                    break;
                case ClearPlaylist:
                    result = Visibility.Visible;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object. This method is called only in TwoWay bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The type of the target property, as a type reference.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>The value to be passed to the source object.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
            // This converter is not intended to use with TwoWay binding,
            // so throwing exception is ok here.
            throw new NotImplementedException();
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
        }
    }
}
