using System;
using System.Threading;
using System.IO;
using System.Windows.Forms;

using WD.Tester;
using WD.Tester.Client;
using WD.Tester.Enums;
using NLog;
using WD.Tester.Module;

namespace Module.Blade
{
    /// <summary>
    /// This is the entity class of blade, communicating through WCF.
    /// </summary>
    public class BladeModel : AbstractBlade
    {
        #region  Fields
        private Logger logger = LogManager.GetLogger("SlotLog");
        private string _MEMSSN;
        private string _DISKSN;
        private string _MBPSN;
        private string _ActuatorSN;
        private string _PCBASN;
        private string _BladeSN;
        private string _MotorSN;
        private string _FlexSN;
        private object onBunnyEventLockObj;
        /// <summary>
        /// Represents the initial value of the data on the Blade.
        /// </summary>
        private readonly string _BladeDataInit;
        #endregion Fields

        #region Constructor and destructor
        /// <summary>
        /// Default constructor, initialize the attributes, not including RemoteConnectLib
        /// </summary>
        public BladeModel()
        {
            logger.Info("BladeModel construct start");
            TestingControl = false;
            _BladeDataInit = "__";

            _MEMSSN = string.Empty;
            _DISKSN = string.Empty;
            _MBPSN = string.Empty;
            _ActuatorSN = string.Empty;
            _PCBASN = string.Empty;
            _BladeSN = string.Empty;
            logger.Info("BladeModel construct complete");
            Initialize();
            onBunnyEventLockObj = new object();
            RemoteInstance.comBunnyEvent += new StatusEventHandler(OnBunnyEvent);
        }

        ~BladeModel()
        {
            Disconnect();
            Deinitialize();
        }

        /// <summary>
        /// Initialize the RemoteConnectLib
        /// </summary>
        public void Initialize()
        {
            Deinitialize();
            logger.Info("BladeModel::Initialize start.");
            RemoteInstance = new RemoteConnectLib();
            if (RemoteInstance != null)
            {
                logger.Info("BladeModel::Initialize complete. [RemoteInstance:{0}]", RemoteInstance.ToString());
            }
        }

        /// <summary>
        /// Deinitialize the RemoteConnectLib
        /// </summary>
        public void Deinitialize()
        {
            logger.Info("Slot::BladeModel::Deinitialize start.");
            // Cleanup RemoteconnectLib
            if (RemoteInstance != null)
            {
                RemoteInstance.Dispose();
                RemoteInstance = null;
            }
        }
        #endregion Constructor and destructor

        #region Properties
        /// <summary>
        /// Provide external communication interface.
        /// </summary>
        public RemoteConnectLib RemoteInstance { get; private set; }

        /// <summary>
        /// Gets and sets the state of test.
        /// </summary>
        public bool TestingControl { get; set; }
        #endregion

