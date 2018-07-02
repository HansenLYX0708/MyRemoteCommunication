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

        private object _ReadInCountsLockObject;
        // Bunny card fields end
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
                                // TODO : OpenCloseMems(numbers[i] > 0 ? 1 : 0);
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

        #endregion Support Methods

    }// end class
}
