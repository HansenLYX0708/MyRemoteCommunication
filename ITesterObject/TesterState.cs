using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hitachi.Tester.Module
{
    public class TesterState : StopGoPauseState
    {
        #region Fields
        /// <summary>
        /// The current step in a sequence
        /// </summary>
        private int _TestNumber;
        
        /// <summary>
        /// The current sequence name.
        /// </summary>
        private string _SequenceName;
        
        /// <summary>
        /// 12V power state.
        /// </summary>
        private bool _12VDC;

        /// <summary>
        /// 5V power state.
        /// </summary>
        private bool _5VDC;

        /// <summary>
        /// Bunny Aux out 0 state.
        /// </summary>
        private bool _AuxOut0;
        
        /// <summary>
        /// Bunny Aux out 1 state.
        /// </summary>
        private bool _AuxOut1;
        
        /// <summary>
        /// Bunny Aux in 0 state.  Updated when port is read.
        /// </summary>
        private bool _AuxIn0;
        
        /// <summary>
        /// Bunny Aux in 1 state.  Updated when port is read.
        /// </summary>
        private bool _AuxIn1;
        
        private bool _MemsSolenoid;

        /// <summary>
        /// Bunny LCD backlight state.
        /// </summary>
        private bool _BackLight;

        /// <summary>
        /// The current grade filename.
        /// </summary>
        private string _GradeName;

        /// <summary>
        /// Pin motion type.
        /// </summary>
        private BunnyPinMotionType _PinMotionType;

        /// <summary>
        /// Open sensor type.
        /// </summary>
        private BunnyPinMotionSensor _PinMotionOpenSensor;

        /// <summary>
        /// Close sensor type
        /// </summary>
        private BunnyPinMotionSensor _PinMotionCloseSensor;

        /// <summary>
        /// Ramp initialized flag.
        /// </summary>
        private bool _RampInited;

        /// <summary>
        /// Bunny status flag
        /// </summary>
        private bool _BunnyGood;

        /// <summary>
        /// Pin motion servo enable flag.
        /// </summary>
        private bool _ServoEnabled;
        #endregion Fields

        #region Constructors
        public TesterState()
        {
            base.NowTestsArePaused = false;
            base.PauseTests = false;
            base.PauseEvents = false;
            base.CmdBusy = false;
            base.PleaseStop = false;
            base.SeqGoing = false;
            _TestNumber = -1;
            _SequenceName = "";
            _12VDC = false;
            _5VDC = false;
            _AuxOut0 = false;
            _AuxOut1 = false;
            _AuxIn0 = false;
            _AuxIn1 = false;
            _MemsSolenoid = false;
            _BackLight = false;
            _GradeName = "";
            _PinMotionType = BunnyPinMotionType.NONE;
            _PinMotionOpenSensor = BunnyPinMotionSensor.NONE;
            _PinMotionCloseSensor = BunnyPinMotionSensor.NONE;
            _RampInited = false;
            _BunnyGood = false;
            _ServoEnabled = false;
        }

        public TesterState (TesterState that)
        {
            Assign(that);
        }
        #endregion Constructors

        #region Properties
        public int TestNumber
        {
            get { return _TestNumber; }
            set { _TestNumber = value; }
        }
        public string SequenceName
        {
            get { return _SequenceName; }
            set { _SequenceName = value; }
        }
        public bool B12VDC
        {
            get { return _12VDC; }
            set { _12VDC = value; }
        }
        public bool B5VDC
        {
            get { return _5VDC; }
            set { _5VDC = value; }
        }
        public bool AuxOut0
        {
            get { return _AuxOut0; }
            set { _AuxOut0 = value; }
        }
        public bool AuxOut1
        {
            get { return _AuxOut1; }
            set { _AuxOut1 = value; }
        }
        public bool AuxIn0
        {
            get { return _AuxIn0; }
            set { _AuxIn0 = value; }
        }
        public bool AuxIn1
        {
            get { return _AuxIn1; }
            set { _AuxIn1 = value; }
        }
        public bool MemsSolenoid
        {
            get { return _MemsSolenoid; }
            set { _MemsSolenoid = value; }
        }
        public bool BackLight
        {
            get { return _BackLight; }
            set { _BackLight = value; }
        }
        public string GradeName
        {
            get { return _GradeName; }
            set { _GradeName = value; }
        }
        public BunnyPinMotionType PinMotionType
        {
            get { return _PinMotionType; }
            set { _PinMotionType = value; }
        }

        public BunnyPinMotionSensor PinMotionOpenSensor
        {
            get { return _PinMotionOpenSensor; }
            set { _PinMotionOpenSensor = value; }
        }

        public BunnyPinMotionSensor PinMotionCloseSensor
        {
            get { return _PinMotionCloseSensor; }
            set { _PinMotionCloseSensor = value; }
        }

        public bool RampInited
        {
            get { return _RampInited; }
            set { _RampInited = value; }
        }

        public bool BunnyGood
        {
            get { return _BunnyGood; }
            set { _BunnyGood = value; }
        }

        public bool ServoEnabled
        {
            get { return _ServoEnabled; }
            set { _ServoEnabled = value; }
        }
        #endregion Properties

        #region Methods
        public void Assign (TesterState that)
        {
            base.CmdBusy = that.CmdBusy;
            base.PleaseStop = that.PleaseStop;
            base.SeqGoing = that.SeqGoing;
            base.PauseEvents = that.PauseEvents;
            base.PauseTests = that.PauseTests;
            base.NowTestsArePaused = that.NowTestsArePaused;
            this._12VDC = that._12VDC;
            this._5VDC = that._5VDC;
            this._AuxIn0 = that._AuxIn0;
            this._AuxIn1 = that._AuxIn1;
            this._AuxOut0 = that._AuxOut0;
            this._AuxOut1 = that._AuxOut1;
            this._BackLight = that._BackLight;
            this._MemsSolenoid = that._MemsSolenoid;
            this._TestNumber = that._TestNumber;
            this._SequenceName = that._SequenceName;
            this._GradeName = that._GradeName;
            this._PinMotionType = that._PinMotionType;
            this._PinMotionOpenSensor = that._PinMotionOpenSensor;
            this._PinMotionCloseSensor = that._PinMotionCloseSensor;
            this._RampInited = that._RampInited;
            this._BunnyGood = that._BunnyGood;
            this._ServoEnabled = that._ServoEnabled;
            //base.bOnLine = that.bOnLine;
        }

        /// <summary>
        /// To string 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(this._12VDC ? "" : "!");
            sb.Append("12V ");
            sb.Append(this._5VDC ? "" : "!");
            sb.Append("5V ");
            sb.Append(this._AuxIn0 ? "" : "!");
            sb.Append("AuxIn0 ");
            sb.Append(this._AuxIn1 ? "" : "!");
            sb.Append("AuxIn1 ");
            sb.Append(this._AuxOut0 ? "" : "!");
            sb.Append("AuxOut0 ");
            sb.Append(this._AuxOut1 ? "" : "!");
            sb.Append("AuxOut1 ");
            sb.Append(this._BackLight ? "" : "!");
            sb.Append("Light ");
            sb.Append(this._MemsSolenoid ? "" : "!");
            sb.Append("SolenoidOpen ");
            sb.Append(this._TestNumber.ToString() + " ");
            sb.Append(this._SequenceName + " ");
            sb.Append(this._GradeName + " ");
            sb.Append(this._PinMotionType.ToString() + " ");
            sb.Append("Open " + this._PinMotionOpenSensor.ToString() + " ");
            sb.Append("Close " + this._PinMotionCloseSensor.ToString() + " ");
            sb.Append(this._RampInited ? "" : "!");
            sb.Append("RampInited ");
            sb.Append(this._BunnyGood ? "" : "!");
            sb.Append("BunnyOK ");
            sb.Append(this._ServoEnabled ? "" : "!");
            sb.Append("ServoEnable ");
            return sb.ToString();
        }
        #endregion Methods
    }

    /// <summary>
    /// Enum  for Pin motion type. 
    /// </summary>
    public enum BunnyPinMotionType
    {
        /// <summary>
        /// No pin motion device
        /// </summary>
        NONE,

        /// <summary>
        /// Servo pin motion device
        /// </summary>
        SERVO,

        /// <summary>
        /// Solenoid pin motion device
        /// </summary>
        SOLENOID,
    }

    /// <summary>
    /// Enum for pin motion sensor type.
    /// </summary>
    public enum BunnyPinMotionSensor
    {
        /// <summary>
        /// No sendor
        /// </summary>
        NONE,

        /// <summary>
        /// High level active sensor.
        /// </summary>
        HIGH,

        /// <summary>
        /// Low level active sensor.
        /// </summary>
        LOW,
    }
}
