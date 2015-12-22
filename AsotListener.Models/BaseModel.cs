namespace AsotListener.Models
{
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base class for all models
    /// </summary>
    [DataContract]
    public class BaseModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        /// <summary>
        /// Fires when one of the instance properties changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes <see cref="PropertyChanged"/> event handler
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the new value for the field and fires <see cref="PropertyChanged"/> event handler
        /// </summary>
        /// <typeparam name="T">Property field type</typeparam>
        /// <param name="field">Property filed to be updated</param>
        /// <param name="value">New property value</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>True if property has been updated, false otherwise</returns>
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
