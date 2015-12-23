namespace AsotListener.Common.Attributes
{
    using System;
    using Windows.ApplicationModel.Resources;

    /// <summary>
    /// Extends <see cref="DisplayAttribute"/> in order to allow usage of localized resources as custom names
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class LocalizedDisplayAttribute: DisplayAttribute
    {
        private static readonly ResourceLoader ResourceLoader = ResourceLoader.GetForCurrentView();
        private readonly string localizedValue;

        /// <summary>
        /// Creates new instance of <see cref="LocalizedDisplayAttribute"/>
        /// </summary>
        /// <param name="fallbackName">Name to be used if no localized resource found</param>
        /// <param name="resouceKey">Localized resource key</param>
        public LocalizedDisplayAttribute(string fallbackName, string resouceKey)
            :this(fallbackName)
        {
            localizedValue = ResourceLoader.GetString(resouceKey);
        }

        /// <summary>
        /// Creates new instance of <see cref="LocalizedDisplayAttribute"/>
        /// </summary>
        /// <param name="fallbackName">Name to be used if no localized resource found</param>
        public LocalizedDisplayAttribute(string fallbackName)
            : base(fallbackName)
        {
        }

        /// <summary>
        /// Attribute target custom name obtained from localized resources
        /// </summary>
        public override string Name => localizedValue ?? base.Name;
    }
}
