// ViewModelBase.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HoudiniSafe.ViewModel
{
    /// <summary>
    /// Base class for view models that implements INotifyPropertyChanged.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Raises the PropertyChanged event for the given property name.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Invoke the PropertyChanged event if there are any subscribers
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the value of a property and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">A reference to the backing field of the property.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property (automatically provided by the caller).</param>
        /// <returns>True if the property value was changed; otherwise, false.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            // Check if the value is different from the current value
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            // Update the field with the new value
            field = value;

            // Notify subscribers that the property value has changed
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