        #region Status Control Override from BladeModeBase Properties
        /// <summary>
        /// Gets and sets the state of Mems.
        /// </summary>
        public override OnOffState MemsControl
        {
            get { return base.MemsControl; }
            set
            {
                logger.Info("BladeModel MemsControl set [status:{0}]", value);
                switch (value)
                {
                    case OnOffState.On:
                    case OnOffState.TurningOn:
                        RemoteInstance.PinMotion(true);
                        base.MemsControl = OnOffState.On;
                        break;

                    case OnOffState.Off:
                    case OnOffState.TurningOff:
                        RemoteInstance.PinMotion(false);
                        base.MemsControl = OnOffState.Off;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of card power in blade.
        /// </summary>
        public override OnOffState CardPowerControl
        {
            get { return base.CardPowerControl; }
            set
            {
                logger.Info("BladeModel CardPowerControl set [status:{0}]", value);
                switch (value)
                {
                    case OnOffState.On:
                    case OnOffState.TurningOn:
                        RemoteInstance.CardPower(true);
                        base.CardPowerControl = OnOffState.On;
                        break;

                    case OnOffState.Off:
                    case OnOffState.TurningOff:
                        RemoteInstance.CardPower(false);
                        base.CardPowerControl = OnOffState.Off;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of LCD.
        /// </summary>
        public override OnOffState LCDControl
        {
            get { return base.LCDControl; }
            set
            {
                logger.Info("BladeModel LCDControl set [status:{0}]", value);
                switch (value)
                {
                    case OnOffState.On:
                    case OnOffState.TurningOn:
                        RemoteInstance.BackLight(true);
                        base.LCDControl = OnOffState.On;
                        break;

                    case OnOffState.Off:
                    case OnOffState.TurningOff:
                        RemoteInstance.BackLight(false);
                        base.LCDControl = OnOffState.Off;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of AuxOut0.
        /// </summary>
        public override OnOffState AuxOut0Control
        {
            get { return base.AuxOut0Control; }
            set
            {
                logger.Info("BladeModel AuxOut0Control set [status:{0}]", value);
                switch (value)
                {
                    case OnOffState.On:
                    case OnOffState.TurningOn:
                        RemoteInstance.AuxOut0(1);
                        base.AuxOut0Control = OnOffState.On;
                        break;

                    case OnOffState.Off:
                    case OnOffState.TurningOff:
                        RemoteInstance.AuxOut0(0);
                        base.AuxOut0Control = OnOffState.Off;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets and sets the state of AuxOut1.
        /// </summary>
        public override OnOffState AuxOut1Control
        {
            get { return base.AuxOut1Control; }
            set
            {

                switch (value)
                {
                    case OnOffState.On:
                    case OnOffState.TurningOn:
                        RemoteInstance.AuxOut1(1);
                        base.AuxOut1Control = OnOffState.On;
                        break;

                    case OnOffState.Off:
                    case OnOffState.TurningOff:
                        RemoteInstance.AuxOut1(0);
                        base.AuxOut1Control = OnOffState.Off;
                        break;
                }
            }
        }
        #endregion Status Control Override from BladeModeBase Properties

        #region SN Properties
        /// <summary>
        /// The serial number information of blade.
        /// </summary>
        public string BladeSN
        {
            get
            {
                if (_BladeSN == string.Empty || _BladeSN == null || _BladeSN == _BladeDataInit)
                {
                    _BladeSN = _GetDataFromBladee("BladeSN");
                    if (_BladeSN == string.Empty)
                    {
                        _BladeSN = _BladeDataInit;
                    }
                }
                return _BladeSN;
            }
        }

        /// <summary>
        /// The serial number information of Mems.
        /// </summary>
        public string MEMSSN
        {
            get
            {
                if (_MEMSSN == string.Empty || _MEMSSN == null || _MEMSSN == _BladeDataInit)
                {
                    _MEMSSN = _GetDataFromBladee("MEMSSN");
                    if (_MEMSSN == string.Empty)
                    {
                        _MEMSSN = _BladeDataInit;
                    }
                }
                return _MEMSSN;
            }
        }

        /// <summary>
        /// The serial number information of disk.
        /// </summary>
        public string DISKSN
        {
            get
            {
                if (_DISKSN == string.Empty || _DISKSN == null || _DISKSN == _BladeDataInit)
                {
                    _DISKSN = _GetDataFromBladee("DISKSN");
                    if (_DISKSN == string.Empty)
                    {
                        _DISKSN = _BladeDataInit;
                    }
                }
                return _DISKSN;
            }
        }

        /// <summary>
        /// The serial number information of MBP.
        /// </summary>
        public string MBPSN
        {
            get
            {
                if (_MBPSN == string.Empty || _MBPSN == null || _MBPSN == _BladeDataInit)
                {
                    _MBPSN = _GetDataFromBladee("MotorBasePlate");
                    if (_MBPSN == string.Empty)
                    {
                        _MBPSN = _BladeDataInit;
                    }
                }
                return _MBPSN;
            }
        }

        /// <summary>
        /// The serial number information of Actuator.
        /// </summary>
        public string ActuatorSN
        {
            get
            {
                if (_ActuatorSN == string.Empty || _ActuatorSN == null || _ActuatorSN == _BladeDataInit)
                {
                    _ActuatorSN = _GetDataFromBladee("Actuator");
                    if (_ActuatorSN == string.Empty)
                    {
                        _ActuatorSN = _BladeDataInit;
                    }
                }
                return _ActuatorSN;
            }
        }

        /// <summary>
        /// The serial number information of PCB.
        /// </summary>
        public string PCBASN
        {
            get
            {
                if (_PCBASN == string.Empty || _PCBASN == null || _PCBASN == _BladeDataInit)
                {
                    _PCBASN = _GetDataFromBladee("PcbaSN");
                    if (_PCBASN == string.Empty)
                    {
                        _PCBASN = _BladeDataInit;
                    }
                }
                return _PCBASN;
            }
        }

        /// <summary>
        /// The serial number information of PCB.
        /// </summary>
        public string FlexSN
        {
            get
            {
                if (_FlexSN == string.Empty || _FlexSN == null || _FlexSN == _BladeDataInit)
                {
                    _FlexSN = _GetDataFromBladee("FlexSN");
                    if (_FlexSN == string.Empty)
                    {
                        _FlexSN = _BladeDataInit;
                    }
                }
                return _FlexSN;
            }
        }

        /// <summary>
        /// The serial number information of PCB.
        /// </summary>
        public string MotorSN
        {
            get
            {
                if (_MotorSN == string.Empty || _MotorSN == null || _MotorSN == _BladeDataInit)
                {
                    _MotorSN = _GetDataFromBladee("MotorSN");
                    if (_MotorSN == string.Empty)
                    {
                        _MotorSN = _BladeDataInit;
                    }
                }
                return _MotorSN;
            }
        }
        #endregion SN Properties

        #region Overrides on BladeModelBase Methods
        /// <summary>
        /// The automation system connects the blade from here. 
        /// </summary>
        /// <param name="Address">The server's IP address, shaped like "10.10.131.131".</param>
        /// <param name="UserID">When authentication is required, the user ID is set aside.</param>
        /// <param name="Password">Same as user ID.</param>
        public override void Connect(string address, string userID, string password)
        {
            logger.Info("BladeModel::Connect [Address:{0}] [UserID:{1}] [Password:{2}]", address, userID, password);
            Disconnect();

            if (address != null && address != string.Empty)
            {
                IPAddress = address;
            }

            BladeControl = BladeState.OnConnecting;
            uint result = RemoteInstance.Connect(IPAddress, string.Empty, string.Empty);
            if (RemoteInstance.Connected)
            {
               // _RemoteInstance.comStatusEvent += new StatusEventHandler(remoteInstance_comStatusEvent);
                string pingResult = RemoteInstance.PingAllEvent("hello");
                _BladeSN = RemoteInstance.GetSerialNumber();
                UpdateMemsStatus();
                BladeControl = BladeState.Idle;
                IsConnected = true;
            }
            else
            {
                BladeControl = BladeState.Disconnected;
            }
            logger.Info("BladeModel::Connect complete [Result:{0}] [BladeControl:{1}] [BladeSn:{2}]", result.ToString(), Enum.GetName(typeof(BladeState), BladeControl), _BladeSN);
        }

        /// <summary>
        /// The automation system disconnects from the blade here.
        /// </summary>
        public override void Disconnect()
        {
            if (RemoteInstance != null && RemoteInstance.Connected)
            {
                logger.Info("BladeModel::Disconnect [IsConnect:{0}] [Remote.Connected:{1}]", IsConnected, RemoteInstance.Connected.ToString());
                RemoteInstance.Disconnect();
            }
            BladeControl = BladeState.Disconnected;
            IsConnected = false;
        }

        /// <summary>
        /// Reserved for extending the TCL module.
        /// </summary>
        /// <param name="Command">Command of TCL.</param>
        public override void TclCommand(string Command)
        {
            if (RemoteInstance != null && RemoteInstance.Connected)
            {
                RemoteInstance.TclCommand(Command, true);
            }
            else
            {
                try
                {
                    RemoteInstance.Connect(IPAddress, string.Empty, string.Empty);
                    RemoteInstance.TclCommand(Command, true);
                }
                catch
                { }
            }
        }
        #endregion Overrides on BladeModelBase Methods

        #region internal Methods
        /// <summary>
        /// Reads the first line of the specified file that is not empty.
        /// </summary>
        /// <param name="dataName">data name</param>
        /// <returns>The first row that is not empty.</returns>
        private string _GetDataFromBladee(string dataName)
        {
            logger.Info("BladeModel::_GetDataFromBladee [data:{0}]", dataName);
            string line = _BladeDataInit;
            try
            {
                // TODO : Just for test.
                //Stream stream = ReadFileFromBlade(string.Format("flash:\\{0}.txt", dataName));
                Stream stream = ReadFileFromBlade(string.Format(@"C:\test\{0}.txt", dataName));
                StreamReader sr = new StreamReader(stream);
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line != string.Empty)
                    {
                        break;
                    }
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return line;
        }

        /// <summary>
        /// Read data from blade.
        /// </summary>
        /// <param name="fileName">Full path to the file.</param>
        /// <returns>Data output in stream format</returns>
        private Stream ReadFileFromBlade(string fileName)
        {
            if (!IsConnected)
            {
                Connect(IPAddress, string.Empty, string.Empty);
            }
            // TODO : (old)This is because takes a long time to connect we need to wait 
            // TODO : but 5 seconds is too long, need verify in test
            //Thread.Sleep(5000);
            return RemoteInstance.BladeFileRead(fileName);
        }

        /// <summary>
        /// Writes the data to the specified file for the blade.
        /// </summary>
        /// <param name="filename">Full path to the file.</param>
        /// <param name="fileBody">Data</param>
        private void WriteFileToBlade(string filename, Stream fileBody)
        {
            logger.Info("BladeModel::WriteFileToBlade start [filename:{0}]", filename);
            string strRet = "";
            strRet = RemoteInstance.BladeFileWrite(fileBody, filename);
            logger.Info("BladeModel::WriteFileToBlade complete [strRet:{0}]", strRet);
        }

        /// <summary>
        /// Update status of Mems when connecting.
        /// </summary>
        private void UpdateMemsStatus()
        {
            switch (RemoteInstance.GetMemsState())
            {
                case MemsStateValues.Opened: MemsControl = OnOffState.On; break;
                case MemsStateValues.Opening: MemsControl = OnOffState.TurningOn; break;
                case MemsStateValues.Closed: MemsControl = OnOffState.Off; break;
                case MemsStateValues.Closing: MemsControl = OnOffState.TurningOff; break;
                case MemsStateValues.Unknown: MemsControl = OnOffState.Unknown; break;
            }
        }

        /// <summary>
        /// Called from remote blade each time a Bunny function is called.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBunnyEvent(object sender, StatusEventArgs e)
        {
            // Mostly don't know why this if is here.
            // "meowner" comes from the counts class and does not need to be displayed - this is OK.
            // "none" -- Do not know.
            // I added in != for each blade info type so that they could be set to "NONE" as a value.
            if (((BunnyEvents)e.EventType) != BunnyEvents.BladeSN &&
               ((BunnyEvents)e.EventType) != BunnyEvents.BladeType &&
               ((BunnyEvents)e.EventType) != BunnyEvents.ActuatorSN &&
               ((BunnyEvents)e.EventType) != BunnyEvents.MemsSN &&
               ((BunnyEvents)e.EventType) != BunnyEvents.MotorBaseSN &&
               ((BunnyEvents)e.EventType) != BunnyEvents.DiskSN &&
               ((BunnyEvents)e.EventType) != BunnyEvents.PcbaSN &&
               ((BunnyEvents)e.EventType) != BunnyEvents.FlexSN &&
               ((BunnyEvents)e.EventType) != BunnyEvents.MotorSN &&
               e.Text.ToLower().Contains("none")
               && !e.Text.ToLower().Contains("meowner")) return;

            lock(onBunnyEventLockObj)
            {
                MethodInvoker del = delegate
                {
                    // Ping all event command
                    if (e.Text.Contains("BunnyEvent " + Constants.CommTestString))
                    {
                        // TODO : Notify UI here ?
                        return;
                    }
                    switch ((BunnyEvents)e.EventType)
                    {
                        case BunnyEvents.BladeSN:
                            _BladeSN = e.Text;
                            break;
                        case BunnyEvents.ActuatorSN:
                            _ActuatorSN = e.Text;
                            break;
                        case BunnyEvents.MemsSN:
                            _MEMSSN = e.Text;
                            break;
                        case BunnyEvents.MotorBaseSN:
                            _MBPSN = e.Text;
                            break;
                        case BunnyEvents.DiskSN:
                            _DISKSN = e.Text;
                            break;
                        case BunnyEvents.PcbaSN:
                            _PCBASN = e.Text;
                            break;
                        case BunnyEvents.FlexSN:
                            _FlexSN = e.Text;
                            break;
                        case BunnyEvents.MotorSN:
                            _MotorSN = e.Text;
                            break;
                        // TODO : Remove Jade serial number? 
                        case BunnyEvents.JadeSN:
                            break;





                        case BunnyEvents.MemsType:
                            break;

                        case BunnyEvents.MemsOpenClose:
                            OpenCloseMems(e.Text);
                            

                            if (e.Text == MemsStateValues.Opening.ToString() ||
                              e.Text == MemsStateValues.Closing.ToString() ||
                              e.Text == MemsStateValues.Unknown.ToString())
                            {
                            }
                            else
                            {

                                base.MemsControl = (OnOffState)Enum.Parse(typeof(OnOffState), "Opened");
                            }
                            break;












                        case BunnyEvents.BackLight:
                            break;
                        case BunnyEvents.Solenoid:
                            break;
                        case BunnyEvents.SolenoidRamp:
                            break;
                        case BunnyEvents.FirmwareVer:
                            break;
                        case BunnyEvents.DriverVer:
                            break;
                        case BunnyEvents.SetSaveServo:
                            break;
                        case BunnyEvents.BunnyStatus:
                            break;
                        case BunnyEvents.BladeType:
                            break;

                    }
                };
                del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { del.EndInvoke(ar); }), del);
            }
        }

        private void OpenCloseMems(string text)
        {
            // Switch MemsStateValues to OnOffState

        }

        //void remoteInstance_comStatusEvent(object sender, StatusEventArgs e)
        //{
        //    //Event::GetParametricDataFromBlade::Blade3ParametricData::04/18/2013,05:21:24,04/18/2013,13:21:24,04/18/2013-13:21:24,ENGR,DEFAULT,{},DEFAULT,DEFAULT,{},{},{},{},{},{},{},07.0102699,0.0.0,69849697,C,DEFAULT,DEFAULT,DN,3,0,DEFAULT,JETB,0,-1,DEFAULT,{},{},DEFAULT,DEFAULT,{},{},DEFAULT,DEFAULT,DEFAULT,DEFAULT,JT37_JG2OA540_JG540T03,{},120000,0,0,-1e-6,0,0,-1,-1,-1,-1,-1,-1,3,2,2,{},{},{},{},{},,,-1,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,6,CB,4000,

        //    switch ((eventInts)e.EventType)
        //    {
        //        case eventInts.toTv: OnTextReceived(e.Text, TextCategory.Normal); break;
        //        case eventInts.cmdWin: OnTextReceived(e.Text, TextCategory.Normal); break;
        //        case eventInts.cmdWinDone: break;
        //        case eventInts.tclResult: OnResultReceived(e.Text); break;
        //        case eventInts.error: OnTextReceived(e.Text, TextCategory.Error); break;
        //        case eventInts.eventVal:
        //            {
        //                logger.Info("Incoming event from blade {0}: {1}", IPAddress, e.Text);
        //                string[] splitString = new string[1];
        //                splitString[0] = "::";

        //                string[] token = e.Text.Trim().Split(splitString, StringSplitOptions.None);
        //                //Match eventMatch = Regex.Match(e.Text.Trim(), @"Event::([\w*]+?)::([\w*]+?)::(\w*)");
        //                //Match eventMatch = Regex.Match(e.Text.Trim(), @"Event::([\w*])::([\w*])::([\w]*)");
        //                if (token.Length >= 3)
        //                {
        //                    string Name = token[1];
        //                    string SubName = token[2];
        //                    string Value = token[3];

        //                    switch (Name)
        //                    {
        //                        //case "Completed":
        //                        //    BladeStatus = BladeState.Idle;
        //                        //    break;

        //                        case "Operational":
        //                            {
        //                                bool bOperational;
        //                                if (bool.TryParse(Value, out bOperational)) Operational = bOperational;
        //                            }
        //                            break;

        //                        case "LUL":
        //                            switch (Value)
        //                            {
        //                                case "Loaded": LoadStatus = OnOffState.On; break;
        //                                case "Unloaded": LoadStatus = OnOffState.Off; break;
        //                                default: LoadStatus = OnOffState.Unknown; break;
        //                            }
        //                            break;

        //                        case "MEMS":
        //                            switch (Value)
        //                            {
        //                                case "Open": MemsStatus = OnOffState.On; break;
        //                                case "Closed": MemsStatus = OnOffState.Off; break;
        //                                default: MemsStatus = OnOffState.Unknown; break;
        //                            }
        //                            break;
        //                        case "GetParametricDataFromBlade":
        //                            OnParametricResultReceived(Value);
        //                            break;
        //                        default:
        //                            OnBladeEventReceived(Name, SubName, Value);
        //                            break;
        //                    }
        //                }
        //                else
        //                {
        //                    logger.Warn("Unknown event format '{0}'", e.Text.Trim());
        //                }
        //            }
        //            break;

        //        case eventInts.StatisticsMinMaxEvent: break;
        //        case eventInts.KeepAliveEvent: break;
        //        default:
        //            throw new NotSupportedException(string.Format(
        //                "Status blade event with type={0} is not supported.", ((eventInts)e.EventType).ToString()));
        //    }
        //}
        #endregion internal Methods
    }
}
