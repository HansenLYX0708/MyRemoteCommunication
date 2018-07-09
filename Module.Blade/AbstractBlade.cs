using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Module.Blade
{
    public abstract class AbstractBlade : IBlade
    {
        #region Fields
        private string _IPAddress = string.Empty;
        private bool _IsConnected = false;
        private bool _Operational = false;
        private BladeState _BladeControl = BladeState.Unknown;
        private OnOffState _MemsStatus = OnOffState.Unknown;
        private OnOffState _LoadStatus = OnOffState.Unknown;
        private OnOffState _CardPower = OnOffState.Unknown;
        private OnOffState _LCD = OnOffState.Unknown;
        private OnOffState _AuxOut0 = OnOffState.Unknown;
        private OnOffState _AuxOut1 = OnOffState.Unknown;
        private OnOffState _AuxIn0 = OnOffState.Unknown;
        private OnOffState _AuxIn1 = OnOffState.Unknown;
        private IEnumerable<BladeCount> _Counts;

        // private HGST.IO.ILine<bool> _UnlockLine;
        public event TextReceivedEventHandler TextReceived;
        public event ResultReceivedEventHandler ResultReceived;
        public event ParametricResultReceivedEventHandler ParametricResultReceived;
        public event BladeEventReceivedEventHandler BladeEventReceived;
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
        public virtual OnOffState AuxOut0Control
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
        public virtual OnOffState AuxOut1Control
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
        public virtual OnOffState AuxIn0Control
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
        public virtual OnOffState AuxIn1Control
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
        /// Gets and sets the state of lock status.
        /// </summary>
        //public virtual OnOffState LockControl
        //{
        //    get
        //    {
        //        if (_UnlockLine == null) return OnOffState.Unknown;
        //        return _UnlockLine.State ? OnOffState.Off : OnOffState.On;
        //    }
        //    set
        //    {
        //        if (_UnlockLine == null) return;
        //        bool newState;
        //        switch (value)
        //        {
        //            case OnOffState.On:
        //            case OnOffState.TuringOn:
        //                newState = false;
        //                break;
        //            case OnOffState.Off:
        //            case OnOffState.TurningOff:
        //                newState = true;
        //                break;
        //            default:
        //                throw new ArgumentException(string.Format("New value {0} is not valid.", value));
        //        }
        //        if (newState != _UnlockLine.State)
        //        {
        //            _UnlockLine.State = newState;
        //            OnPropertyChanged("Lock");
        //        }
        //    }
        //}

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
        public abstract void Connect(string Address, string UserID, string Password);
        public abstract void Disconnect();
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
