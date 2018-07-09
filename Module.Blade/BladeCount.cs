using System.ComponentModel;

namespace Module.Blade
{
    /// <summary>
    /// Record the amount to be recorded on the Blade.
    /// </summary>
    public class BladeCount : INotifyPropertyChanged
    {
        #region Fields
        private string _Name;
        private string _Title;
        private long _Count;
        private long _Warn;
        private long _Max;
        #endregion Fields

        #region Constructor
        public BladeCount()
        {
            _Name = string.Empty;
            _Title = string.Empty;
            _Count = 0;
            _Warn = 0;
            _Max = 0;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The name that needs to be counted.
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// The title that needs to be counted.
        /// </summary>
        public string Title
        {
            get { return _Title; }
            set
            {
                if (_Title != value)
                {
                    _Title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        /// <summary>
        /// Indicates the current count value.
        /// </summary>
        public long Count
        {
            get { return _Count; }
            set
            {
                if (_Count != value)
                {
                    _Count = value;
                    OnPropertyChanged("Count");
                }
            }
        }

        /// <summary>
        /// Indicates the preset warning value
        /// </summary>
        public long Warn
        {
            get { return _Warn; }
            set
            {
                if (_Warn != value)
                {
                    _Warn = value;
                    OnPropertyChanged("Warn");
                }
            }
        }

        /// <summary>
        /// Indicates the preset max value
        /// </summary>
        public long Max
        {
            get { return _Max; }
            set
            {
                if (_Max != value)
                {
                    _Max = value;
                    OnPropertyChanged("Max");
                }
            }
        }
        #endregion Properties

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string PropertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
        }
        #endregion INotifyPropertyChanged Members
    }
}
