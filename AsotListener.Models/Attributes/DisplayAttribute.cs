namespace AsotListener.Models.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.All)]
    public class DisplayAttribute: Attribute
    {
        public virtual string Name { get; }

        public DisplayAttribute(string name)
        {
            Name = name;
        }
    }
}
