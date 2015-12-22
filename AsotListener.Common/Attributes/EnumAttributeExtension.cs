namespace AsotListener.Common.Attributes
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Enum extensions for working with custom attributes
    /// </summary>
    public static class EnumAttributeExtension
    {
        /// <summary>
        /// Gets custom attribute applied to enum field
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="enumValue">Enum value to obtain attribute from</param>
        /// <returns>Custom attribute applied to enum field</returns>
        public static T GetAttribute<T>(this Enum enumValue) where T : Attribute =>
            enumValue
                .GetType()
                .GetTypeInfo()
                .GetDeclaredField(enumValue.ToString())
                .GetCustomAttribute<T>();
    }
}
