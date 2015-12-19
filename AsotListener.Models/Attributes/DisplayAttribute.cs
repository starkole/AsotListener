namespace AsotListener.Models.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.All)]
    public class DisplayAttribute: Attribute
    {
        public virtual string Name { get; }

        public DisplayAttribute(string name)
        {
#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
            Name = name;
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor
        }
    }
}
