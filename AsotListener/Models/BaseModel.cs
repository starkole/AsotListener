namespace AsotListener.Models
{
    using Services;
    using Models;
    using System.Collections.ObjectModel;
    using System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using System.Runtime.Serialization;
    using System.ComponentModel;
    using Windows.Foundation.Diagnostics;
    using System.Threading.Tasks;

    public class BaseModel: INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) 
            { 
                handler(this, new PropertyChangedEventArgs(propertyName)); 
            }
        }
        
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
