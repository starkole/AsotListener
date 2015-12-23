namespace AsotListener.Common.Attributes
{
    using System;

    /// <summary>
    /// Attribute for assigning custom name to attribute target
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class DisplayAttribute: Attribute
    {
        /// <summary>
        /// Attribute target custom name
        /// </summary>
        public virtual string Name { get; }

        /// <summary>
        /// Creates new instance of <see cref="DisplayAttribute"/>
        /// </summary>
        /// <param name="name">Custom name to be assigned to attribute target</param>
        public DisplayAttribute(string name)
        {
#pragma warning disable RECS0021 // Warns about calls to virtual member functions occurring in the constructor
            // It's OK to call virtual member here
            Name = name;
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occurring in the constructor
        }
    }
}
