namespace AsotListener.Common.Attributes
{
    using System;
    using System.Reflection;

    public static class EnumAttributeExtension
    {
        public static T GetAttribute<T>(this Enum enumValue) where T : Attribute =>
            enumValue
                .GetType()
                .GetTypeInfo()
                .GetDeclaredField(enumValue.ToString())
                .GetCustomAttribute<T>();
    }
}
