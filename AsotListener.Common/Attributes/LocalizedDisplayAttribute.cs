namespace AsotListener.Common.Attributes
{
    using System;
    using Windows.ApplicationModel.Resources;

    [AttributeUsage(AttributeTargets.All)]
    public class LocalizedDisplayAttribute: DisplayAttribute
    {
        private static readonly ResourceLoader ResourceLoader = ResourceLoader.GetForCurrentView();
        private readonly string localizedValue;

        public LocalizedDisplayAttribute(string fallbackName, string resouceKey)
            :this(fallbackName)
        {
            localizedValue = ResourceLoader.GetString(resouceKey);
        }

        public LocalizedDisplayAttribute(string fallbackName)
            : base(fallbackName)
        {
        }

        public override string Name => localizedValue ?? base.Name;
    }
}
