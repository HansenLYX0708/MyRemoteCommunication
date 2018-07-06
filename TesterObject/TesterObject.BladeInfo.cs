using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Windows.Forms;

using Hitachi.Tester.Enums;
using HGST.Blades;

namespace Hitachi.Tester.Module
{
    public partial class TesterObject : ITesterObject
    {
        #region Fields
        private string _MyLocation;
        private string _JadeSn;
        private string _MemsSn;
        private string _DiskSn;
        private string _PcbaSn;
        private string _MotorBaseSn;
        private string _ActuatorSn;
        private string _BladeSn;
        private string _FlexSn;
        private string _MotorSn;

        private HGST.Blades.MemsStateValues _MemsStatus;
        private readonly object _MemsStatusLockObj;
        private string _BladeType;

        private int _OpenDelay;
        private int _CloseDelay;
        private string _MemsOpenDelay;
        private string _MemsCloseDelay;

        private bool _VoltageCheckBlade;
        private bool _VoltageCheckThreadGoing;

        // Bunny card fields start
        internal volatile CountsStatsClass _CountStateFromDisk;

        private bool _ConstructorFinished;
        private volatile bool _ResetGoing;
        private object _UsbResetLockObject;
        private bool _ResetStatus;

        private bool _UpdatedStatus;
        private bool _ReadBunnyStatusBusy;
        private int _FormerStatusRead;
        private int _FormerStatusRead2;
        private object _ReadBunnyStatusAndUpdateFlagsLockObject;

        private double _FiveVoltSupplyNoLoad;
        private double _FiveVoltSupplyLoaded;
        private double _FiveVoltSwitched;
        private double _TwelveVoltSupplyNoLoad;
        private double _TwelveVoltSupplyLoaded;
        private double _TwelveVoltSwitched;

        private bool _In5vNoLoadOk;
        private bool _In5vLoadOk;
        private bool _In5vSwOk;
        private bool _In12vNoLoadOk;
        private bool _In12vLoadOk;
        private bool _In12vSwOk;

        private readonly double _In5vMin;
        private readonly double _In5vMax;
        private readonly double _In12vMin;
        private readonly double _In12vMax;
        private readonly double _InLoad5vMin;
        private readonly double _InLoad5vMax;
        private readonly double _InLoad12vMin;
        private readonly double _InLoad12vMax;
        private readonly double _Sw5vMin;
        private readonly double _Sw5vMax;
        private readonly double _Sw12vMin;
        private readonly double _Sw12vMax;

        private double _FiveVoltSupplyCalFactor;
        private double _TwelveVoltSupplyCalFactor;
        private double _FiveVoltSwitchedCalFactor;
        private double _TwelveVoltSwitchedCalFactor;

        private bool _NeedToReLoadAllValues;

        private HGST.Blades.MemsStateValues _MemsRequestedState;
        private readonly object _RequestedLockObj;

        private string _FirmwareRev;
        private string _DriverRev;
        // Bunny card fields end
        private object _ReadInCountsLockObject;

        private System.Threading.Timer memsOpenWatchdogTimer;
        private System.Threading.TimerCallback watchdogDelegate;
        DateTime m_MemsOpenClosedStartTime;
        private ServoPositionClass openPosition;
        private ServoPositionClass closePosition;
        private volatile bool bMemsThreadGoing;
        private double verNumDriver = -1.0;
        private double verNumFirm = -1.0;

        private bool m_ServoArrived;
        private HGST.Blades.MemsStateValues m_ServoMemsState;

        private delegate bool boolFiveIntsDelegate(int value1, int value2, int value3, int value4, int value5);

        private int servoMoveCloseCallbackRetries;
        private int servoMoveOpenCallbackRetries;
        private int hgstGetServoRetryCount;
        private int hgstSetSaveServoRetries = 0;
        private int hgstGetNeutralRetryCount = 0;
        #endregion Fields

        #region Properties
        public string Name()
        {
            return "FACT BladeRunner";
        }

        public string ActuatorSN
        {
            get
            {
                if (_ActuatorSn.Length == 0)
                {
                    try
                    {
                        _ActuatorSn = ReadDataFiles(Constants.ActuatorTXT).Trim();
                    }
                    catch
                    {
                        string message;
                        try
                        {
                            message = StaticServerTalker.getCurrentCultureString("FileNotFound");
                        }
                        catch
                        {
                            message = "FileNotFound";
                        }
                        SendBunnyEvent(this, new StatusEventArgs(message, (int)BunnyEvents.ActuatorSN));
                    }
                }
                return _ActuatorSn;
            }
            set
            {
                WriteDataFiles(Constants.ActuatorTXT, value.Trim());
                _ActuatorSn = value.Trim();
            }
        }

        public string BladeSN
        {
            get
            {
                if (_BladeSn.Length == 0)
                {
                    try
                    {
                        _BladeSn = ReadDataFiles(Constants.BladeSNTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.BladeSN));
                    }
                }
                return _BladeSn;
            }
            set
            {
                WriteDataFiles(Constants.BladeSNTXT, value.Trim());
                _BladeSn = value.Trim();
            }
        }

