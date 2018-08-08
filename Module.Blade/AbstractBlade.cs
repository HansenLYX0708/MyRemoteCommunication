using System;
using System.Collections.Generic;
using System.ComponentModel;
using WD.Tester.Enums;

namespace Module.Blade
{
    public abstract class AbstractBlade : IBlade
    {
        #region Fields
        private string _IPAddress = string.Empty;
        private bool _IsConnected = false;
        private bool _Operational = false;

        private OnOffValues _Pwr12V;
        private OnOffValues _Pwr5V;
        private OnOffValues _AuxOut0;
        private OnOffValues _AuxOut1;
        private OnOffValues _AuxIn0;
        private OnOffValues _AuxIn1;
        private OnOffValues _BackLight;
        private OnOffValues _Solenoid;
        private String _SolenoidRamp;
        private String _FirmwareVer;
        private String _DriverVer;


        private BladeState _BladeControl = BladeState.Unknown;
        private OnOffState _MemsStatus = OnOffState.Unknown;
        private OnOffState _LoadStatus = OnOffState.Unknown;
        private OnOffState _CardPower = OnOffState.Unknown;
        private OnOffState _LCD = OnOffState.Unknown;
        
        
        private IEnumerable<BladeCount> _Counts;
        #endregion Fields

        #region Properties
        /// <summary>
        /// The IP address that needs to be connected
        /// </summary>
        public virtual string IPAddress
        {
            get { return _IPAddress; }
            set
            {
                if (_IPAddress != value)
                {
                    _IPAddress = value;
                    OnPropertyChanged("IPAddress");
                }
            }
        }

        /// <summary>
        /// Indicates that the blade is connected.
        /// </summary>
        public virtual bool IsConnected
        {
            get { return _IsConnected; }
            set
            {
                if (_IsConnected != value)
                {
                    _IsConnected = value;
                    OnPropertyChanged("IsConnected");
                }
            }
        }

        /// <summary>
        /// Indicates that the blade has been operated.
        /// </summary>
        public virtual bool Operational
        {
            get { return _Operational; }
            protected set
            {
                if (_Operational != value)
                {
                    _Operational = value;
                    OnPropertyChanged("Operational");
                }
            }
        }

        /// <summary>
        /// Indicates the status of blade when running.
        /// </summary>
        public virtual BladeState BladeControl
        {
            get { return _BladeControl; }
            protected set
            {
                if (_BladeControl != value)
                {
                    _BladeControl = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of Mems.
        /// It's override in BladeModel
        /// </summary>
        public virtual OnOffState MemsControl
        {
            get { return _MemsStatus; }
            set
            {
                if (_MemsStatus != value)
                {
                    _MemsStatus = value;
                    OnPropertyChanged("MemsStatus");
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of card power in blade.
        /// It's override in BladeModel
        /// </summary>
        public virtual OnOffState CardPowerControl
        {
            get { return _CardPower; }
            set
            {
                if (_CardPower != value)
                {
                    _CardPower = value;
                    OnPropertyChanged("CardPower");
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of LCD.
        /// It's override in BladeModel
        /// </summary>
        public virtual OnOffState LCDControl
        {
            get { return _LCD; }
            set
            {
                if (_LCD != value)
                {
                    _LCD = value;
                    OnPropertyChanged("LCD");
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of AuxOut0.
        /// It's override in BladeModel
        /// </summary>
        public virtual OnOffValues AuxOut0
        {
            get { return _AuxOut0; }
            set
            {
                if (_AuxOut0 != value)
                {
                    _AuxOut0 = value;
                    OnPropertyChanged("AuxOut0");
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of AuxOut1.
        /// It's override in BladeModel
        /// </summary>
        public virtual OnOffValues AuxOut1
        {
            get { return _AuxOut1; }
            set
            {
                if (_AuxOut1 != value)
                {
                    _AuxOut1 = value;
                    OnPropertyChanged("AuxOut1");
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of load state.
        /// </summary>
        public virtual OnOffState LoadStatus
        {
            get { return _LoadStatus; }
            set
            {
                if (_LoadStatus != value)
                {
                    _LoadStatus = value;
                    OnPropertyChanged("LoadStatus");
                }
            }
        }

        /// <summary>
        /// Gets the state of AuxIn0.
        /// </summary>
        public virtual OnOffValues AuxIn0
        {
            get { return _AuxIn0; }
            protected set
            {
                if (_AuxIn0 != value)
                {
                    _AuxIn0 = value;
                    OnPropertyChanged("AuxIn0");
                }
            }
        }

        /// <summary>
        /// Gets the state of AuxIn1.
        /// </summary>
        public virtual OnOffValues AuxIn1
        {
            get { return _AuxIn1; }
            protected set
            {
                if (_AuxIn1 != value)
                {
                    _AuxIn1 = value;
                    OnPropertyChanged("AuxIn1");
                }
            }
        }

        /// <summary>
        /// Record the amount to be recorded on the Blade.
        /// </summary>
        public virtual IEnumerable<BladeCount> Counts
        {
            get { return _Counts; }
            set
            {
                if (_Counts != value)
                {
                    _Counts = value;
                    OnPropertyChanged("Counts");
                }
            }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        /// Abstract interface to connect to a server.
        /// </summary>
        /// <param name="Address">The server's IP address, shaped like "10.10.131.131".</param>
        /// <param name="UserID">When authentication is required, the user ID is set aside.</param>
        /// <param name="Password">Same as user ID.</param>
        public abstract void Connect(string Address, string UserID, string Password);
        /// <summary>
        /// Abstract interface to disconnect to a server.
        /// </summary>
        public abstract void Disconnect();
        /// <summary>
        /// Abstract interface to TCL command.
        /// </summary>
        /// <param name="Command"></param>
        public abstract void TclCommand(string Command);
        #endregion Methods

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
        #endregion INotifyPropertyChanged Members
    }
}
