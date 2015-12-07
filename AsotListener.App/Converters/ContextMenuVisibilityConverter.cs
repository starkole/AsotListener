namespace AsotListener.App.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Models;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Data;

    public class ContextMenuVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Episode episode = value as Episode;
            if(episode == null)
            {
                return Visibility.Collapsed;
            }

            // TODO: Implement visibility checking logic here

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}