        public string FlexSN
        {
            get
            {
                if (_FlexSn.Length == 0)
                {
                    try
                    {
                        _FlexSn = ReadDataFiles(Constants.FlexSNTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.FlexSN));
                    }
                }
                return _FlexSn;
            }
            set
            {
                WriteDataFiles(Constants.FlexSNTXT, value.Trim());
                _FlexSn = value.Trim();
            }
        }

        public string DiskSn
        {
            get
            {
                if (_DiskSn.Length == 0)
                {
                    try
                    {
                        _DiskSn = ReadDataFiles(Constants.DiskSNTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.DiskSN));
                    }
                }
                return _DiskSn;
            }
            set
            {
                WriteDataFiles(Constants.DiskSNTXT, value.Trim());
                _DiskSn = value.Trim();
            }
        }

        public string MemsSn
        {
            get
            {
                if (_MemsSn.Length == 0)
                {
                    try
                    {
                        _MemsSn = ReadDataFiles(Constants.MemsSNTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MemsSN));
                    }
                }
                return _MemsSn;
            }
            set
            {
                WriteDataFiles(Constants.MemsSNTXT, value.Trim());
                _MemsSn = value.Trim();
            }
        }

        public string MotorBaseSn
        {
            get
            {
                if (_MotorBaseSn.Length == 0)
                {
                    try
                    {
                        _MotorBaseSn = ReadDataFiles(Constants.MotorBaseplateTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MotorBaseSN));
                    }
                }
                return _MotorBaseSn;
            }
            set
            {
                WriteDataFiles(Constants.MotorBaseplateTXT, value.Trim());
                _MotorBaseSn = value.Trim();
            }
        }

        public string MotorSN
        {
            get
            {
                if (_MotorSn.Length == 0)
                {
                    try
                    {
                        _MotorSn = ReadDataFiles(Constants.MotorSNTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MotorSN));
                    }
                }
                return _MotorSn;
            }
            set
            {
                WriteDataFiles(Constants.MotorSNTXT, value.Trim());
                _MotorSn = value.Trim();
            }
        }

        public string PcbaSn
        {
            get
            {
                if (_PcbaSn.Length == 0)
                {
                    try
                    {
                        _PcbaSn = ReadDataFiles(Constants.PcbaSNTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.PcbaSN));
                    }
                }
                return _PcbaSn;
            }
            set
            {
                WriteDataFiles(Constants.PcbaSNTXT, value.Trim());
                _PcbaSn = value.Trim();
            }
        }

        public string JadeSN
        {
            get
            {

                if (_JadeSn.Length == 0)
                {
                    try
                    {
                        _JadeSn = ReadDataFiles(Constants.JadeSNTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.JadeSN));
                    }
                }
                return _JadeSn;
            }
            set
            {
                WriteDataFiles(Constants.JadeSNTXT, value.Trim());
                _JadeSn = value.Trim();
            }
        }

        /// <summary>
        /// Get Set current MEMS state with lock for thread problems.
        /// </summary>
        private HGST.Blades.MemsStateValues MemsStatus
        {
            get
            {
                lock (_MemsStatusLockObj)
                {
                    return _MemsStatus;
                }
            }
            set
            {
                lock (_MemsStatusLockObj)
                {
                    _MemsStatus = value;
                }
            }
        }

        public string BladeType
        {
            get
            {
                if (_BladeType.Length == 0)
                {
                    try
                    {
                        _BladeType = ReadDataFiles(Constants.BladeCapabilityTXT).Trim();
                        IsThisVoltageCheckBlade();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.BladeType));
                    }
                }
                return _BladeType;
            }
            set
            {
                WriteDataFiles(Constants.BladeCapabilityTXT, value.Trim());
                _BladeType = value.Trim();
                IsThisVoltageCheckBlade();
            }
        }

        public string MyLocation
        {
            get
            {
                if (_MyLocation.Length == 0 || _MyLocation.Trim().ToLower().Contains("none"))
                {
                    try
                    {
                        _MyLocation = ReadDataFiles(Constants.BladeLocTXT).Trim();
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.BladeLoc));
                    }
                }
                return _MyLocation;
            }
            set
            {
                _MyLocation = value;
            }
        }

        private int OpenDelay
        {
            get
            {
                return _OpenDelay;
            }
            set
            {
                // TODO : MemsOpenDelay = value.ToString();
            }
        }

        private int CloseDelay
        {
            get
            {
                return _CloseDelay;
            }

            set
            {
                // TODO : MemsCloseDelay = value.ToString();
            }
        }

        public string MemsOpenDelay
        {
            get
            {
                if (_MemsOpenDelay.Length == 0)
                {
                    try
                    {
                        try
                        {
                            _MemsOpenDelay = ReadDataFiles(Constants.MemsOpenDelayTXT).Trim();
                        }
                        catch
                        {
                            _MemsOpenDelay = "1500";
                            MemsOpenDelay = _MemsOpenDelay;
                            // pops exception if file still does not work.
                            _MemsOpenDelay = ReadDataFiles(Constants.MemsOpenDelayTXT).Trim();
                        }
                        _OpenDelay = ParseMemsDelay(ref _MemsOpenDelay);
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MemsOpenDelay));
                    }
                }
                return _MemsOpenDelay;
            }
            set
            {
                int intValue = ParseMemsDelay(ref value);
                _OpenDelay = intValue;
                _MemsOpenDelay = value.Trim();
                WriteDataFiles(Constants.MemsOpenDelayTXT, _MemsOpenDelay);
            }
        }

        public string MemsCloseDelay
        {
            get
            {
                if (_MemsCloseDelay.Length == 0)
                {
                    try
                    {
                        try
                        {
                            _MemsCloseDelay = ReadDataFiles(Constants.MemsCloseDelayTXT).Trim();
                        }
                        catch
                        {
                            _MemsCloseDelay = "1500";
                            // TODO : close = open?????????
                            MemsCloseDelay = _MemsOpenDelay;
                            // pops exception if file still does not work.
                            _MemsCloseDelay = ReadDataFiles(Constants.MemsCloseDelayTXT).Trim();
                        }
                        _CloseDelay = ParseMemsDelay(ref _MemsCloseDelay);
                    }
                    catch
                    {
                        SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MemsCloseDelay));
                    }
                }
                return _MemsCloseDelay;
            }
            set
            {
                int intValue = ParseMemsDelay(ref value);
                _CloseDelay = intValue;
                _MemsCloseDelay = value.Trim();
                WriteDataFiles(Constants.MemsCloseDelayTXT, _MemsCloseDelay);
            }
        }

        /// <summary>
        /// Get Set MEMS requested state with lock for thread problems.
        /// </summary>
        private HGST.Blades.MemsStateValues MemsRequestedState
        {
            get
            {
                lock (_RequestedLockObj)
                {
                    return _MemsRequestedState;
                }
            }
            set
            {
                lock (_RequestedLockObj)
                {
                    _MemsRequestedState = value;
                }
            }
        }

        private string FirmwareRev
        {
            get
            {
                return _FirmwareRev;
            }
            set
            {
                if (_FirmwareRev.Length == 0 || _FirmwareRev == "OffLine")
                {
                    ReadInCountsdata();
                }
                _FirmwareRev = value;
            }
        }
        #endregion Properties

        #region ITesterObject Methods
        /// <summary>
        /// Get one or more blade strings.
        /// Appends result for each item in names[] to returned string[].
        /// </summary>
        /// <param name="key"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public string[] GetStrings(string key, string[] names)
        {
            List<string> retList = new List<string>();

            StringBuilder sb = new StringBuilder();

            foreach (string aName in names)
            {
                // append to sb for WriteLineContent data.
                sb.Append(string.Format("GetStrings called for {0}", aName));

                switch (aName)
                {
                    case BladeDataName.ActuatorSN:
                        try
                        {
                            retList.Add(ActuatorSN);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.BladeSN:
                        try
                        {
                            retList.Add(BladeSN);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.DiskSN:
                        try
                        {
                            retList.Add(DiskSn);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.FlexSN:
                        try
                        {
                            retList.Add(FlexSN);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.MemsSN:
                        try
                        {
                            retList.Add(MemsSn);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.MotorBaseplateSN:
                        try
                        {
                            retList.Add(MotorBaseSn);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.MotorSN:
                        try
                        {
                            retList.Add(MotorSN);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.PcbaSN:
                        try
                        {
                            retList.Add(PcbaSn);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.BladeType:
                        try
                        {
                            retList.Add(BladeType);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.MemsOpenDelay:
                        try
                        {
                            retList.Add(MemsOpenDelay);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.MemsCloseDelay:
                        try
                        {
                            retList.Add(MemsCloseDelay);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.JadeSN:
                        try
                        {
                            retList.Add(JadeSN);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.BladeLoc:
                        try
                        {
                            retList.Add(MyLocation);
                        }
                        catch
                        {
                            retList.Add("File not found");
                        }
                        break;
                    case BladeDataName.Counts:
                        try
                        {
                            string ownerStr = "";
                            try
                            {
                                ownerStr = ReadDataFiles(Constants.BladeSNTXT);
                            }
                            catch
                            {
                                ownerStr = "NONE";
                            }
                            if (_CountStateFromDisk.OwnerSerialNumber != ownerStr)
                            {
                                ReadInCountsdata();
                                _CountStateFromDisk.OwnerSerialNumber = ownerStr;
                            }
                        }
                        catch
                        {
                            // if still broke, then just give back what we already have.
                        }
                        retList.Add(_CountStateFromDisk.ToString());
                        break;
                    case BladeDataName.VoltageCheckBlade:
                        string valueString = MakeVoltageBladeEventString();
                        retList.Add(valueString);
                        break;
                    // TODO : TCL  case BladeDataName.TclPath:
                    //    retList.Add(TclPath);
                    //    break;
                    case BladeDataName.BladePath:
                        retList.Add(BladePath);
                        break;
                    case BladeDataName.FactPath:
                        retList.Add(FactPath);
                        break;
                    case BladeDataName.GradePath:
                        retList.Add(GradePath);
                        break;
                    case BladeDataName.FirmwarePath:
                        retList.Add(FirmwarePath);
                        break;
                    case BladeDataName.ResultPath:
                        retList.Add(ResultPath);
                        break;
                    case BladeDataName.LogPath:
                        retList.Add(LogPath);
                        break;
                    case BladeDataName.DebugPath:
                        retList.Add(DebugPath);
                        break;
                    // TODO : TCL     case BladeDataName.TclStart:
                    //    retList.Add(TclStart);
                    //    break;
                    case BladeDataName.CountsPath:
                        retList.Add(CountsPath);
                        break;
                    case BladeDataName.BladeRunnerPath:
                        retList.Add(AppDomain.CurrentDomain.BaseDirectory);
                        break;

                    //case BladeDataName.FwRev:
                    //    {
                    //        BunnyCount += 1;
                    //        WriteLine("Firmware revision called " + BunnyCount.ToString());
                    //        firmwareRev = this.uploadFirmwareVersion();
                    //        WriteLineContent(firmwareRev.ToString());
                    //        retList.Add(firmwareRev);
                    //        break;
                    //    }

                    //case BladeDataName.DriverVer:
                    //    {
                    //        BunnyCount += 1;
                    //        WriteLine("OCX version called " + BunnyCount.ToString());
                    //        driverRev = this.uploadDriverVersion();
                    //        WriteLineContent(driverRev.ToString());
                    //        retList.Add(driverRev);
                    //        break;
                    //    }

                    default:
                        retList.Add("");
                        break;
                } // end switch

                if (retList.Count > 0)
                {
                    sb.Append(string.Format(" {0}.", retList[retList.Count - 1]));
                }
            } // end foreach
            WriteLineContent(string.Format("TesterObject::GetString [string:{0}] [retListcount: {1}]", sb.ToString(), retList.Count));
            WriteLineContent("TesterObject::GetString [string:" + sb.ToString() + "]");

            return retList.ToArray();
        }

        public void SetStrings(string key, string[] names, string[] strings)
        {
            object[] passingObjAray = new object[] { key, names, strings };
            Thread setStringsThread = new Thread(new ParameterizedThreadStart(SetStringsThreadFunc))
            {
                IsBackground = true
            };
            setStringsThread.Start(passingObjAray);
        }

        public int[] GetIntegers(string key, string[] names)
        {
            List<int> retList = new List<int>();

            StringBuilder sb = new StringBuilder();

            foreach (string aName in names)
            {
                sb.Append(string.Format("GetIntegers called for {0}", aName));

                int tmpStatus = 0;
                switch (aName)
                {
                    case BladeDataName.MemsCount:
                    case BladeDataName.PatrolCount:
                    case BladeDataName.ScanCount:
                    case BladeDataName.TestCount:
                    case BladeDataName.DiskLoadCount:
                        retList.Add(_CountStateFromDisk.GetValue(aName));
                        continue;

                    case BladeDataName.Ramp:
                        // TODO : retList.Add(ReadRampValue());
                        continue;

                    case BladeDataName.BunnyStatus:
                        retList.Add(ReadBunnyStatusAndUpdateFlags());
                        continue;
                    case BladeDataName.Solenoid:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.SOLENOID) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.Aux12VDC:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.AUX_12VDC) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.Aux5VDC:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.AUX_5VDC) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.AuxIn0:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.AUX_IN0) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.AuxIn1:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.AUX_IN1) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.AuxOut0:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.AUX_OUT0) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.AuxOut1:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.AUX_OUT1) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.BackLight:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        retList.Add((tmpStatus & (int)HGST.Blades.EnumBunnyStatusBits.BACKLIGHT) > 0 ? 1 : 0);
                        break;
                    case BladeDataName.MemsState:
                        // TODO : retList.Add((int)GetMemsState());
                        break;
                    case BladeDataName.CardPower:
                        tmpStatus = ReadBunnyStatusAndUpdateFlags();
                        bool pwrOn = (tmpStatus & ((int)HGST.Blades.EnumBunnyStatusBits.AUX_12VDC | (int)HGST.Blades.EnumBunnyStatusBits.AUX_5VDC)) == ((int)HGST.Blades.EnumBunnyStatusBits.AUX_12VDC | (int)HGST.Blades.EnumBunnyStatusBits.AUX_5VDC);
                        retList.Add(pwrOn ? 1 : 0);
                        break;
                    default:
                        retList.Add(-1);
                        continue;
                } // end switch

                if (retList.Count > 0) sb.Append(retList[retList.Count - 1].ToString());

            } // end for

            WriteLineContent(sb.ToString());

            return retList.ToArray();
        }

        public void SetIntegers(string key, string[] names, int[] numbers)
        {
            object[] passingObjAray = new object[] { (object)key, (object)names, (object)numbers };
            Thread setIntsThread = new Thread(new ParameterizedThreadStart(SetIntegersThreadFunc))
            {
                IsBackground = true
            };
            setIntsThread.Start((object)passingObjAray);
        }

        /// <summary>
        /// Set Mems to opposite state.
        /// </summary>
        public void PinMotionToggle()
        {
            // TODO :
            //if (MemsState == HGST.Blades.MemsStateValues.Opened || MemsState == HGST.Blades.MemsStateValues.Unknown)
            //{
            //    OpenCloseMems(0); // close it
            //}
            //else if (MemsState == HGST.Blades.MemsStateValues.Closed)
            //{
            //    OpenCloseMems(1); // open it
            //}
            // else if memsState == MemsStateValues.Changing, we do nothing.
        }

        /// <summary>
        /// Calls BunnyLib hgst_get_servo and returns answer in out parameter.
        /// Also returns answer async in Bunny event.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dev"></param>
        private void hgst_get_servo(int index, int dev, out int i_position)
        {
            i_position = -1;
            if (!CheckIfServoIsOK(true)) return;
            WriteLine("hgst_get_servo called ");
            bool status = false;

            if (_BunnyCard != null)
            {
                status = _BunnyCard.GetServoPosition(dev, ref i_position);
            }
            else
            {
                status = false;
                i_position = -1;
            }
            if (_Exit) return;


            // if recorded position not valid, then read in position (only happens first time).
            if (openPosition.position == 0 || openPosition.velocity == 0 || openPosition.acceleration == 0)
            {
                ServoPositionClass currentPosition = new ServoPositionClass();

                if (_BunnyCard != null) _BunnyCard.SetSaveServo((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoSaveTypes.READEEPROM,
                    ref openPosition.position, ref openPosition.velocity, ref openPosition.acceleration,
                    ref closePosition.position, ref closePosition.velocity, ref closePosition.acceleration,
                    ref currentPosition.position, ref currentPosition.position, ref currentPosition.position);
            }

            if (i_position == openPosition.position)
            {
                m_ServoMemsState = HGST.Blades.MemsStateValues.Opened;
            }
            else if (i_position == closePosition.position)
            {
                m_ServoMemsState = HGST.Blades.MemsStateValues.Closed;
            }
            else
            {
                m_ServoMemsState = HGST.Blades.MemsStateValues.Unknown;
            }

            NotifyWorldBunnyStatus(status, "hgst_get_servo");
            WriteLineContent(index.ToString() + " " + dev.ToString() + " " + i_position.ToString());

            if (status)
            {
                SendBunnyEvent(this, new StatusEventArgs(i_position.ToString(), (int)BunnyEvents.Position));
            }
        }

        /// <summary>
        /// For servo pin motion to enable, disable, open, or close.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dev"></param>
        /// <param name="type"></param>
        /// <param name="end_pos"></param>
        /// <param name="max_vel"></param>
        /// <param name="accel"></param>
        public void hgst_move_servo(int index, int dev, int type, int end_pos, int max_vel, int accel)
        {
            if (!CheckIfServoIsOK(false)) return;

            WriteLine("hgst_move_servo called ");
            WriteLineContent(index.ToString() + " " + dev.ToString() + " " + type.ToString() + " " +
              end_pos.ToString() + " " + max_vel.ToString() + " " + accel.ToString());
            bool status = true;

            // If not moving (enable or disable).
            if ((int)EnumServoTypeActions.CLOSE != type && (int)EnumServoTypeActions.OPEN != type)
            {
                if (_BunnyCard != null)
                {
                    // Enable or disable servo.
                    status = _BunnyCard.MoveServo((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, type, end_pos, max_vel, accel);
                }
                else
                {
                    status = false;
                }
                NotifyWorldBunnyStatus(status, "hgst_move_servo");
                if (!status)
                {
                    return; // broke
                }
                if ((int)EnumServoTypeActions.SERVOON == type)
                {
                    SendBunnyEvent(this, new StatusEventArgs(((int)EnableDisable.Enable).ToString(), (int)BunnyEvents.ServoEnable));
                    _TesterState.ServoEnabled = true;
                    return;
                }
                if ((int)EnumServoTypeActions.SERVOOFF == type)
                {
                    SendBunnyEvent(this, new StatusEventArgs(((int)EnableDisable.Disable).ToString(), (int)BunnyEvents.ServoEnable));
                    _TesterState.ServoEnabled = false;
                    return;
                }
            }

            if ((int)EnumServoTypeActions.CLOSE == type)
            {
                m_ServoArrived = false;  // not there yet.
                boolFiveIntsDelegate servoMoveDelegate = new boolFiveIntsDelegate(_BunnyCard.MoveServo);
                servoMoveDelegate.BeginInvoke((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, type, end_pos, max_vel, accel,
                   new AsyncCallback(ServoMoveCloseCallback), new object[] { servoMoveDelegate, index, dev, type, end_pos, max_vel, accel });
                return;
            }
            if ((int)EnumServoTypeActions.OPEN == type)
            {
                m_ServoArrived = false;  // not there yet.
                boolFiveIntsDelegate servoMoveDelegate = new boolFiveIntsDelegate(_BunnyCard.MoveServo);
                servoMoveDelegate.BeginInvoke((int)EnumSolenoidServoAddr.SERVO, type, end_pos, max_vel, accel,
                   new AsyncCallback(ServoMoveOpenCallback), new object[] { servoMoveDelegate, index, dev, type, end_pos, max_vel, accel });
                return;
            }
        }

        /// <summary>
        /// Calls BunnyLib hgst_get_servo and returns answer async in Bunny event.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dev"></param>
        public void hgst_get_servo(int index, int dev)
        {
            if (!CheckIfServoIsOK(true)) return;
            WriteLine("hgst_get_servo called ");
            bool status = false;
            int i_position = 0;

            while (hgstGetServoRetryCount < Constants.BunnyRetryLimit)
            {

                i_position = 0;
                if (_BunnyCard != null)
                {
                    status = _BunnyCard.GetServoPosition(dev, ref i_position);
                }
                else
                {
                    status = false;
                }
                if (status)
                {
                    break;
                }
                else
                {
                    Interlocked.Increment(ref hgstGetServoRetryCount);
                    Application.DoEvents();
                    Thread.Sleep(500);
                    if (_Exit) return;
                }
            }

            // if recorded position not valid, then read in position (only happens first time).
            if (openPosition.position == 0 || openPosition.velocity == 0 || openPosition.acceleration == 0)
            {
                ServoPositionClass currentPosition = new ServoPositionClass();

                if (_BunnyCard != null) _BunnyCard.SetSaveServo((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoSaveTypes.READEEPROM,
                    ref openPosition.position, ref openPosition.velocity, ref openPosition.acceleration,
                    ref closePosition.position, ref closePosition.velocity, ref closePosition.acceleration,
                    ref currentPosition.position, ref currentPosition.position, ref currentPosition.position);
            }

            if (i_position == openPosition.position)
            {
                m_ServoMemsState = HGST.Blades.MemsStateValues.Opened;
            }
            else if (i_position == closePosition.position)
            {
                m_ServoMemsState = HGST.Blades.MemsStateValues.Closed;
            }
            else
            {
                m_ServoMemsState = HGST.Blades.MemsStateValues.Unknown;
            }

            hgstGetServoRetryCount = 0;
            NotifyWorldBunnyStatus(status, "hgst_get_servo");
            WriteLineContent("hgst_get_servo called " + index.ToString() + " " + dev.ToString() + " " + i_position.ToString());

            if (status)
            {
                SendBunnyEvent(this, new StatusEventArgs(i_position.ToString(), (int)BunnyEvents.Position));
            }
        }

        /// <summary>
        /// GetDataViaEvent reads item and sends result via BunnyEvent.
        /// </summary>
        /// <param name="names"></param>
        public void GetDataViaEvent(string[] names)
        {
            object passingObj = names;
            Thread getDataThread = new Thread(new ParameterizedThreadStart(GetDataViaEventThreadFunc))
            {
                IsBackground = true
            };
            getDataThread.Start(passingObj);
        }

        public void hgst_set_save_servo(int index, int dev, int type,
           int open_end_pos, int open_max_vel, int open_accel,
           int close_end_pos, int close_max_vel, int close_accel,
           int current_end_pos, int current_max_vel, int current_accel)
        {
            if (!CheckIfServoIsOK(true)) return;

            WriteLine("hgst_set_save_servo called ");
            WriteLineContent("hgst_set_save_servo called " + index.ToString() + " " + dev.ToString() + " " + type.ToString() + " " +
              open_end_pos.ToString() + " " + open_max_vel.ToString() + " " + open_accel.ToString() + " " +
              close_end_pos.ToString() + " " + close_max_vel.ToString() + " " + close_accel.ToString() + " " +
              current_end_pos.ToString() + " " + current_max_vel.ToString() + " " + current_accel.ToString());

            bool status = false;

            while (hgstSetSaveServoRetries < Constants.BunnyRetryLimit)
            {

                if (_BunnyCard != null)
                {
                    status = _BunnyCard.SetSaveServo((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, type,
                       ref open_end_pos, ref open_max_vel, ref open_accel,
                       ref close_end_pos, ref close_max_vel, ref close_accel,
                       ref current_end_pos, ref current_max_vel, ref current_accel);
                }
                else
                {
                    status = false;
                }
                if (!status)
                {
                    Interlocked.Increment(ref hgstSetSaveServoRetries);
                    Thread.Sleep(500);
                    if (_Exit) return;

                }
                else
                {
                    break;
                }
            }

            hgstSetSaveServoRetries = 0;
            NotifyWorldBunnyStatus(status, "hgst_set_save_servo");
            if (status)
            {
                openPosition.position = open_end_pos;
                openPosition.velocity = open_max_vel;
                openPosition.acceleration = open_accel;

                closePosition.position = close_end_pos;
                closePosition.velocity = close_max_vel;
                closePosition.acceleration = close_accel;

                SendBunnyEvent(this, new StatusEventArgs(
                   open_end_pos.ToString() + ", " + open_max_vel.ToString() + ", " + open_accel.ToString() + ", " +
                   close_end_pos.ToString() + ", " + close_max_vel.ToString() + ", " + close_accel.ToString() + ", " +
                   current_end_pos.ToString() + ", " + current_max_vel.ToString() + ", " + current_accel.ToString(),
                   (int)BunnyEvents.SetSaveServo));
            }
        }

        public void hgst_get_neutral(int index, int dev)
        {
            if (!CheckIfServoIsOK(true)) return;

            WriteLine("hgst_get_neutral called ");
            WriteLineContent("hgst_get_neutral called " + index.ToString() + " " + dev.ToString());
            bool status = false;
            int i_neutral = 0;

            while (hgstGetNeutralRetryCount < Constants.BunnyRetryLimit)
            {
                i_neutral = 0;
                if (_BunnyCard != null)
                {
                    status = _BunnyCard.GetNeutral((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, ref i_neutral);
                }
                else
                {
                    status = false;
                }
                if (!status)
                {
                    Interlocked.Increment(ref hgstGetNeutralRetryCount);
                    Application.DoEvents();
                    Thread.Sleep(500);
                    if (_Exit) return;
                }
                else
                {
                    break;
                }
            }
            hgstGetNeutralRetryCount = 0;
            NotifyWorldBunnyStatus(status, "hgst_get_neutral");

            if (status)
            {
                SendBunnyEvent(this, new StatusEventArgs(i_neutral.ToString(), (int)BunnyEvents.Neutral));
            }
        }

        #endregion ITesterObject Methods

        #region Support Methods
        private void SimpleBladeInfoInit()
        {
            _NeedToReLoadAllValues = true;
            _ConstructorFinished = false;
            _ResetGoing = false;
            _UsbResetLockObject = new object();
            _ResetStatus = false;
            _Boards = BunnyBoard.Manager.Devices;

            try
            {
                _BunnyCard = _Boards[0];
                if (!_BunnyCard.Connected)
                {
                    _BunnyCard.Connect();
                }
                if (!_BunnyCard.Connected)
                {
                    try
                    {
                        StaticServerTalker.MessageString = "Cannot initialize Bunny Library in form1 constructor. " +
                            Environment.NewLine +
                            "Exception while reading data from Blade controller card.";
                    }
                    catch { }
                }
                else
                {
                    _TesterState.BunnyGood = true;
                    BladePath = _BunnyCard.Flash;
                }
            }
            catch (Exception e)
            {
                _TesterState.BunnyGood = false;
                _TesterState.RampInited = false;
                try
                {
                    StaticServerTalker.MessageString = "Bunny initialization failed.  " + "Exception while reading data from Blade controller card." +
                       Environment.NewLine +
                       "Did you install the kernel driver?  " + Environment.NewLine + MakeUpExceptionString(e).ToString();
                }
                catch { }
            }
            _MyLocation = "";
            _JadeSn = "";
            _MemsSn = "";
            _DiskSn = "";
            _PcbaSn = "";
            _MotorBaseSn = "";
            _ActuatorSn = "";
            _BladeSn = "";
            _FlexSn = "";
            _MotorSn = "";
            _OpenDelay = 0;
            _CloseDelay = 0;
            _MemsOpenDelay = "";
            _MemsCloseDelay = "";

            _MemsStatus = HGST.Blades.MemsStateValues.Unknown;
            
            _BladeType = "";
            _VoltageCheckBlade = false;
            _VoltageCheckThreadGoing = false;

            _UpdatedStatus = false;
            _ReadBunnyStatusBusy = false;
            _FormerStatusRead = -1;
            _FormerStatusRead2 = -1;

            _ReadBunnyStatusAndUpdateFlagsLockObject = new object();

            _FiveVoltSupplyNoLoad = 0.0;
            _FiveVoltSupplyLoaded = 0.0;
            _FiveVoltSwitched = 0.0;
            _TwelveVoltSupplyNoLoad = 0.0;
            _TwelveVoltSupplyLoaded = 0.0;
            _TwelveVoltSwitched = 0.0;

            _In5vNoLoadOk = false;
            _In5vLoadOk = false;
            _In5vSwOk = false;
            _In12vNoLoadOk = false;
            _In12vLoadOk = false;
            _In12vSwOk = false;

            _MemsRequestedState = HGST.Blades.MemsStateValues.Closed;

            _FirmwareRev = "";
            _DriverRev = "";

            _ReadInCountsLockObject = new object();

            _CountStateFromDisk = new CountsStatsClass(this.CountsPath, Application.StartupPath);
            // TODO : _CountStateFromDisk Add event
            //_CountStateFromDisk.StatisticsMinMaxEvent += new StatusEventHandler(countState_StatisticsMinMaxEvent);
            //_CountStateFromDisk.StatisticsValueEvent += new StatusEventHandler(countState_StatisticsValueEvent);
            //_CountStateFromDisk.StatisticsFromToTimeEvent += new StatusEventHandler(countState_StatisticsFromToTimeEvent);
            //_CountStateFromDisk.StatisticsDGREvent += new StatusEventHandler(countState_StatisticsDGREvent);

            watchdogDelegate = new TimerCallback(MemsOpenWatchdogTimerCallback);
            memsOpenWatchdogTimer = new System.Threading.Timer(watchdogDelegate, null, Timeout.Infinite, Timeout.Infinite);
            m_MemsOpenClosedStartTime = DateTime.Now;

            openPosition = new ServoPositionClass();
            closePosition = new ServoPositionClass();
            bMemsThreadGoing = false;

            m_ServoMemsState = HGST.Blades.MemsStateValues.Unknown;
            m_ServoArrived = false;
        }

        /// <summary>
        /// Reads blade data files from bunny card
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string ReadDataFiles(string fileName)
        {
            WriteLine("ReadDataFiles " + fileName);
            // TODO : string tmpStr = _BunnyCard.ReadDataFiles(fileName);
            string tmpStr = string.Empty;
            WriteLineContent("ReadDataFiles " + fileName + Environment.NewLine + tmpStr);
            return tmpStr;
        } // end readDataFiles

        /// <summary>
        /// Write Blade data files from bunny card
        /// </summary>
        /// <param name="key"></param>
        /// <param name="serialNumber"></param>
        private void WriteDataFiles(string fileName, string data)
        {
            WriteLine("WriteDataFiles " + fileName);
            WriteLineContent("WriteDataFiles " + fileName + Environment.NewLine + data);
            // TODO : _BunnyCard.WriteDataFiles(fileName, data);
        }

        private void SetStringsThreadFunc(object obj)
        {
            // TODO : SetStringsThreadFunc     throw new NotImplementedException();
        }

        /// <summary>
        /// Checks blade type for voltage check blade and sets or clears flag for such.
        /// </summary>
        private void IsThisVoltageCheckBlade()
        {
            _VoltageCheckBlade = _BladeType.ToLower().Contains(BladeDataName.VoltageCheckBlade.ToLower());

            if (_VoltageCheckBlade && !_VoltageCheckThreadGoing)
            {
                Thread voltageThread = new Thread(VoltageCheckThreadFunction)
                {
                    IsBackground = true,
                    Name = "voltageThread"
                };
                _VoltageCheckThreadGoing = true;
                voltageThread.Start();

                ReadInBcfFile();
            }

            if (!_VoltageCheckBlade && _VoltageCheckThreadGoing)
            {
                _VoltageCheckThreadGoing = false;
            }
        }

        /// <summary>
        /// Runs on a thread when a voltage check blade is inserted.
        /// </summary>
        private void VoltageCheckThreadFunction()
        {
            //return;
            int bitCount = 10;
            int portCount = 4;
            uint count5vSupply = 0;
            uint count12vSupply = 0;
            uint count5VSw = 0;
            uint count12VSw = 0;

            // While Blade there and not exiting
            while (_VoltageCheckThreadGoing && !_Exit)
            {
                try
                {
                    // turn the power on or off.
                    if (!_BunnyCard.CardPower)
                    {
                        _BunnyCard.CardPower = true;
                    }
                    else
                    {
                        _BunnyCard.CardPower = false;
                    }

                    // Tell Blade to start a measurement
                    _BunnyCard.SetAuxOut1(false); // toggle low
                    Thread.Sleep(10);
                    _BunnyCard.SetAuxOut1(true); // toggle high
                    Thread.Sleep(10);

                    DateTime startTime = DateTime.Now;
                    TimeSpan howlong = TimeSpan.FromSeconds(5);
                    ReadBunnyStatusAndUpdateFlagsAbsolute();

                    // Wait for voltage check blade to collect a reading.
                    while (!_TesterState.AuxIn0 && _VoltageCheckThreadGoing && !_Exit)
                    {
                        ReadBunnyStatusAndUpdateFlagsAbsolute();
                        if ((DateTime.Now - startTime) > howlong)
                        {
                            break;
                        }
                    }

                    for (int i = 0; i < portCount; i++)
                    {
                        uint tempValue = 0;

                        // Loop for each bit
                        for (int j = 0; j < bitCount; j++)
                        {
                            if (!_TesterState.AuxIn0 || !_VoltageCheckThreadGoing) break;

                            // Set clock line high (tells voltage check blade to give us a bit).
                            _BunnyCard.SetAuxOut0(true);
                            Thread.Sleep(10);
                            // Voltage check blade will put bit on data line.
                            // Set clock line low (tells voltage check blade that we have this bit).
                            _BunnyCard.SetAuxOut0(false);
                            Thread.Sleep(10);

                            // read bit and put in right bit location.
                            ReadBunnyStatusAndUpdateFlagsAbsolute();
                            tempValue += (uint)((_TesterState.AuxIn1) ? (0x01 << j) : 0);
                        } // end for j = which bit

                        // select which analog value 
                        switch (i)
                        {
                            case 0:
                                count5VSw = tempValue;
                                break;
                            case 1:
                                count5vSupply = tempValue;
                                break;
                            case 2:
                                count12VSw = tempValue;
                                break;
                            case 3:
                                count12vSupply = tempValue;
                                break;
                        } // end switch              
                    } // end for i = which value

                    // Magical DAC numbers
                    // We have a 10 bit DAC.
                    double maxDacCounts = 1023.0;

                    //When the voltages (both 5 and 12) are correct, the DAC will read around a value of around 511 (or half).
                    // One half times the following factors will give 5V or 12V.
                    double twelveVoltFactor = 24.0;  //  (511/1023) * 24 = 12V
                    double fiveVoltFactor = 10.0;    //  (511/1023) * 10 = 5V

                    if (_BunnyCard.CardPower)
                    {
                        _TwelveVoltSupplyLoaded = (((double)count12vSupply) / maxDacCounts) * twelveVoltFactor * _TwelveVoltSupplyCalFactor;
                        _FiveVoltSupplyLoaded = (((double)count5vSupply) / maxDacCounts) * fiveVoltFactor * _FiveVoltSupplyCalFactor;
                        _TwelveVoltSwitched = (((double)count12VSw) / maxDacCounts) * twelveVoltFactor * _TwelveVoltSwitchedCalFactor;
                        _FiveVoltSwitched = (((double)count5VSw) / maxDacCounts) * fiveVoltFactor * _FiveVoltSwitchedCalFactor;
                        if (String.Format("{0:0.00}", _FiveVoltSwitched).Length > 4)
                        {
                            _BunnyCard.LcdScrollText(String.Format("  5V is {0:0.00}", _FiveVoltSwitched));
                        }
                        else
                        {
                            _BunnyCard.LcdScrollText(String.Format("  5V is {0: 0.00}", _FiveVoltSwitched));
                        }
                        if (String.Format("{0:0.00}", _TwelveVoltSwitched).Length > 4)
                        {
                            _BunnyCard.LcdScrollText(String.Format(" 12V is {0:0.00}", _TwelveVoltSwitched));
                        }
                        else
                        {
                            _BunnyCard.LcdScrollText(String.Format(" 12V is {0: 0.00}", _TwelveVoltSwitched));
                        }
                    }
                    else
                    {
                        _TwelveVoltSupplyNoLoad = (((double)count12vSupply) / maxDacCounts) * twelveVoltFactor * _TwelveVoltSupplyCalFactor;
                        _FiveVoltSupplyNoLoad = (((double)count5vSupply) / maxDacCounts) * fiveVoltFactor * _FiveVoltSupplyCalFactor;
                    }

                    // Wait for blade to ACK.
                    bool isOK = true;
                    startTime = DateTime.Now;
                    while (_TesterState.AuxIn0 && _VoltageCheckThreadGoing && !_Exit)
                    {
                        if ((DateTime.Now - startTime) > howlong)
                        {
                            isOK = false;
                            break;
                        }
                    }

                    if (isOK)
                    {
                        // check if in limits or not
                        _In5vNoLoadOk = _FiveVoltSupplyNoLoad >= _In5vMin && _FiveVoltSupplyNoLoad <= _In5vMax;
                        _In5vLoadOk = _FiveVoltSupplyLoaded >= _InLoad5vMin && _FiveVoltSupplyLoaded <= _InLoad5vMax;
                        _In5vSwOk = _FiveVoltSwitched >= _In5vMin && _FiveVoltSwitched <= _In5vMax;

                        _In12vNoLoadOk = _TwelveVoltSupplyNoLoad >= _In12vMin && _TwelveVoltSupplyNoLoad <= _In12vMax;
                        _In12vLoadOk = _TwelveVoltSupplyLoaded >= _InLoad12vMin && _TwelveVoltSupplyLoaded <= _InLoad12vMax;
                        _In12vSwOk = _TwelveVoltSwitched >= _Sw12vMin && _TwelveVoltSwitched <= _Sw12vMax;

                        string valueString = MakeVoltageBladeEventString();
                        SendBunnyEvent(this, new StatusEventArgs(valueString, (int)BunnyEvents.VoltageCheckBlade));
                    }
                    else
                    {
                        string valueString = String.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}",
                            0, "false", 0, "false", 0, "false", 0, "false", 0, "false", 0, "false");
                        SendBunnyEvent(this, new StatusEventArgs(valueString, (int)BunnyEvents.VoltageCheckBlade));
                    }
                } // end try
                catch
                {
                    // just keep thread going until bunny wakes up.
                }
            }
        }

        /// <summary>
        /// Reads in saved BCF values.
        /// </summary>
        private void ReadInBcfFile()
        {
            FileStream fs = null;
            StreamReader sr = null;

            try
            {
                // Read current BCF file.
                fs = new FileStream(System.IO.Path.Combine(BladePath, BladeDataName.VoltageCheckBlade + ".bcf"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                sr = new StreamReader(fs);
                string bcfString = sr.ReadLine();
                sr.Close();
                sr.Dispose();
                sr = null;

                string[] tokens = bcfString.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                int tokenCount = 4;
                if (tokens.Length != tokenCount) throw new Exception("Improperly formatted BCF file.  Wrong token count.");

                _FiveVoltSupplyCalFactor = double.Parse(tokens[0]);
                _TwelveVoltSupplyCalFactor = double.Parse(tokens[1]);
                _FiveVoltSwitchedCalFactor = double.Parse(tokens[2]);
                _TwelveVoltSwitchedCalFactor = double.Parse(tokens[3]);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem reading BCF file. " +
                                Environment.NewLine + ex.Message +
                                Environment.NewLine + "Return blade to maintenance for calibration.",
                    "Error reading BCF file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }
        }

        private int ParseMemsDelay(ref string delayStr)
        {
            if (!int.TryParse(delayStr, out int intValue))
            {
                intValue = 1500;
                delayStr = "1500";
            }
            return intValue;
        }

        /// <summary>
        /// Reads status and waits until async function finishes.
        /// </summary>
        private void ReadBunnyStatusAndUpdateFlagsAbsolute()
        {
            _UpdatedStatus = false;
            ReadBunnyStatusAndUpdateFlags();
            while (!_UpdatedStatus)
            {
                Thread.Sleep(10);
            }
        }

        private int ReadBunnyStatusAndUpdateFlags()
        {
            int bunnyStatus = ReadBunnyStatusAndUpdateFlags(out bool kernelStatus);
            if (bunnyStatus < 0) bunnyStatus = 0;
            return bunnyStatus;
        }

        private int ReadBunnyStatusAndUpdateFlags(out bool kernelStatus)
        {
            if (_ReadBunnyStatusBusy && _FormerStatusRead2 > 0)
            {
                kernelStatus = false;
                return _FormerStatusRead2;
            }

            lock (_ReadBunnyStatusAndUpdateFlagsLockObject)
            {
                try
                {
                    _ReadBunnyStatusBusy = true;
                    int status = -1;  // status bits from bunny card.
                    kernelStatus = false;  // status from driver.
                    if (_BunnyCard != null)
                    {
                        kernelStatus = _BunnyCard.GetDeviceStatus(ref status, true);
                    }
                    if (!kernelStatus || status < 0)
                    {
                        try
                        {
                            UsbReset(0);
                            kernelStatus = _BunnyCard.GetDeviceStatus(ref status, true);
                        }
                        catch
                        {
                            kernelStatus = false;
                        }
                        _FormerStatusRead = -1;
                    }

                    // Remember the real status value returned.
                    int tmpStatus = status;
                    if (status < 0) status = 0; // For a bit set a valid number of nothing.

                    if (_FormerStatusRead != status && kernelStatus)
                    {
                        SendBunnyEvent(this, new StatusEventArgs(status.ToString(), (int)BunnyEvents.BunnyStatus));
                    }
                    _TesterState.B12VDC = ((status & (int)HGST.Blades.EnumBunnyStatusBits.AUX_12VDC) > 0) ? true : false;
                    _TesterState.B5VDC = ((status & (int)HGST.Blades.EnumBunnyStatusBits.AUX_5VDC) > 0) ? true : false;
                    _TesterState.AuxIn0 = ((status & (int)HGST.Blades.EnumBunnyStatusBits.AUX_IN0) > 0) ? true : false;
                    _TesterState.AuxIn1 = ((status & (int)HGST.Blades.EnumBunnyStatusBits.AUX_IN1) > 0) ? true : false;
                    _TesterState.AuxOut0 = ((status & (int)HGST.Blades.EnumBunnyStatusBits.AUX_OUT0) > 0) ? true : false;
                    _TesterState.AuxOut1 = ((status & (int)HGST.Blades.EnumBunnyStatusBits.AUX_OUT1) > 0) ? true : false;
                    _TesterState.BackLight = ((status & (int)HGST.Blades.EnumBunnyStatusBits.BACKLIGHT) > 0) ? true : false;
                    _TesterState.MemsSolenoid = ((status & (int)HGST.Blades.EnumBunnyStatusBits.SOLENOID) > 0) ? true : false;
                    _TesterState.ServoEnabled = ((status & (int)HGST.Blades.EnumBunnyStatusBits.SERVOENABLED) > 0) ? true : false;
                    // Remember this value for next time.
                    _FormerStatusRead = tmpStatus;
                    _FormerStatusRead2 = tmpStatus;
                    return tmpStatus;
                }
                finally
                {
                    _ReadBunnyStatusBusy = false;
                    _UpdatedStatus = true;
                }
            }
        }

        /// <summary>
        /// Read an application setting from the app.config file.
        /// If item not in app.config file then returns default value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private double GetLimitFromAppConfig(string name, double defaultValue)
        {
            double tmpDouble;

            try
            {
                tmpDouble = Double.Parse(System.Configuration.ConfigurationManager.AppSettings[name]);
            }
            catch
            {
                tmpDouble = defaultValue;
            }
            return tmpDouble;
        }

        /// <summary>
        /// Make up string for voltage event
        /// </summary>
        /// <returns></returns>
        private string MakeVoltageBladeEventString()
        {
            string valueString = String.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}",
                _FiveVoltSupplyNoLoad, _In5vNoLoadOk,
                _FiveVoltSupplyLoaded, _In5vLoadOk,
                _FiveVoltSwitched, _In5vSwOk,
                _TwelveVoltSupplyNoLoad, _In12vNoLoadOk,
                _TwelveVoltSupplyLoaded, _In12vLoadOk,
                _TwelveVoltSwitched, _In12vSwOk);
            return valueString;
        }

        /// <summary>
        /// Reset USB.
        /// </summary>
        /// <param name="state"></param>
        private bool UsbReset(int index)
        {
            if (!_ConstructorFinished) return _ResetStatus;

            lock (_UsbResetLockObject)
            {
                int delay = 1000;
                if (_ResetGoing)
                {
                    return _ResetStatus;
                }
                try
                {
                    _ResetGoing = true;

                    ClearAllValues();

                    if (_BunnyCard == null)
                    {
                        _Boards = BunnyBoard.Manager.Devices;
                        if (_Boards.Count > 0)
                        {
                            _BunnyCard = _Boards[0];
                        }
                    }

                    _ResetStatus = false;
                    if (_BunnyCard != null)
                    {
                        try
                        {
                            _BunnyCard.Disconnect();
                            _BunnyCard.Connect();
                            _ResetStatus = _Boards[0].Connected;
                        }
                        catch { }
                    }

                    if (_ResetStatus)
                    {
                        _TesterState.BunnyGood = true;
                    }
                    else
                    {
                        _TesterState.BunnyGood = false;

                        SendBunnyEvent(this, new StatusEventArgs(Constants.OffLine, (int)BunnyEvents.UsbReset));
                        NotifyWorldBunnyStatus(_ResetStatus, "USB reset fail");
                    }

                    if (_BunnyCard != null || !_ResetStatus)
                    {
                        if (!_ResetStatus)
                        {
                            _TesterState.BunnyGood = false;
                            if (_BunnyCard != null) _BunnyCard.Dispose();
                            _BunnyCard = null;

                            if (FirmwareRev.Length > 0)
                            {
                                SendBunnyEvent(this, new StatusEventArgs("", (int)BunnyEvents.FirmwareVer));
                                FirmwareRev = "";
                            }
                            if (_DriverRev.Length > 0)
                            {
                                SendBunnyEvent(this, new StatusEventArgs("", (int)BunnyEvents.DriverVer));
                                _DriverRev = "";
                            }
                        }
                    }
                    if (!_ResetStatus)
                    {
                        Thread.Sleep(delay);  // Give Windows a chance to find the Bunny's Flash drive.
                        Application.DoEvents();
                    }
                }
                finally
                {
                    _ResetGoing = false;
                }
                return _ResetStatus;
            }
        }

        private void ClearAllValues()
        {
            _MemsCloseDelay = _MemsOpenDelay = _MotorSn = _FlexSn = _BladeSn = _ActuatorSn =
                _MotorBaseSn = _PcbaSn = _DiskSn = _MemsSn = _BladeType = _MyLocation = _JadeSn = "";
            _NeedToReLoadAllValues = true;
        }

        /// <summary>
        /// If status is 0 then tells the world that bunny is now OK.
        /// If status not 0 then tells the world that bunny is now sick.
        /// </summary>
        /// <param name="status"></param>
        private void NotifyWorldBunnyStatus(bool status, string whatOperation)
        {
            if (status && !_TesterState.BunnyGood) // function is called before bBunnyGood flag is set.
            {
                _TesterState.BunnyGood = true;
                // SendBunnyEvent has side effect of setting bBunnyGood.
                SendBunnyEvent(this, new StatusEventArgs("Bunny Card operational", (int)BunnyEvents.BunnyFixed));
            }
            else if (!status && _TesterState.BunnyGood)
            {
                _TesterState.BunnyGood = false;
                if (_TesterState.PinMotionType == BunnyPinMotionType.SOLENOID)
                {
                    MemsRequestedState = HGST.Blades.MemsStateValues.Closed;
                }
                SendBunnyEvent(this, new StatusEventArgs(whatOperation, (int)BunnyEvents.Broke));
            }
        }

        /// <summary>
        /// Read from file on local drive (usually C:\counts) or flash drive (usually F:\).
        /// </summary>
        private void ReadInCountsdata()
        {
            lock (_ReadInCountsLockObject)
            {
                if (_CountStateFromDisk == null)
                {
                    _CountStateFromDisk = new CountsStatsClass(CountsPath, Application.StartupPath);
                }

                bool flashWorked = false;
                bool diskWorked = false;
                string realOwner = "";
                string diskOwner = "";

                // try to read counts from hard drive counts dir.
                try
                {
                    _CountStateFromDisk.readInData(System.IO.Path.Combine(CountsPath, Constants.TesterCountsTXT));
                    diskOwner = _CountStateFromDisk.OwnerSerialNumber;
                    diskWorked = true;
                }
                catch
                {
                    diskWorked = false;
                }

                // try to read counts from blade flash
                try
                {
                    realOwner = ReadDataFiles(Constants.BladeSNTXT);
                    _CountStateFromDisk.readInData(System.IO.Path.Combine(BladePath, Constants.TesterCountsTXT));
                    flashWorked = true;
                }
                catch
                {
                    flashWorked = false;
                }


                if (flashWorked && !diskWorked)
                {
                    _CountStateFromDisk.OwnerSerialNumber = realOwner;
                }
                else if (!flashWorked && diskWorked)
                {
                    _CountStateFromDisk.readInData(System.IO.Path.Combine(CountsPath, Constants.TesterCountsTXT));
                    _CountStateFromDisk.OwnerSerialNumber = "NONE";
                }
                else if (flashWorked && diskWorked && diskOwner == realOwner)
                {
                    _CountStateFromDisk.readInData(System.IO.Path.Combine(CountsPath, Constants.TesterCountsTXT));
                }
                else if (flashWorked && diskWorked && diskOwner != realOwner)
                {
                    _CountStateFromDisk.readInData(System.IO.Path.Combine(BladePath, Constants.TesterCountsTXT));
                    _CountStateFromDisk.OwnerSerialNumber = realOwner;
                    WriteOutCountsData();
                }
            } // end lock
        }

        private void SetIntegersThreadFunc(object passingObj)
        {
            try
            {
                object[] passingObjAray = (object[])passingObj;
                //string key = (string)passingObjAray[0];
                string[] names = (string[])passingObjAray[1];
                int[] numbers = (int[])passingObjAray[2];

                bool counterStateUpdated = true;

                for (int i = 0; i < names.Length && i < numbers.Length; i++)
                {
                    WriteLineContent(string.Format("SetIntegers called {0} {1}", names[i], numbers[i]));
                    switch (names[i])
                    {
                        case BladeDataName.MemsCount:
                        case BladeDataName.PatrolCount:
                        case BladeDataName.ScanCount:
                        case BladeDataName.TestCount:
                        case BladeDataName.DiskLoadCount:
                            counterStateUpdated = false;
                            _CountStateFromDisk.SetValue(names[i], numbers[i]);
                            //TODO : SendBunnyCountEvents(names[i]);
                            continue;

                        case BladeDataName.AuxOut0:
                            if (_BunnyCard != null)
                            {
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.Aux0Out));
                                if (_BunnyCard != null) _BunnyCard.SetAuxOut0(numbers[i] > 0);
                            }
                            continue;

                        case BladeDataName.AuxOut1:
                            if (_BunnyCard != null)
                            {
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.Aux1Out));
                                if (_BunnyCard != null) _BunnyCard.SetAuxOut1(numbers[i] > 0);
                            }
                            continue;

                        case BladeDataName.Aux5VDC:
                            if (_BunnyCard != null)
                            {
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.Pwr5V));
                                if (_BunnyCard != null) _BunnyCard.Set5vdc(numbers[i] > 0);
                            }
                            continue;

                        case BladeDataName.Aux12VDC:
                            if (_BunnyCard != null)
                            {
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.Pwr12V));
                                if (_BunnyCard != null) _BunnyCard.Set12vdc(numbers[i] > 0);
                            }
                            continue;

                        case BladeDataName.BackLight:
                            if (_BunnyCard != null)
                            {
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.BackLight));
                                if (_BunnyCard != null) _BunnyCard.SetBackLight(numbers[i] > 0);
                            }
                            continue;

                        case BladeDataName.PinMotion:
                            if (_BunnyCard != null)
                            {
                                WriteLine("PinMotion " + ((numbers[i] != 0) ? "true" : "false"));
                                WriteLineContent(numbers[i].ToString());
                                OpenCloseMems(numbers[i] > 0 ? 1 : 0);
                            }
                            continue;

                        case BladeDataName.CardPower:
                            if (_BunnyCard != null)
                            {
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.Pwr12V));
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.Pwr5V));
                                if (_BunnyCard != null) _BunnyCard.Set12vdc(numbers[i] > 0);
                                if (_BunnyCard != null) _BunnyCard.Set5vdc(numbers[i] > 0);
                            }
                            continue;

                        case BladeDataName.Solenoid:
                            if (_BunnyCard != null)
                            {
                                SendBunnyEvent(this, new StatusEventArgs(numbers[i].ToString(), (int)BunnyEvents.Solenoid));
                                if (_BunnyCard != null) _BunnyCard.SetSolenoid((int)HGST.Blades.EnumSolenoidServoAddr.SOLENOID, numbers[i] > 0);
                            }
                            continue;

                        case BladeDataName.Ramp:
                            if (_BunnyCard != null)
                            {
                                WriteLine("Set RampRate called ");
                                WriteLineContent(numbers[i].ToString());
                                // TODO : SetRampValue(numbers[i]);
                            }
                            continue;
                    } // end switch
                } // end for

                if (!counterStateUpdated)
                {
                    WriteOutCountsData();
                    //counterStateUpdated = true;
                }
            }
            catch
            {
                // TODO : CheckIfBunnyNowIsOK(false, "set integers bunny off ");
            }
        }

        private void OpenCloseMems(int value)
        {
            // If close then stop timer.
            if (value == 0)
            {
                MemsTimerStop();
            }
            else // else open then start timer.
            {
                MemsTimerStart();
            }

            m_MemsOpenClosedStartTime = DateTime.Now;  // MEMS state checking thread uses this for open/close delay time.
            MemsRequestedState = (value == 0) ? HGST.Blades.MemsStateValues.Closed : HGST.Blades.MemsStateValues.Opened;

            string whatType = "";

            // inc counter if open requested and it is now closed
            if (MemsRequestedState == HGST.Blades.MemsStateValues.Opened && MemsStatus == HGST.Blades.MemsStateValues.Closed)
            {
                _CountStateFromDisk.IncValue(BladeDataName.MemsCount);
                string cPath = CountsPath;
                WriteOutCountsData();
                SendBunnyEvent(this, new StatusEventArgs(_CountStateFromDisk.ToString(), (int)BunnyEvents.Counts));
                SendBunnyEvent(this, new StatusEventArgs(_CountStateFromDisk.GetValue(BladeDataName.MemsCount).ToString(), (int)BunnyEvents.MemsCount));
            }

            // if we do not know what kind of pin motion yet then ...
            if (_TesterState.PinMotionType == BunnyPinMotionType.NONE)
            {
                try
                {
                    // read the requested type
                    whatType = ReadDataFiles(Constants.WhichPinMotionFileNameTXT);
                }
                catch (FileNotFoundException)
                {
                    whatType = BunnyPinMotionType.NONE.ToString();
                }
                // is requested type valid?
                if (whatType.Trim() != BunnyPinMotionType.SERVO.ToString() && whatType.Trim() != BunnyPinMotionType.SOLENOID.ToString())
                {
                    // Not valid do nothing
                }
                else if (whatType.Trim() == BunnyPinMotionType.SERVO.ToString())
                {
                    _TesterState.PinMotionType = BunnyPinMotionType.SERVO;
                }
                else if (whatType.Trim() == BunnyPinMotionType.SOLENOID.ToString())
                {
                    _TesterState.PinMotionType = BunnyPinMotionType.SOLENOID;
                }
            }

            // if servo make sure it is ok.
            if (_TesterState.PinMotionType == BunnyPinMotionType.SERVO && !CheckIfServoIsOK(true))
            {
                // Not valid do nothing
            }

            // if servo and open
            if (_TesterState.PinMotionType == BunnyPinMotionType.SERVO && MemsRequestedState == HGST.Blades.MemsStateValues.Opened)
            {
                ServoPositionClass currentPosition = new ServoPositionClass();
                // if recorded position not valid, then read in position (only happens first time).
                if (openPosition.position == 0 || openPosition.velocity == 0 || openPosition.acceleration == 0)
                {

                    if (_BunnyCard != null) _BunnyCard.SetSaveServo((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoSaveTypes.READEEPROM,
                        ref openPosition.position, ref openPosition.velocity, ref openPosition.acceleration,
                        ref closePosition.position, ref closePosition.velocity, ref closePosition.acceleration,
                        ref currentPosition.position, ref currentPosition.position, ref currentPosition.position);
                }
                else // Just get current position
                {
                    hgst_get_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO, out currentPosition.position);
                }

                // If not there then move.
                if (currentPosition.position != openPosition.position)
                {
                    MemsStatus = HGST.Blades.MemsStateValues.Opening;
                    SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                    // Tell servo to open.
                    hgst_move_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoTypeActions.OPEN, 0, 0, 0);
                }
            }

            // if servo and close
            else if (_TesterState.PinMotionType == BunnyPinMotionType.SERVO && MemsRequestedState == HGST.Blades.MemsStateValues.Closed)
            {
                ServoPositionClass currentPosition = new ServoPositionClass();
                // if position not valid, then read in position and accel and vel
                if (closePosition.position == 0 || closePosition.velocity == 0 || closePosition.acceleration == 0)
                {
                    if (_BunnyCard != null) _BunnyCard.SetSaveServo((int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoSaveTypes.READEEPROM,
                       ref openPosition.position, ref openPosition.velocity, ref openPosition.acceleration,
                       ref closePosition.position, ref closePosition.velocity, ref closePosition.acceleration,
                       ref currentPosition.position, ref currentPosition.position, ref currentPosition.position);
                }
                else // Just get current pos
                {
                    hgst_get_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO, out currentPosition.position);
                }

                // If we are not there then move.
                if (currentPosition.position != closePosition.position)
                {
                    MemsStatus = HGST.Blades.MemsStateValues.Closing;
                    SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                    // Tell servo to close.
                    hgst_move_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoTypeActions.CLOSE, 0, 0, 0);
                }
            }
            else if (_TesterState.PinMotionType == BunnyPinMotionType.SOLENOID)
            {
                if (_BunnyCard != null)
                {
                    if (MemsStatus != MemsRequestedState)
                    {
                        if (MemsRequestedState == HGST.Blades.MemsStateValues.Closed)
                        {
                            MemsStatus = HGST.Blades.MemsStateValues.Closing;
                            SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                        }
                        else if (MemsRequestedState == HGST.Blades.MemsStateValues.Opened)
                        {
                            MemsStatus = HGST.Blades.MemsStateValues.Opening;
                            SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                        }
                    }                // Turn solenoid on or off.
                    _BunnyCard.SetSolenoid(0, MemsRequestedState == HGST.Blades.MemsStateValues.Opened ? true : false);
                }
                else
                {
                    // Nothing works.
                    MemsStatus = HGST.Blades.MemsStateValues.Unknown;
                }
                // Set bit per request (even if we could not do it).
                _TesterState.MemsSolenoid = value > 0;
            }

            if (!bMemsThreadGoing)
            {
                Thread openCloseThread = new Thread(new ThreadStart(MemsOpenCloseStateCheckThreadFunc));
                openCloseThread.IsBackground = true;
                openCloseThread.Name = "openCloseThread";
                openCloseThread.Start();
            }

        }

        private void MemsTimerStop()
        {
            memsOpenWatchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void MemsTimerStart()
        {
            // Stop timer
            memsOpenWatchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Find watchdog timeout
            int msFactor = 60 * 1000;
            int timeoutMs;
            try
            {
                // Read from constants (or app.config file).
                timeoutMs = Constants.MemsOpenTimeoutMins * msFactor;
            }
            catch
            {
                // If app config has strange value.
                timeoutMs = 10 * msFactor;
            }
            // Restart timer.
            memsOpenWatchdogTimer.Change(timeoutMs, Timeout.Infinite);
        }

        /// <summary>
        /// Called if MEMS open watchdog timer times out.
        /// </summary>
        private void MemsOpenWatchdogTimerCallback(object state)
        {
            OpenCloseMems(0);
        }

        private bool CheckIfServoIsOK(bool enableIfNeeded)
        {
            // if versions not inited yet then read versions
            if (verNumDriver <= 0.0 || verNumFirm <= 0.0)
            {
                GetDriverVer();
                GetFwRev();
            }

            // Make sure both Bunny card and driver can accept servo.
            if (verNumDriver < Constants.verNumDriver || verNumFirm < Constants.verNumFirm)
            {
                return false;
            }
            else // servo OK
            {
                // See if we need to read status.
                if (_FormerStatusRead == -1)
                {
                    ReadBunnyStatusAndUpdateFlags();
                }
                // Is status now OK?
                if (_FormerStatusRead == -1)
                {
                    // not OK.
                    return false;
                }
                // Is servo not enabled?
                if (!_TesterState.ServoEnabled && enableIfNeeded)
                {
                    // Not enabled so enable servo.
                    hgst_move_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoTypeActions.SERVOON, 0, 0, 0);
                }
                return true;
            }
        }

        private void SleepUntilDelayFinished(int requestedDelay)
        {
            TimeSpan timeused = DateTime.Now - m_MemsOpenClosedStartTime;
            int milliSecondsLeft = requestedDelay - (int)timeused.TotalMilliseconds;
            if (milliSecondsLeft > 10) Thread.Sleep(milliSecondsLeft);
        }
        /// <summary>
        /// This function checks that the mems has arrived and is still there at the requested location.
        /// </summary>
        private void MemsOpenCloseStateCheckThreadFunc()
        {
            bMemsThreadGoing = true;
            // Run until we exit.
            while (!_Exit)
            {
                try
                {
                    //Application.DoEvents();
                    // If needed sleep until timeout expired (if already expired this returns soon).
                    SleepUntilDelayFinished((MemsRequestedState == HGST.Blades.MemsStateValues.Opened) ? OpenDelay : CloseDelay);

                    // If solenoid and open requested.
                    if (_TesterState.PinMotionType == BunnyPinMotionType.SOLENOID &&
                        MemsRequestedState == HGST.Blades.MemsStateValues.Opened &&
                        _TesterState.MemsSolenoid &&
                        MemsOpenSensorOpened() &&
                        !MemsCloseSensorClosed())
                    {
                        if (MemsStatus != HGST.Blades.MemsStateValues.Opened)
                        {
                            MemsStatus = HGST.Blades.MemsStateValues.Opened;
                            SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                        }
                    }
                    // if solenoid and close requested.
                    else if (_TesterState.PinMotionType == BunnyPinMotionType.SOLENOID &&
                             MemsRequestedState == HGST.Blades.MemsStateValues.Closed &&
                             !_TesterState.MemsSolenoid &&
                             !MemsOpenSensorOpened() &&
                             MemsCloseSensorClosed())
                    {
                        if (MemsStatus != HGST.Blades.MemsStateValues.Closed)
                        {
                            MemsStatus = HGST.Blades.MemsStateValues.Closed;
                            SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                        }
                    }
                    // If servo and open requested.
                    else if (_TesterState.PinMotionType == BunnyPinMotionType.SERVO &&
                             MemsRequestedState == HGST.Blades.MemsStateValues.Opened &&
                             m_ServoMemsState == MemsRequestedState &&
                             m_ServoArrived &&
                             MemsOpenSensorOpened() &&
                             !MemsCloseSensorClosed())
                    {
                        if (MemsStatus != HGST.Blades.MemsStateValues.Opened)
                        {
                            MemsStatus = HGST.Blades.MemsStateValues.Opened;
                            SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                        }
                    }
                    // If servo and close requested.
                    else if (_TesterState.PinMotionType == BunnyPinMotionType.SERVO &&
                             MemsRequestedState == HGST.Blades.MemsStateValues.Closed &&
                             m_ServoMemsState == MemsRequestedState &&
                             m_ServoArrived &&
                             !MemsOpenSensorOpened() &&
                             MemsCloseSensorClosed())
                    {
                        if (MemsStatus != HGST.Blades.MemsStateValues.Closed)
                        {
                            MemsStatus = HGST.Blades.MemsStateValues.Closed;
                            SendBunnyEvent(this, new StatusEventArgs(MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                        }
                    }

                    Thread.Sleep(25);  // Give the processor a rest.
                    if (MemsStatus == MemsRequestedState) Thread.Sleep(75);
                } // end inner try
                catch { }  // We are running in a thread and cannot stop.
            } // end while not exiting
            bMemsThreadGoing = false;
        }

        private void GetDriverVer()
        {
            WriteLine("GetDriverVer called ");
            try
            {
                if (_BunnyCard != null) _DriverRev = _BunnyCard.GetDriverVersion;
                verNumDriver = GetRevNumber(_DriverRev);
                SendBunnyEvent(this, new StatusEventArgs(_DriverRev, (int)BunnyEvents.DriverVer));
            }
            catch
            {
                _DriverRev = "OffLine";
                verNumFirm = -1;
                SendBunnyEvent(this, new StatusEventArgs(_DriverRev, (int)BunnyEvents.DriverVer));
            }
            WriteLineContent("GetDriverVer called " + _DriverRev.ToString());
        }

        /// <summary>
        /// Reads the firmware revision from the Bunny card.
        /// Sends the values off to the world.
        ///  Calls UpdateAllValues if needed.
        /// </summary>
        private void GetFwRev()
        {
            try
            {
                string tmpFwRev = "";
                if (_BunnyCard != null) tmpFwRev = _BunnyCard.GetFirmwareVersion;
                FirmwareRev = tmpFwRev;
                verNumFirm = GetRevNumber(FirmwareRev);
                SendBunnyEvent(this, new StatusEventArgs(FirmwareRev, (int)BunnyEvents.FirmwareVer));

                //if firmware rev worked then send bunny fixed event
                if (tmpFwRev.Length > 0 && !tmpFwRev.ToLower().Contains("offline") && verNumFirm > 0.5)
                {
                    SendBunnyEvent(this, new StatusEventArgs("Bunny Card operational", (int)BunnyEvents.BunnyFixed));
                }

                if (_NeedToReLoadAllValues)
                {
                    _NeedToReLoadAllValues = false;
                    UpdateAllValues();
                }

            }
            catch
            {
                FirmwareRev = "OffLine";
                verNumFirm = -1;
                SendBunnyEvent(this, new StatusEventArgs(FirmwareRev, (int)BunnyEvents.FirmwareVer));
            }
            WriteLine("Fw " + FirmwareRev.ToString());
        }

        /// <summary>
        /// Read various values from Blade.
        /// </summary>
        private void UpdateAllValues()
        {
            string[] allTheStuff = {BladeDataName.ActuatorSN, BladeDataName.BladeLoc, BladeDataName.BladeSN, BladeDataName.BladeType,
                                    BladeDataName.Counts, BladeDataName.DiskSN, BladeDataName.FlexSN, BladeDataName.GradeName,
                                    BladeDataName.JadeSN, BladeDataName.MemsCloseDelay, BladeDataName.MemsCloseSensor,
                                    BladeDataName.MemsOpenDelay, BladeDataName.MemsOpenSensor, BladeDataName.MemsSN,
                                    BladeDataName.MemsState, BladeDataName.MemsType, BladeDataName.MotorBaseplateSN,
                                    BladeDataName.MotorSN, BladeDataName.PcbaSN, BladeDataName.PinMotion, BladeDataName.Position,
                                    BladeDataName.Ramp, BladeDataName.RunningAvgSize, BladeDataName.Aux12VDC, BladeDataName.Aux5VDC,
                                    BladeDataName.AuxIn0, BladeDataName.AuxIn1, BladeDataName.AuxOut0,
                                    BladeDataName.AuxOut1, BladeDataName.BackLight};
            // Reads the above set values and sends events for all to see.
            GetDataViaEvent(allTheStuff);
        }

        /// <summary>
        /// Parse version string for double version number.
        /// </summary>
        /// <param name="verString"></param>
        /// <returns></returns>
        private double GetRevNumber(string verString)
        {
            double dblVal;
            int startSpot = 0;
            int stopSpot = 0;
            char[] theChars = verString.ToCharArray();
            bool checkStart = true;
            bool foundDot = false;
            try
            {
                foreach (char aChar in theChars)
                {
                    stopSpot += 1;
                    if (!char.IsDigit(aChar) && checkStart)
                    {
                        startSpot += 1;
                        continue;
                    }
                    else if (char.IsDigit(aChar) && checkStart)
                    {
                        checkStart = false;
                        continue;
                    }
                    else if (char.IsDigit(aChar) && !checkStart)
                    {
                        continue;
                    }
                    else if (aChar == '.' && !checkStart && !foundDot)
                    {
                        foundDot = true;
                        continue;
                    }
                    else if (aChar == '.' && !checkStart && foundDot)
                    {
                        break;
                    }
                    else if (!char.IsDigit(aChar) && !checkStart)
                    {
                        break;
                    }
                }
                if (stopSpot == startSpot)
                {
                    dblVal = 0;
                }
                else if (!double.TryParse(verString.Substring(startSpot, stopSpot - startSpot - 1), out dblVal))
                {
                    dblVal = 0;
                }
            }
            catch
            {
                dblVal = 0;
            }
            return dblVal;
        }

        private void ServoMoveCloseCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;

            // hgst_move_servo(int index, int dev, int type, int end_pos, int max_vel, int accel)
            object[] objArray = (object[])ar.AsyncState;
            boolFiveIntsDelegate caller = (boolFiveIntsDelegate)objArray[0];
            int index = (int)objArray[1];
            int dev = (int)objArray[2];
            int type = (int)objArray[3];
            int end_pos = (int)objArray[4];
            int max_vel = (int)objArray[5];
            int accel = (int)objArray[6];
            bool status = false;

            // Retry loop
            while (servoMoveCloseCallbackRetries < Constants.BunnyRetryLimit)
            {
                try
                {
                    status = caller.EndInvoke(ar);
                }
                catch
                {
                    status = false;
                }
                if (status)
                {
                    break;
                }
                else
                {
                    Interlocked.Increment(ref servoMoveCloseCallbackRetries);
                    if (_Exit) return;

                    Application.DoEvents();
                    Thread.Sleep(500);
                    hgst_move_servo(index, dev, type, end_pos, max_vel, accel);
                    return;
                }
            }
            servoMoveCloseCallbackRetries = 0;
            NotifyWorldBunnyStatus(status, "hgst_move_servo close");

            if (status)
            {
                hgst_get_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO);
                m_ServoArrived = true;
            }
        }

        private void ServoMoveOpenCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;

            // hgst_move_servo(int index, int dev, int type, int end_pos, int max_vel, int accel)
            object[] objArray = (object[])ar.AsyncState;
            boolFiveIntsDelegate caller = (boolFiveIntsDelegate)objArray[0];
            int index = (int)objArray[1];
            int dev = (int)objArray[2];
            int type = (int)objArray[3];
            int end_pos = (int)objArray[4];
            int max_vel = (int)objArray[5];
            int accel = (int)objArray[6];
            bool status = false;

            // Retry loop
            while (servoMoveOpenCallbackRetries < Constants.BunnyRetryLimit)
            {
                try
                {
                    status = caller.EndInvoke(ar);
                }
                catch
                {
                    status = false;
                }
                if (status)
                {
                    break;
                }
                else
                {
                    Interlocked.Increment(ref servoMoveOpenCallbackRetries);
                    Application.DoEvents();
                    if (_Exit) return;

                    Thread.Sleep(500);
                    hgst_move_servo(index, dev, type, end_pos, max_vel, accel);
                    return;
                }
            }
            servoMoveOpenCallbackRetries = 0;
            NotifyWorldBunnyStatus(status, "hgst_move_servo open ");

            if (status)
            {
                hgst_get_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO);
                m_ServoArrived = true;
            }
        }

        private bool MemsOpenSensorOpened()
        {
            // If low active open sensor and input low.
            if (_TesterState.PinMotionOpenSensor == BunnyPinMotionSensor.LOW &&
               !_TesterState.AuxIn0)
            {
                return true;
            }
            // If high active open sensor and input high.
            else if (_TesterState.PinMotionOpenSensor == BunnyPinMotionSensor.HIGH &&
                     _TesterState.AuxIn0)
            {
                return true;
            }
            // else no open sensor 
            else if (_TesterState.PinMotionOpenSensor == BunnyPinMotionSensor.NONE)
            {
                return MemsRequestedState == HGST.Blades.MemsStateValues.Opened; // return request
            }
            else // not open
            {
                return false;
            }
        }

        private bool MemsCloseSensorClosed()
        {
            // If low active close sensor and input low.
            if (_TesterState.PinMotionCloseSensor == BunnyPinMotionSensor.LOW &&
               !_TesterState.AuxIn1)
            {
                return true;
            }
            // If high active close sensor and input high.
            else if (_TesterState.PinMotionCloseSensor == BunnyPinMotionSensor.HIGH &&
                     _TesterState.AuxIn1)
            {
                return true;
            }
            // else no close sensor 
            else if (_TesterState.PinMotionCloseSensor == BunnyPinMotionSensor.NONE &&
                     MemsRequestedState == HGST.Blades.MemsStateValues.Closed)
            {
                return MemsRequestedState == HGST.Blades.MemsStateValues.Closed; // return request
            }
            else  // not closed
            {
                return false;
            }
        }

        private void GetDataViaEventThreadFunc(object passingObj)
        {
            string[] names = (string[])passingObj;

            WriteLineContent(string.Format("GetDataViaEvent called for {0}", string.Join(",", names)));
            ServoPositionClass currentPosition = new ServoPositionClass();

            foreach (string aName in names)
            {
                switch (aName)
                {
                    case BladeDataName.ActuatorSN:
                        if (ActuatorSN.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(ActuatorSN, (int)BunnyEvents.ActuatorSN));
                        }
                        continue;

                    case BladeDataName.BladeType:
                        if (BladeType.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(BladeType, (int)BunnyEvents.BladeType));
                        }
                        continue;

                    case BladeDataName.BladeSN:
                        if (BladeSN.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(BladeSN, (int)BunnyEvents.BladeSN));
                        }
                        continue;

                    case BladeDataName.DiskSN:
                        if (DiskSn.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(DiskSn, (int)BunnyEvents.DiskSN));
                        }
                        continue;

                    case BladeDataName.FlexSN:
                        if (FlexSN.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(FlexSN, (int)BunnyEvents.FlexSN));
                        }
                        continue;

                    case BladeDataName.MemsSN:
                        if (MemsSn.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(MemsSn, (int)BunnyEvents.MemsSN));
                        }
                        continue;

                    case BladeDataName.MotorBaseplateSN:
                        if (MotorBaseSn.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(MotorBaseSn, (int)BunnyEvents.MotorBaseSN));
                        }
                        continue;

                    case BladeDataName.MotorSN:
                        if (MotorSN.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(MotorSN, (int)BunnyEvents.MotorSN));
                        }
                        continue;

                    case BladeDataName.PcbaSN:
                        if (PcbaSn.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(PcbaSn, (int)BunnyEvents.PcbaSN));
                        }
                        continue;

                    case BladeDataName.MemsCloseDelay:
                        if (MemsCloseDelay.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(MemsCloseDelay, (int)BunnyEvents.MemsCloseDelay));
                        }
                        continue;

                    case BladeDataName.MemsOpenDelay:
                        if (MemsOpenDelay.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(MemsOpenDelay, (int)BunnyEvents.MemsOpenDelay));
                        }
                        continue;

                    case BladeDataName.BunnyStatus:
                        _FormerStatusRead = -1;
                        ReadBunnyStatusAndUpdateFlags(); // function sends event
                        continue;

                    case BladeDataName.FwRev:
                        GetFwRev();  // function sends event
                        continue;

                    case BladeDataName.DriverVer:
                        GetDriverVer();  // function sends event
                        continue;

                    case BladeDataName.MemsType:
                        try
                        {
                            GetMemsType();
                        }
                        catch
                        {
                            SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MemsType));
                        }
                        continue;

                    case BladeDataName.JadeSN:
                        if (JadeSN.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(JadeSN, (int)BunnyEvents.JadeSN));
                        }
                        continue;

                    case BladeDataName.BladeLoc:
                        if (MyLocation.Length > 0)
                        {
                            SendBunnyEvent(this, new StatusEventArgs(MyLocation, (int)BunnyEvents.BladeLoc));
                        }
                        continue;

                    case BladeDataName.Ramp:
                        try
                        {
                            SendBunnyEvent(this, new StatusEventArgs(ReadRampValue().ToString(), (int)BunnyEvents.SolenoidRamp));
                        }
                        catch
                        {
                            SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.SolenoidRamp));
                        }
                        continue;
                    case BladeDataName.GetRomServoValues:
                        hgst_set_save_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO, (int)HGST.Blades.EnumServoSaveTypes.READEEPROM,
                          openPosition.position, openPosition.velocity, openPosition.acceleration,
                          closePosition.position, closePosition.velocity, closePosition.acceleration,
                          currentPosition.position, currentPosition.position, currentPosition.position);
                        break;
                    case BladeDataName.Position:
                        hgst_get_servo(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO);
                        break;
                    case BladeDataName.Counts:
                        string ownerStr = "";
                        try
                        {
                            ownerStr = ReadDataFiles(Constants.BladeSNTXT);
                        }
                        catch
                        {
                            ownerStr = "NONE";
                            string complaint1 = StaticServerTalker.getCurrentCultureString("InvalidCountsPath");
                            string complaint2 = StaticServerTalker.getCurrentCultureString("CountPath");
                            string caption = StaticServerTalker.getCurrentCultureString("CheckSettings");

                            WriteLineContent(string.Format("{0} {1}.{2}{3}.",
                                complaint1, complaint2, Environment.NewLine, caption));
                            WriteLine(complaint1);
                        }

                        if (_CountStateFromDisk.OwnerSerialNumber != ownerStr)
                        {
                            ReadInCountsdata();
                            _CountStateFromDisk.OwnerSerialNumber = ownerStr;
                        }
                        string countStr = _CountStateFromDisk.ToString();
                        SendBunnyEvent(this, new StatusEventArgs(countStr, (int)BunnyEvents.Counts));
                        break;
                    case BladeDataName.TestCount:
                        SendBunnyEvent(this, new StatusEventArgs(_CountStateFromDisk.GetValue(aName).ToString(), (int)BunnyEvents.TestCount));
                        break;
                    case BladeDataName.MemsCount:
                        SendBunnyEvent(this, new StatusEventArgs(_CountStateFromDisk.GetValue(aName).ToString(), (int)BunnyEvents.MemsCount));
                        break;
                    case BladeDataName.DiskLoadCount:
                        SendBunnyEvent(this, new StatusEventArgs(_CountStateFromDisk.GetValue(aName).ToString(), (int)BunnyEvents.DiskLoadCount));
                        break;
                    case BladeDataName.PatrolCount:
                        SendBunnyEvent(this, new StatusEventArgs(_CountStateFromDisk.GetValue(aName).ToString(), (int)BunnyEvents.PatrolCount));
                        break;
                    case BladeDataName.ScanCount:
                        SendBunnyEvent(this, new StatusEventArgs(_CountStateFromDisk.GetValue(aName).ToString(), (int)BunnyEvents.ScanCount));
                        break;
                    case BladeDataName.Neutral:
                        hgst_get_neutral(0, (int)HGST.Blades.EnumSolenoidServoAddr.SERVO);
                        break;
                    case BladeDataName.MemsOpenSensor:
                        try
                        {
                            updateOpenSensorValue();
                        }
                        catch
                        {
                            SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MemsOpenSensor));
                        }
                        break;
                    case BladeDataName.MemsCloseSensor:
                        try
                        {
                            updateCloseSensorValue();
                        }
                        catch
                        {
                            SendBunnyEvent(this, new StatusEventArgs(StaticServerTalker.getCurrentCultureString("FileNotFound"), (int)BunnyEvents.MemsCloseSensor));
                        }
                        break;
                    case BladeDataName.PinMotion:
                    case BladeDataName.MemsState:
                        SendBunnyEvent(this, new StatusEventArgs(this.MemsStatus.ToString(), (int)BunnyEvents.MemsOpenClose));
                        break;
                    case BladeDataName.Aux12VDC:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.B12VDC ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Pwr12V));
                        break;
                    case BladeDataName.Aux5VDC:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.B5VDC ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Pwr5V));
                        break;
                    case BladeDataName.AuxIn0:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.AuxIn0 ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Aux0In));
                        break;
                    case BladeDataName.AuxIn1:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.AuxIn1 ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Aux1In));
                        break;
                    case BladeDataName.AuxOut0:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.AuxOut0 ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Aux0Out));
                        break;
                    case BladeDataName.AuxOut1:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.AuxOut1 ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Aux1Out));
                        break;
                    case BladeDataName.BackLight:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.BackLight ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.BackLight));
                        break;
                    case BladeDataName.CardPower:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.B12VDC ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Pwr12V));
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.B5VDC ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Pwr5V));
                        break;
                    case BladeDataName.Solenoid:
                        SendBunnyEvent(this, new StatusEventArgs(_TesterState.MemsSolenoid ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Solenoid));
                        break;
                    case BladeDataName.VoltageCheckBlade:
                        string valueString = MakeVoltageBladeEventString();
                        SendBunnyEvent(this, new StatusEventArgs(valueString, (int)BunnyEvents.VoltageCheckBlade));
                        break;
                    case BladeDataName.TclPath:
                        SendBunnyEvent(this, new StatusEventArgs(TclPath, (int)BunnyEvents.TclPath));
                        break;
                    case BladeDataName.BladePath:
                        SendBunnyEvent(this, new StatusEventArgs(BladePath, (int)BunnyEvents.BladePath));
                        break;
                    case BladeDataName.FactPath:
                        SendBunnyEvent(this, new StatusEventArgs(TclPath, (int)BunnyEvents.TclPath));
                        break;
                    case BladeDataName.GradePath:
                        SendBunnyEvent(this, new StatusEventArgs(FactPath, (int)BunnyEvents.FactPath));
                        break;
                    case BladeDataName.FirmwarePath:
                        SendBunnyEvent(this, new StatusEventArgs(FirmwarePath, (int)BunnyEvents.FirmwarePath));
                        break;
                    case BladeDataName.ResultPath:
                        SendBunnyEvent(this, new StatusEventArgs(ResultPath, (int)BunnyEvents.ResultPath));
                        break;
                    case BladeDataName.LogPath:
                        SendBunnyEvent(this, new StatusEventArgs(LogPath, (int)BunnyEvents.LogPath));
                        break;
                    case BladeDataName.DebugPath:
                        SendBunnyEvent(this, new StatusEventArgs(DebugPath, (int)BunnyEvents.DebugPath));
                        break;
                    case BladeDataName.CountsPath:
                        SendBunnyEvent(this, new StatusEventArgs(CountsPath, (int)BunnyEvents.CountsPath));
                        break;
                    case BladeDataName.TclStart:
                        SendBunnyEvent(this, new StatusEventArgs(TclStart, (int)BunnyEvents.TclStart));
                        break;
                    case BladeDataName.BladeRunnerPath:
                        SendBunnyEvent(this, new StatusEventArgs(AppDomain.CurrentDomain.BaseDirectory, (int)BunnyEvents.BladeRunnerPath));
                        break;

                    //case BladeDataName.bCmdBusy:
                    //case BladeDataName.GradeName:
                    //case BladeDataName.NowTestsArePaused:
                    //case BladeDataName.PauseEvents:
                    //case BladeDataName.PauseTests:
                    //case BladeDataName.PleaseStop:
                    //case BladeDataName.RunningAvgSize:
                    //case BladeDataName.SeqGoing:
                    //case BladeDataName.SequenceName:
                    //case BladeDataName.TestNumber:
                    default:
                        continue;
                } // end switch
            } // end foreach
        } // end GetStringsViaEvent

        public void GetMemsType()
        {
            string whatType = BunnyPinMotionType.NONE.ToString();
            try
            {
                whatType = ReadDataFiles(Constants.WhichPinMotionFileNameTXT);
            }
            catch (FileNotFoundException)
            {
                whatType = BunnyPinMotionType.NONE.ToString();
            }

            if (whatType == BunnyPinMotionType.SOLENOID.ToString())
            {
                _TesterState.PinMotionType = BunnyPinMotionType.SOLENOID;
            }
            else if (whatType == BunnyPinMotionType.SERVO.ToString() && CheckIfServoIsOK(true))
            {
                _TesterState.PinMotionType = BunnyPinMotionType.SERVO;
            }
            else if (whatType == BunnyPinMotionType.NONE.ToString())
            {
                _TesterState.PinMotionType = BunnyPinMotionType.SOLENOID;
            }
            else
            {
                //testerState.pinMotionType = BunnyPinMotionType.SOLENOID;
                //writeDataFiles(Constants.WhichPinMotionFileNameTXT, testerState.pinMotionType.ToString());
            }
            SendBunnyEvent(this, new StatusEventArgs(_TesterState.PinMotionType.ToString(), (int)BunnyEvents.MemsType));
        }

        private int ReadRampValue()
        {
            string currentString;
            try
            {
                currentString = ReadDataFiles(Constants.MemsRampTXT);
            }
            catch
            {
                currentString = "";
            }
            int currentValue;
            if (!int.TryParse(currentString, out currentValue))
            {
                currentValue = Constants.DefaultRampValue;
            }
            if (currentValue < Constants.MinRampValue || currentValue > Constants.MaxRampValue)
            {
                if (currentValue < Constants.MinRampValue || currentValue > Constants.MaxRampValue)
                {
                    currentValue = Constants.DefaultRampValue;
                }
            }
            if (currentString != currentValue.ToString())
            {
                WriteDataFiles(Constants.MemsRampTXT, currentValue.ToString());
            }
            SendBunnyEvent(this, new StatusEventArgs(currentValue.ToString(), (int)BunnyEvents.SolenoidRamp));
            return currentValue;
        }

        private void updateCloseSensorValue()
        {
            string closeStr = "";
            try
            {
                closeStr = ReadDataFiles(Constants.MemsCloseSensorTXT);
                try
                {
                    BunnyPinMotionSensor tmp = (BunnyPinMotionSensor)Enum.Parse(typeof(BunnyPinMotionSensor), closeStr.ToUpper(), true);
                }
                catch
                {
                    throw new FileNotFoundException();
                }
            }
            catch (FileNotFoundException e)
            {
                try
                {
                    WriteDataFiles(Constants.MemsCloseSensorTXT, BunnyPinMotionSensor.NONE.ToString());
                    closeStr = BunnyPinMotionSensor.NONE.ToString();
                }
                catch
                {
                    throw e;
                }
            }
            catch (Exception e)
            {
                Exception exp = e.InnerException;
                try
                {
                    if (exp != null) throw exp;
                    else throw e;
                }
                catch (FileNotFoundException)
                {
                    WriteDataFiles(Constants.MemsCloseSensorTXT, BunnyPinMotionSensor.NONE.ToString());
                    closeStr = BunnyPinMotionSensor.NONE.ToString();
                }
                catch
                {
                    closeStr = BunnyPinMotionSensor.NONE.ToString();
                    throw e;
                }
            }
            BunnyPinMotionSensor whichCloseType = (BunnyPinMotionSensor)Enum.Parse(typeof(BunnyPinMotionSensor), closeStr.ToUpper(), true);
            _TesterState.PinMotionCloseSensor = whichCloseType;
            SendBunnyEvent(this, new StatusEventArgs(whichCloseType.ToString(), (int)BunnyEvents.MemsCloseSensor));
        }

        private void updateOpenSensorValue()
        {
            string openStr = "";
            try
            {
                openStr = ReadDataFiles(Constants.MemsOpenSensorTXT);
                try
                {
                    BunnyPinMotionSensor tmp = (BunnyPinMotionSensor)Enum.Parse(typeof(BunnyPinMotionSensor), openStr.ToUpper(), true);
                }
                catch
                {
                    throw new FileNotFoundException();
                }
            }
            catch (FileNotFoundException e)
            {
                try
                {
                    WriteDataFiles(Constants.MemsOpenSensorTXT, BunnyPinMotionSensor.NONE.ToString());
                    openStr = BunnyPinMotionSensor.NONE.ToString();
                }
                catch
                {
                    throw e;
                }
            }
            catch (Exception e)
            {
                Exception exp = e.InnerException;
                try
                {
                    if (exp != null) throw exp;
                    else throw e;
                }
                catch (FileNotFoundException)
                {
                    WriteDataFiles(Constants.MemsOpenSensorTXT, BunnyPinMotionSensor.NONE.ToString());
                    openStr = BunnyPinMotionSensor.NONE.ToString();
                }
                catch
                {
                    openStr = BunnyPinMotionSensor.NONE.ToString();
                    throw e;
                }
            }
            BunnyPinMotionSensor whichOpenType = (BunnyPinMotionSensor)Enum.Parse(typeof(BunnyPinMotionSensor), openStr.ToUpper(), true);
            _TesterState.PinMotionOpenSensor = whichOpenType;
            SendBunnyEvent(this, new StatusEventArgs(whichOpenType.ToString(), (int)BunnyEvents.MemsOpenSensor));
        }

        #endregion Support Methods

    }// end class
}
