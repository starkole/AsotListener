namespace AsotListener.Common.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.All)]
    public class DisplayAttribute: Attribute
    {
        public virtual string Name { get; }

        public DisplayAttribute(string name)
        {
#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
            // It's OK to call virtual member here
            Name = name;
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor
        }
    }
}
