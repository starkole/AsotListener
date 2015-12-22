namespace AsotListener.App.Converters
{
    using System;
    using Common.Attributes;
    using Windows.UI.Xaml.Data;

    /// <summary>
    /// Converter to convert given enum value to its localized representation
    /// </summary>
    public sealed class EnumToLocalizedNamesConverter : IValueConverter
    {
        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target</param>
        /// <param name="targetType">The type of the target property, as a type reference</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic</param>
        /// <param name="language">The language of the conversion</param>
        /// <returns>The value to be passed to the target dependency property</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var enumValue = value as Enum;
            var attr = enumValue?.GetAttribute<LocalizedDisplayAttribute>();
            return attr?.Name ?? string.Empty;
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
