using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ServiceModel;
using System.Net;
using System.Windows.Forms;
using System.IO;

using WD.Tester;
using WD.Tester.Enums;
using WD.Tester.Module;

namespace WD.Tester.Client
{
    /// <summary>
    /// Summary description of RemoteConnectLib
    /// </summary>
    public class RemoteConnectLib : IDisposable
    {
        #region Fields
        private readonly NLog.Logger nlogger = NLog.LogManager.GetLogger("RemoteConnectLibLog");
        private string _OurName;
        private bool _Disposed;

        private DuplexChannelFactory<ITesterObject> _Factory;
        private ChannelFactory<ITesterObjectStreaming> _FactoryStreaming;
        private string _CurrentUrlAddress;
        private string _CurrentUserID;
        private string _CurrentPassword;
        private bool _CurrentNetTcpConnectFlag;
        private object _ConnectLockObject;
        private bool _BusyConnecting;

        // private WCF stuff
        private ITesterObject _TesterObject;  // proxy for non-stream functions.
        private ITesterObjectStreaming _TesterObjectStreaming; // proxy for stream funcitons.
        private System.Threading.Timer _KeepAliveTimer;
        private ResuffleEvents processSequenceEventsInOrder;
        private BladeEventClass _BladeEvent;

        private bool _Connected = false; // Flag to see if we have ever connected.
        // Justin
        // TODO : Should add remove function.
        static private Dictionary<string, RemoteConnectLib> _Connections;
        private static object oBladeInfoLockObject;
        private delegate void TclCommandDelegate(object CommandObj);
        private delegate string EventPingDelegate(string str);
        private delegate Stream BladeFileReadDelegate(string fileRequest);
        private delegate FileStreamResponse BladeFileWriteDelegate(FileStreamRequest fileRequest);
        private delegate FileNameStruct[] FileDirDelegate(string path, string Filter);

        private delegate string[] GetBladeStringsDelegate(string key, string[] names);
        private delegate void SetBladeStringsDelegate(string key, string[] names, string[] strings);
        private delegate int[] GetBladeIntsDelegate(string key, string[] names);
        private delegate void SetBladeIntsDelegate(string key, string[] names, int[] numbers);
        private delegate bool DelConfigDelegate(string Key, string TestName);

        /// <summary>
        /// Used to mark communication success every 30 seconds.
        /// </summary>
        public bool _KeepAliveArrived;

        /// <summary>
        /// Service definition for Client callback.
        /// </summary>
        public TesterObjectCallback _BladeEventCallbackClass = null;

        /// <summary>
        /// The status handle represents the Status event, this external interface used to listen for events
        /// </summary>
        public event StatusEventHandler comStatusEvent;

        /// <summary>
        /// The status handle represents the bunny event, this external interface used to listen for events
        /// </summary>
        public event StatusEventHandler comBunnyEvent;

        /// <summary>
        /// The status handle represents the program closing event, this external interface used to listen for events
        /// </summary>
        public event StatusEventHandler comProgramClosingEvent;

        /// <summary>
        /// The status handle represents the sequence update event, this external interface used to listen for events
        /// </summary>
        public event StatusEventHandler comSequenceUpdateEvent;

        /// <summary>
        /// The status handle represents the sequence Aborting event, this external interface used to listen for events
        /// </summary>
        public event StatusEventHandler comSequenceAbortingEvent;

        /// <summary>
        /// The status handle represents the sequence complete event, this external interface used to listen for events
        /// </summary>
        public event StatusEventHandler comSequenceCompleteEvent;

        /// <summary>
        /// The started handle represents the sequence started event, this external interface used to listen for events
        /// </summary>
        public event StartedEventHandler comSequenceStartedEvent;

        /// <summary>
        /// The status handle represents the test started event, this external interface used to listen for events
        /// </summary>
        public event StatusEventHandler comTestStartedEvent;

        /// <summary>
        /// The complete handle represents the test complete event, this external interface used to listen for events
        /// </summary>
        public event CompleteEventHandler comTestCompleteEvent;

        // public event EventHandler ConnectedToRemote;
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public RemoteConnectLib()
        {
            Init();
        }

        /// <summary>
        /// Initialization function
        /// </summary>
        private void Init()
        {
            Microsoft.VisualBasic.Devices.Computer computer = new Microsoft.VisualBasic.Devices.Computer();
            _OurName = computer.Name;
            _Disposed = false;
            _TesterObject = null;
            _TesterObjectStreaming = null;
            _Factory = null;
            _FactoryStreaming = null;

            _CurrentUrlAddress = string.Empty;
            _CurrentUserID = string.Empty;
            _CurrentPassword = string.Empty;
            _CurrentNetTcpConnectFlag = false;
            _ConnectLockObject = new object();
            _BusyConnecting = false;

            oBladeInfoLockObject = new object();

            _BladeEventCallbackClass = new TesterObjectCallback(this);
            _BladeEvent = new BladeEventClass(this);

            // After connected, timer pings host every now and then to keep the channel awake.
            _KeepAliveTimer = new System.Threading.Timer(KeepAliveTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
            _KeepAliveArrived = true;

            processSequenceEventsInOrder = new ResuffleEvents();
            processSequenceEventsInOrder.SequenceCompletedEvent += new StatusEventHandler(SendSequenceCompleteEventToJade);
            processSequenceEventsInOrder.SequenceStartedEvent += new StartedEventHandler(SendSequenceStartedEventToJade);
            processSequenceEventsInOrder.TestCompletedEvent += new CompleteEventHandler(SendTestCompleteEventToJade);
            processSequenceEventsInOrder.TestStartedEvent += new StatusEventHandler(SendTestStartedEventToJade);
            processSequenceEventsInOrder.SequenceAbortEvent += new StatusEventHandler(SendSequenceAbortEventToJade);
        }

        /// <summary>
        /// In case of forgetting to explicitly call the Dispose method
        /// </summary>
        ~RemoteConnectLib()
        {
            Dispose(false);
        }
        #endregion Constructors

        #region Properties
        /// <summary>
        /// The _BladeEvent attribute interface is provided externally
        /// </summary>
        public BladeEventClass BladeEvent
        {
            get { return _BladeEvent; }
        }

        /// <summary>
        /// The Connections attribute interface is provided externally
        /// </summary>
        public Dictionary<string, RemoteConnectLib> Connections
        {
            get
            {
                if (_Connections == null)
                {
                    _Connections = new Dictionary<string, RemoteConnectLib>();
                }
                return _Connections;
            }
        }

        /// <summary>
        /// Proxy to WCF and BladeRunner for non-stream functions
        /// </summary>
        private ITesterObject Obj
        {
            get
            {
                if (_TesterObject == null || ((ICommunicationObject)_TesterObject).State != CommunicationState.Opened)
                {
                    if (_TesterObject != null)
                    {
                        ((ICommunicationObject)_TesterObject).Abort();
                    }
                    Connect(_CurrentUrlAddress, _CurrentUserID, _CurrentPassword, _CurrentNetTcpConnectFlag, true, false);
                }
                return _TesterObject;
            }
        }

        /// <summary>
        /// Public property to expose ITesterObjectStreaming (to us).
        /// This is a proxy to the remote host.
        /// This is for stream functions.
        /// Property checks if the channel is OK (not faulted).
        /// If channel is faulted, it re-opens the channel.
        /// </summary>
        private ITesterObjectStreaming ObjStreaming  // Proxy to WCF and BladeRunner for stream functions.
        {
            get
            {
                //CommunicationState swhat = ((System.ServiceModel.ICommunicationObject)m_testerObjectStreaming).State;
                // If WCF broke, then restart.
                if (_TesterObjectStreaming == null || ((System.ServiceModel.ICommunicationObject)_TesterObjectStreaming).State != CommunicationState.Opened)
                {
                    if (_TesterObjectStreaming != null)
                    {
                        ((System.ServiceModel.ICommunicationObject)_TesterObjectStreaming).Abort();
                    }
                    Connect(_CurrentUrlAddress, _CurrentUserID, _CurrentPassword, _CurrentNetTcpConnectFlag, false, true);
                }
                return _TesterObjectStreaming;

            }
        }

        /// <summary>
        /// See if we are connected.
        /// </summary>
        public bool Connected
        {
            get
            {
                return (_TesterObject != null &&
                    _TesterObjectStreaming != null &&
                    ((ICommunicationObject)_TesterObject).State == CommunicationState.Opened &&
                    ((ICommunicationObject)_TesterObjectStreaming).State == CommunicationState.Opened &&
                    _Connected);
            }
        }
        #endregion Properties

        #region TesterObject Service methods
        /// <summary>
        /// Used to establish a communication connection with the server
        /// </summary>
        /// <param name="urlAddress"></param>
        /// <param name="userID"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public UInt32 Connect(string urlAddress, string userID, string password)
        {
            _KeepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            UInt32 value = Connect(urlAddress, userID, password, true, true, true);
            _KeepAliveTimer.Change(Constants.KeepAliveTimeout, Timeout.Infinite);
            return value;
        }

        /// <summary>
        /// Used to disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            nlogger.Info("RemoteConnectLib::Disconnect start");
            _Connected = false;
            try
            {
                Delegates.Action<string, string> disconnectDelegate = new Delegates.Action<string, string>(Obj.Disconnect);
                IAsyncResult ar = disconnectDelegate.BeginInvoke(_CurrentUserID, _OurName, null, disconnectDelegate);
                ar.AsyncWaitHandle.WaitOne(3000, false);
                if (ar.IsCompleted)
                {
                    try { disconnectDelegate.EndInvoke(ar); }
                    catch { }
                }
                ((ICommunicationObject)_TesterObject).Close();
                ((ICommunicationObject)_TesterObjectStreaming).Close();
            }
            catch (Exception ex)
            {
                nlogger.Info("RemoteConnectLib::Disconnect delegate error [ex.message:{0}]", ex.Message);
            }
            nlogger.Info("RemoteConnectLib::Disconnect end");
        }

        /// <summary>
        /// Sources FACT TCL.
        /// </summary>
        public void InitializeTCL()
        {
            if (Obj == null) return;
            MethodInvoker del = delegate
            {
                Obj.InitializeTCL(MakeKey());
            };
            del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { del.EndInvoke(ar); }), del);
        }

        /// <summary>
        /// Test all event types
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string PingAllEvent(string message)
        {
            if (Obj == null) return "";
            EventPingDelegate pingDelegate = new EventPingDelegate(Obj.PingAllEvent);
            IAsyncResult ar = pingDelegate.BeginInvoke(message, null, null);
            // for loop used instead of AsyncWaitHandle so that Twidler moves during the wait.
            for (int i = 0; i < 200 && !ar.IsCompleted; i++)
            {
                Thread.Sleep(100);
            }
            return ar.IsCompleted ? pingDelegate.EndInvoke(ar) : "Fail";
        }

        /// <summary>
        /// Returns directory list to client.  
        /// </summary>
        /// <param name="path"></param>
        /// <param name="Filter"></param>
        /// <returns></returns>
        public FileNameStruct[] BladeFileDir(string path, string Filter)
        {
            if (Obj == null) throw new Exception(String.Format("Cannot read directory \"{0}\" with filter \"{1}\" in BladeFileDir", path, Filter)); ;
            FileDirDelegate fileDirDelegate = new FileDirDelegate(Obj.BladeFileDir);
            IAsyncResult ar = fileDirDelegate.BeginInvoke(path, Filter, null, fileDirDelegate);
            ar.AsyncWaitHandle.WaitOne(30000, false);
            if (ar.IsCompleted) return (FileNameStruct[])fileDirDelegate.EndInvoke(ar);
            else throw new Exception(String.Format("Cannot read directory \"{0}\" with filter \"{1}\" in BladeFileDir", path, Filter));
        }

        /// <summary>
        /// Get the string data for the blade based on the name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetBladeString(string name)
        {
            string strValue = (GetBladeStrings(new string[] { name }))[0];
            return strValue;
        }

        /// <summary>
        /// Set the string data for the blade based on the name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetBladeString(string name, string value)
        {
            SetBladeStrings(new string[] { name }, new string[] { value });
        }

        /// <summary>
        /// Get the Integer data for the blade based on the name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetBladeInteger(string name)
        {
            if (Obj == null) return -1;
            return GetBladeIntegers(new string[] { name })[0];
        }


        /// <summary>
        /// Set the Integer data for the blade based on the name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetBladeInteger(string name, int value)
        {
            if (Obj == null) return;
            SetBladeIntegers(new string[] { name }, new int[] { value });
        }

        /// <summary>
        /// Copy fromFile to tofile on the blade
        /// </summary>
        /// <param name="fromFile"></param>
        /// <param name="toFile"></param>
        /// <returns></returns>
        public bool CopyFileOnBlade(string fromFile, string toFile)
        {
            if (Obj == null) return false;
            return Obj.CopyFileOnBlade(fromFile, toFile);
        }

        /// <summary>
        /// Delete FileName on the blade
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool BladeDelFile(string FileName)
        {
            DelConfigDelegate factFileDeleteDelegate = new DelConfigDelegate(Obj.BladeDelFile);
            IAsyncResult ar = factFileDeleteDelegate.BeginInvoke(MakeKey(), FileName, null, null);
            ar.AsyncWaitHandle.WaitOne(30000, false);
            if (ar.IsCompleted) return (bool)factFileDeleteDelegate.EndInvoke(ar);
            else throw new Exception("Cannot delete file in BladeDelFile " + FileName + ".");
        }

        /// <summary>
        /// Safe eject Blade
        /// </summary>
        public void SafelyRemove()
        {
            Obj.SafeRemoveBlade();
        }

        /// <summary>
        /// Set the Mems state in the opposite direction
        /// </summary>
        public void PinMotionToggle()
        {
            if (Obj == null) return;
            Obj.PinMotionToggle();
        }

        /// <summary>
        /// Gets the name of the server
        /// </summary>
        /// <returns></returns>
        public string Name()
        {
            if (Obj != null) return Obj.Name();
            else return "";
        }

        /// <summary>
        /// Returns the path of grade file
        /// </summary>
        /// <returns></returns>
        public string GradeFilePath()
        {
            if (Obj == null) return "";
            return Obj.GetStrings("", new string[] { BladeDataName.GradePath })[0];
        }

        /// <summary>
        /// Returns the path of firmware file
        /// </summary>
        /// <returns></returns>
        public string FirmwareFilePath()
        {
            if (Obj == null) return "";
            return Obj.GetStrings("", new string[] { BladeDataName.FirmwarePath })[0];
        }

        /// <summary>
        /// Returns the path of fact file
        /// </summary>
        /// <returns></returns>
        public string FactFilePath()
        {
            if (Obj == null) return "";
            return Obj.GetStrings("", new string[] { BladeDataName.FactPath })[0];
        }

        /// <summary>
        /// Gets the state of mems
        /// </summary>
        /// <returns></returns>
        public MemsStateValues GetMemsState()
        {
            if (Obj == null) return MemsStateValues.Unknown;
            return Obj.GetMemsStatus();
        }

        /// <summary>
        /// Sends a command to the BladeRunner via thread pool thread.
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="bToTv"></param>
        public void TclCommand(string Command, bool bToTv)
        {
            if (Obj == null) return;
            if (Command.Length == 0) return;

            // if exit, we do NOT close remote, we close ourselves.
            if (Command == "exit")
            {
                Application.Exit();
                // if Exit returns then user pressed cancel.
                Command = Environment.NewLine;
            }

            object[] objArray = new object[] { Command, bToTv };
            TclCommandDelegate tclCommandDelegate = new TclCommandDelegate(ObjTclCmd);
            // send string to remote
            tclCommandDelegate.BeginInvoke(objArray, new AsyncCallback(delegate (IAsyncResult ar) { tclCommandDelegate.EndInvoke(ar); }), tclCommandDelegate);
        }
        #endregion TesterObject Service methods

        #region TesterObjectStream service methods
        /// <summary>
        /// Read the blade file through ObjectStreaming
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Stream BladeFileRead(string fileName)
        {
            BladeFileReadDelegate factFileReadingDelegate =
                new BladeFileReadDelegate(ObjStreaming.BladeFileRead);
            IAsyncResult ar = factFileReadingDelegate.BeginInvoke(fileName, null, factFileReadingDelegate);
            ar.AsyncWaitHandle.WaitOne(30000, false);
            if (ar.IsCompleted)
            {
                return factFileReadingDelegate.EndInvoke(ar);
            }
            else throw new Exception("Cannot open file in BladeFileRead " + fileName + ".");
        }

        /// <summary>
        /// Write the blade file through ObjectStreaming
        /// </summary>
        /// <param name="readStream"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string BladeFileWrite(Stream readStream, string fileName)
        {
            FileStreamRequest fileRequest = new FileStreamRequest
            {
                FileName = fileName,
                FileByteStream = readStream
            };
            BladeFileWriteDelegate factFileWritingDelegate = new BladeFileWriteDelegate(ObjStreaming.BladeFileWrite);
            IAsyncResult ar = factFileWritingDelegate.BeginInvoke(fileRequest, null, factFileWritingDelegate);
            ar.AsyncWaitHandle.WaitOne(30000, false);
            if (ar.IsCompleted)
            {
                FileStreamResponse response = factFileWritingDelegate.EndInvoke(ar);
                return response.FileName;
            }
            else throw new Exception("Could not write file in BladeFileWrite " + fileName + ".");
        }
        #endregion TesterObjectStream service methods

        #region Blade string and integer methods
        /// <summary>
        /// GetFwRev
        /// </summary>
        /// <returns></returns>
        public string GetFwRev()
        {
            return GetBladeString(BladeDataName.FwRev);
        }

        /// <summary>
        /// Get the type of blade
        /// </summary>
        /// <returns></returns>
        public string GetBladeType()
        {
            return GetBladeString(BladeDataName.BladeType);
        }

        /// <summary>
        /// Get the serial number of blade
        /// </summary>
        /// <returns></returns>
        public string GetSerialNumber()
        {
            return GetBladeString(BladeDataName.BladeSN);
        }

        /// <summary>
        /// Get TCL start status of blade
        /// </summary>
        /// <returns></returns>
        public string GetTclStart()
        {
            return GetBladeString(BladeDataName.TclStart);
        }

        /// <summary>
        /// Set state of card power
        /// </summary>
        /// <param name="State"></param>
        public void CardPower(bool State)
        {
            SetBladeInteger(BladeDataName.CardPower, State ? 1 : 0);
        }

        /// <summary>
        /// Set the serial number of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetSerialNumber(string serialNumber)
        {
            SetBladeString(BladeDataName.BladeSN, serialNumber);
        }

        /// <summary>
        /// Set the type of blade
        /// </summary>
        /// <param name="bladeType"></param>
        public void SetBladeType(string bladeType)
        {
            SetBladeString(BladeDataName.BladeType, bladeType);
        }

        /// <summary>
        /// Set Motor Base plate SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetMotorBaseplateSN(string serialNumber)
        {
            SetBladeString(BladeDataName.MotorBaseplateSN, serialNumber);
        }

        /// <summary>
        /// Set Motor SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetMotorSN(string serialNumber)
        {
            SetBladeString(BladeDataName.MotorSN, serialNumber);
        }

        /// <summary>
        /// Set Actuator SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetActuatorSN(string serialNumber)
        {
            SetBladeString(BladeDataName.ActuatorSN, serialNumber);
        }

        /// <summary>
        /// Set Disk SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetDiskSN(string serialNumber)
        {
            SetBladeString(BladeDataName.DiskSN, serialNumber);
        }

        /// <summary>
        /// Set Pcba SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetPcbaSN(string serialNumber)
        {
            SetBladeString(BladeDataName.PcbaSN, serialNumber);
        }

        /// <summary>
        /// Set jade SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetJadeSN(string serialNumber)
        {
            SetBladeString(BladeDataName.JadeSN, serialNumber);
        }

        /// <summary>
        /// Set blade loc 
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetBladeLoc(string serialNumber)
        {
            SetBladeString(BladeDataName.BladeLoc, serialNumber);
        }

        /// <summary>
        /// Set Mems open delay of blade
        /// </summary>
        /// <param name="delayMs"></param>
        public void SetMemsOpenDelay(string delayMs)
        {
            SetBladeString(BladeDataName.MemsOpenDelay, delayMs);
        }

        /// <summary>
        /// Set Mems close delay of blade
        /// </summary>
        /// <param name="delayMs"></param>
        public void SetMemsCloseDelay(string delayMs)
        {
            SetBladeString(BladeDataName.MemsCloseDelay, delayMs);
        }

        /// <summary>
        /// Set Flex SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetFlexSN(string serialNumber)
        {
            SetBladeString(BladeDataName.FlexSN, serialNumber);
        }

        /// <summary>
        /// Set Mems SN of blade
        /// </summary>
        /// <param name="serialNumber"></param>
        public void SetMemsSN(string serialNumber)
        {
            SetBladeString(BladeDataName.MemsSN, serialNumber);
        }

        /// <summary>
        /// Set TCL start status of blade
        /// </summary>
        /// <param name="command"></param>
        public void SetTclStart(string command)
        {
            SetBladeString(BladeDataName.TclStart, command);
        }

        /// <summary>
        /// Set mems state of blade
        /// </summary>
        /// <param name="State"></param>
        public void PinMotion(bool State)
        {
            SetBladeInteger(BladeDataName.PinMotion, State ? 1 : 0);
        }

        /// <summary>
        /// Set backLight state of blade
        /// </summary>
        /// <param name="State"></param>
        public void BackLight(bool State)
        {
            SetBladeInteger(BladeDataName.BackLight, State ? 1 : 0);
        }

        /// <summary>
        /// Set AuxOut0 state of blade
        /// </summary>
        /// <param name="output"></param>
        public void AuxOut0(int output)
        {
            SetBladeInteger(BladeDataName.AuxOut0, output);
        }

        /// <summary>
        /// Set AuxOut1 state of blade
        /// </summary>
        /// <param name="output"></param>
        public void AuxOut1(int output)
        {
            SetBladeInteger(BladeDataName.AuxOut1, output);
        }

        #endregion Blade string and integer methods

        #region internal support Methods
        private string MakeKey()
        {
            return _OurName;
        }

        /// <summary>
        /// This is the connect function.  This function must be called before talking to any remote tester.
        /// </summary>
        /// <param name="urlAddress"></param>
        /// <param name="userID"></param>
        /// <param name="password"></param>
        /// <param name="netTcp">true</param>
        /// <param name="tobj"></param>
        /// <param name="tobjStr"></param>
        /// <returns></returns>
        private UInt32 Connect(string urlAddress, string userID, string password, bool netTcp, bool tobj, bool tobjStr)
        {
            nlogger.Info("RemoteConnectLib::Connect start [url:{0}] [userID:{1}] [password:[2]][netTcp:{3}][tobj:{4}][tobjStr:{5}]", urlAddress, userID, password, netTcp, tobj, tobjStr);
            UInt32 retVal = 0;
            nlogger.Info("RemoteConnectLib::Connect [ConnectBusy:{0}]", _BusyConnecting);

            // TODO : Determine whether the connection has been made, or reconnect put to keep alive function.
            // _BusyConnecting keep only one connect operation in one class.
            if (!_BusyConnecting)
            {
                lock (_ConnectLockObject)
                {
                    _BusyConnecting = true;
                    // The reconnect uses these.
                    _CurrentUrlAddress = urlAddress;
                    _CurrentUserID = userID;
                    _CurrentPassword = password;
                    _CurrentNetTcpConnectFlag = netTcp;

                    // Build the complete path
                    string strUrl = MakeupCompleteUrl(urlAddress, netTcp);
                    string strStreamUrl = MakeupCompleteStreamingUrl(strUrl, netTcp);
                    nlogger.Info("RemoteConnectLib::Connect [strUrl:{0}] [strStreamUrl:{1}]", strUrl, strStreamUrl);
                    // Add connect function
                    try
                    {
                        // Init channel factory
                        InitChannelFactory(strUrl, strStreamUrl, netTcp, tobj, tobjStr);
                        nlogger.Info("RemoteConnectLib::Connect [Factory:{0}]", _Factory.ToString());

                        // Create channel and open service.
                        if (tobj)
                        {
                            _TesterObject = _Factory.CreateChannel();
                            CommunicationState chanState = ((ICommunicationObject)_TesterObject).State;
                            if (chanState != CommunicationState.Opened)
                            {
                                ((ICommunicationObject)_TesterObject).Open();
                            }
                        }
                        if (tobjStr)
                        {
                            _TesterObjectStreaming = _FactoryStreaming.CreateChannel();
                            CommunicationState chanState1 = ((System.ServiceModel.ICommunicationObject)_TesterObjectStreaming).State;
                            if (chanState1 != CommunicationState.Opened)
                            {
                                ((System.ServiceModel.ICommunicationObject)_TesterObjectStreaming).Open();
                            }
                        }
                    }
                    catch (WebException ex)
                    {
                        nlogger.Error("RemoteConnectLib::Connect Create Channel WebException [Exception :{0}]", ex.Message);
                        retVal = (UInt32)ReturnValues.connectionBad;
                    }
                    catch (Exception ex)
                    {
                        nlogger.Error("RemoteConnectLib::Connect Create Channel exception [Exception :{0}]", ex.Message);
                        retVal = (UInt32)ReturnValues.connectionBad;
                    }

                    if (_Factory == null || _FactoryStreaming == null || _TesterObject == null || _TesterObjectStreaming == null)
                    {
                        nlogger.Error("RemoteConnectLib::Connect failed to attach event to remote server [URL:{0}]", strUrl);
                        retVal = (UInt32)ReturnValues.connectionBad;
                    }

                    if (retVal != (UInt32)ReturnValues.connectionBad)
                    {
                        try
                        {
                            // This will add our proxy to testerObject's callback list.
                            retVal = _TesterObject.Connect(userID, "", _OurName);
                        }
                        catch (WebException ex)
                        {
                            nlogger.Error("RemoteConnectLib::Connect Create Channel WebException [Exception :{0}]", ex.Message);
                            retVal = (UInt32)ReturnValues.connectionBad;
                        }
                        catch (Exception ex)
                        {
                            nlogger.Error("RemoteConnectLib::Connect Create Channel exception [Exception :{0}]", ex.Message);
                            retVal = (UInt32)ReturnValues.connectionBad;
                        }

                        if ((retVal & (int)ReturnValues.bladeAccessBit) != 0)
                        {
                            string cached = urlAddress.Replace(":" + Constants.WcfBladePort.ToString() + "/TesterObject", "");
                            if (!Connections.ContainsKey(cached))
                            {
                                Connections.Add(cached, this);
                            }
                            else
                            {
                                Connections[cached] = this;
                            }
                            _Connected = true;
                            _KeepAliveArrived = true;
                        }
                        nlogger.Error("RemoteConnectLib::Connect  [Connections number :{0}]", Connections.Count);
                    }
                }
            }

            // TODO : no use now, but we can use it.
            // pop event to notify that we successfully connected to the wcf service.
            //if (retVal == (UInt32)Hitachi.Tester.Enums.ReturnValues.bladeAccessBit)
            //{
            //    EventHandler handler = ConnectedToRemote;
            //    if (handler != null)
            //    {
            //        MethodInvoker del = delegate { handler(this, new EventArgs()); };
            //        del.BeginInvoke(null, null);
            //    }
            //}

            _BusyConnecting = false;

            return retVal;
        }

        // TODO : Think about put MakeupCompleteUrl and MakeupCompleteStreamingUrl together.
        private string MakeupCompleteUrl(string urlAddress, bool netTcp)
        {
            // make up the url from the computer name/ip passed to this func
            string retVal = string.Empty;
            int WhereIsColon = urlAddress.LastIndexOf(":");

            // if URL contains Port number xx.xx.xx.xx:pppp
            if ((WhereIsColon > 1) && (WhereIsColon < (urlAddress.Length - 1)))
            {
                if (netTcp)
                {
                    retVal = @"net.tcp://" + urlAddress + "/TesterObject";
                }
                else
                {
                    retVal = @"net.pipe://" + urlAddress + "/TesterObject";
                }
            }
            // if URL has colon at end xx.xx.xx.xx:
            else if ((WhereIsColon > 1) && (WhereIsColon == (urlAddress.Length - 1)))
            {
                if (netTcp)
                {
                    retVal = @"net.tcp://" + urlAddress + Constants.WcfBladePort.ToString() + "/TesterObject";
                }
                else
                {
                    retVal = @"net.pipe://" + urlAddress + "/TesterObject";
                }
            }
            // if URL only has address
            else
            {
                if (netTcp)
                {
                    retVal = @"net.tcp://" + urlAddress + ":" + Constants.WcfBladePort.ToString() + "/TesterObject";
                }
                else
                {
                    retVal = @"net.pipe://" + urlAddress + "/TesterObject";
                }
            }
            return retVal;
        }

        private string MakeupCompleteStreamingUrl(string strUrl, bool netTcp)
        {
            string retVal = string.Empty;
            int portNumber = Constants.WcfBladePort;
            int lastColon = strUrl.LastIndexOf(":");
            int testerObjSpot = strUrl.IndexOf("/TesterObject");
            if (lastColon > 0 && lastColon < testerObjSpot)
            {
                if (!int.TryParse(strUrl.Substring(lastColon + 1, testerObjSpot - lastColon - 1), out portNumber))
                {
                    portNumber = Constants.WcfBladePort;
                }
            }
            if (netTcp)
            {
                retVal = strUrl.Substring(0, lastColon + 1) + (portNumber + 1).ToString() + strUrl.Substring(testerObjSpot) + "Streaming";
            }
            else
            {
                retVal = strUrl + "Streaming";
            }
            return retVal;
        }

        private void InitChannelFactory(string url, string streamingUrl, bool netTcp, bool tobj, bool tobjStr)
        {
            if (url == null || url == "" || streamingUrl == null || streamingUrl == "")
            {
                nlogger.Info("RemoteConnectLib::InitChannelFactory fail, url is null or empty");
                return;
            }

            InstanceContext context;
            EndpointAddress endpoint;
            EndpointAddress endpoint2;

            context = new InstanceContext(_BladeEventCallbackClass);
            endpoint = new EndpointAddress(url);
            endpoint2 = new EndpointAddress(streamingUrl);
            if (netTcp)
            {
                // TCP model
                if (tobj)
                {
                    NetTcpBinding binding = new NetTcpBinding();
                    binding.Security.Mode = SecurityMode.None;
                    binding.MaxReceivedMessageSize = 524288;
                    binding.MaxBufferSize = 524288;
                    binding.MaxBufferPoolSize = 524288;
                    binding.ReaderQuotas.MaxArrayLength = 524288;
                    binding.ReaderQuotas.MaxBytesPerRead = 524288;
                    binding.ReaderQuotas.MaxNameTableCharCount = 524288;
                    binding.ReaderQuotas.MaxStringContentLength = 524288;
                    _Factory = new DuplexChannelFactory<ITesterObject>(context, binding, endpoint);
                }
                if (tobjStr)
                {
                    NetTcpBinding binding2 = new NetTcpBinding();
                    binding2.Security.Mode = SecurityMode.None;
                    binding2.TransferMode = TransferMode.Streamed;
                    binding2.MaxReceivedMessageSize = 524288;
                    binding2.MaxBufferSize = 524288;
                    binding2.MaxBufferPoolSize = 524288;
                    binding2.ReaderQuotas.MaxArrayLength = 524288;
                    binding2.ReaderQuotas.MaxBytesPerRead = 524288;
                    binding2.ReaderQuotas.MaxNameTableCharCount = 524288;
                    binding2.ReaderQuotas.MaxStringContentLength = 524288;
                    _FactoryStreaming = new ChannelFactory<ITesterObjectStreaming>(binding2, endpoint2);
                }
            }
            else
            {
                // Log information
                nlogger.Info("RemoteConnectLib::InitChannelFactory fail, pattern error, we only support net tcp pattern.");
                // Pipe model, may be no use
                //if (tobj)
                //{
                //    //NetNamedPipeBinding binding = new NetNamedPipeBinding();
                //    //binding.MaxReceivedMessageSize = 524288;
                //    //binding.MaxBufferSize = 524288;
                //    //binding.MaxBufferPoolSize = 524288;
                //    //binding.ReaderQuotas.MaxArrayLength = 524288;
                //    //binding.ReaderQuotas.MaxBytesPerRead = 524288;
                //    //binding.ReaderQuotas.MaxNameTableCharCount = 524288;
                //    //binding.ReaderQuotas.MaxStringContentLength = 524288;
                //    //_Factory = new DuplexChannelFactory<ITesterObject>(context, binding, endpoint);
                //}
                //if (tobjStr)
                //{
                //    // TODO : object Streaming
                //}
            }
        }

        /// <summary>
        /// Timer callback, pings host every now and then to keep channel alive (and
        /// to verify that everything is working).
        /// </summary>
        /// <param name="state"></param>
        private void KeepAliveTimer_Tick(object state)
        {
            nlogger.Info("RemoteConnectLib::KeepAliveTimer_Tick start");
            uint ret = 999;
            try
            {
                // Turn off the timer until we finish (we do not know how long this will take).
                _KeepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // We must have connected at least once.
                if (!_Connected) return;
                nlogger.Info("RemoteConnectLib::KeepAliveTimer_Tick [TesterObject:{0}] [KeepAliverTimer:{1}]", _TesterObject.ToString(), _KeepAliveArrived);

                // If connected but keep alive flag is false, then re-connect.
                if (_TesterObject != null && !_KeepAliveArrived)
                {
                    // Keep alive callback failed, so reconnect.
                    CleanObject();
                    ret = Connect(_CurrentUrlAddress, _CurrentUserID, _CurrentPassword, _CurrentNetTcpConnectFlag, true, true);
                }

                _KeepAliveArrived = false;  // Set to false.
                // PingInt in the Host sends a callback to set this (see TesterInstance OnStatusEvent).
                // If callback fails, then this stays false and we know to reconnect.
                // Call Ping int in host.
                int value = _TesterObject.KeepAliveChannel();
                // Is the return value correct?
                nlogger.Info("RemoteConnectLib::KeepAliveTimer_Tick [value:{0}] [currentUrlAddress.Length:{1}] [currentUserID.Length:{2}]", value, _CurrentUrlAddress.Length, _CurrentUserID.Length);
                if (value != Constants.KeepAliveTimeout && _CurrentUrlAddress.Length > 0 && _CurrentUserID.Length > 0)
                {
                    // We must have connected at least once.
                    if (!_Connected) return;
                    // Return value was not correct so reconnect.
                    // Clean up the TCP link.
                    CleanObject();
                    ret = Connect(_CurrentUrlAddress, _CurrentUserID, _CurrentPassword, _CurrentNetTcpConnectFlag, true, true);
                    nlogger.Info("RemoteConnectLib::KeepAliveTimer_Tick [value:{0}] [currentUrlAddress.Length:{1}] [currentUserID.Length:{2}]", value, _CurrentUrlAddress.Length, _CurrentUserID.Length);
                }
            }
            catch
            {
                nlogger.Info("RemoteConnectLib::KeepAliveTimer_Tick catch [currentUrlAddress.Length:{0}] [currentUserID.Length:{1}]", _CurrentUrlAddress.Length, _CurrentUserID.Length);
                // It broke, if some address present then try to connect.
                if (_CurrentUrlAddress.Length > 0 && _CurrentUserID.Length > 0)
                {
                    // We must have connected at least once.
                    if (!_Connected) return;
                    // something broke, so reconnect.
                    CleanObject();
                    ret = Connect(_CurrentUrlAddress, _CurrentUserID, _CurrentPassword, _CurrentNetTcpConnectFlag, true, true);
                }
            }
            finally
            {
                if (_KeepAliveTimer != null && _Connected)
                {
                    // Restart the timer.
                    _KeepAliveTimer.Change(Constants.KeepAliveTimeout, Constants.KeepAliveTimeout);
                } // else disposing so do nothing.
            }
        }

        private void CleanObject()
        {
            nlogger.Info("RemoteConnectLib::CleanObject [busyConnecting:{0}] ", _BusyConnecting);
            if (_BusyConnecting) return;
            Disconnect();

            // Close TesterObject service.
            if (_TesterObject != null && ((ICommunicationObject)_TesterObject).State == CommunicationState.Opened)
            {
                try { ((ICommunicationObject)_TesterObject).Close(); }
                catch
                { }
            }
            // Close TesterObject service.
            else if (_TesterObject != null)
            {
                try { ((ICommunicationObject)_TesterObject).Abort(); }
                catch
                {
                    // ignored
                }
            }
            _TesterObject = null;

            if (_TesterObjectStreaming != null && ((ICommunicationObject)_TesterObjectStreaming).State == CommunicationState.Opened)
            {
                try { ((ICommunicationObject)_TesterObjectStreaming).Close(); }
                catch
                {
                    // ignored
                }
            }
            else if (_TesterObjectStreaming != null)
            {
                try { ((ICommunicationObject)_TesterObjectStreaming).Abort(); }
                catch
                {
                    // ignored
                }
            }
            _TesterObjectStreaming = null;

        }

        internal virtual void OnBunnyEvent(object sender, StatusEventArgs e)
        {
            comBunnyEvent?.Invoke(this, e);
        }

        /// <summary>
        /// When BladeRunner tells us that it is closing with this closes the window on Form1.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal virtual void OnProgramClosingEvent(object sender, StatusEventArgs e)
        {
            comProgramClosingEvent?.Invoke(this, e);
        }

        internal virtual void OnSequenceAbortingEvent(object sender, StatusEventArgs e)
        {
            if (e.Text.Contains("SequenceAbortingEvent " + Constants.CommTestString))
            {
                SendSequenceAbortEventToJade(sender, e);
            }
            else
            {
                processSequenceEventsInOrder.QueueUpSequenceAbortingEvent(sender, e);
            }
        }

        internal virtual void OnSequenceCompleteEvent(object sender, StatusEventArgs e)
        {
            if (e.Text.Contains("SequenceCompleteEvent " + Constants.CommTestString))
            {
                SendSequenceCompleteEventToJade(sender, e);
            }
            else
            {
                processSequenceEventsInOrder.QueueUpSequenceCompletedEvent(sender, e);
            }
        }

        internal virtual void SendSequenceCompleteEventToJade(object sender, StatusEventArgs e)
        {
            comSequenceCompleteEvent?.Invoke(this, e);
        }

        internal virtual void OnSequenceStartedEvent(object sender, StartedEventArgs e)
        {
            if (e.seqName.Contains("SequenceStartedEvent " + Constants.CommTestString))
            {
                SendSequenceStartedEventToJade(sender, e);
            }
            else
            {
                processSequenceEventsInOrder.QueueUpSequenceStartedEvent(sender, e);
            }
        }

        internal virtual void SendSequenceStartedEventToJade(object sender, StartedEventArgs e)
        {
            comSequenceStartedEvent?.Invoke(this, e);
        }

        internal virtual void OnSequenceUpdateEvent(object sender, StatusEventArgs e)
        {
            comSequenceUpdateEvent?.Invoke(this, e);
        }


        internal virtual void SendSequenceAbortEventToJade(object sender, StatusEventArgs e)
        {
            comSequenceAbortingEvent?.Invoke(this, e);
        }

        internal virtual void OnTestCompleteEvent(object sender, CompletedEventArgs e)
        {
            if (e.Text.Contains("TestCompletedEvent " + Constants.CommTestString))
            {
                SendTestCompleteEventToJade(sender, e);
            }
            else
            {
                processSequenceEventsInOrder.QueueUpTestCompletedEvent(sender, e);
            }
        }

        internal virtual void SendTestCompleteEventToJade(object sender, CompletedEventArgs e)
        {
            if (e.Text.Contains(Constants.SkipItSkipIt)) return;
            comTestCompleteEvent?.Invoke(this, e);
        }

        internal virtual void OnTestStartedEvent(object sender, StatusEventArgs e)
        {
            if (e.Text.Contains("TestStartedEvent " + Constants.CommTestString))
            {
                SendTestStartedEventToJade(sender, e);
            }
            else
            {
                processSequenceEventsInOrder.QueueUpTestStartedEvent(sender, e);
            }
        }

        internal virtual void SendTestStartedEventToJade(object sender, StatusEventArgs e)
        {
            if (e.Text.Contains(Constants.SkipItSkipIt)) return;
            comTestStartedEvent?.Invoke(this, e);
        }

        internal virtual void OnStatusEvent(object sender, StatusEventArgs e)
        {
            nlogger.Info("RemoteConnectLib::OnStatusEvent start[EventType:{0}] [Text:{1}]", e.EventType, e.Text);
            // If keep alive event then set KeepAliveArrived
            if ((eventInts)e.EventType == eventInts.KeepAliveEvent)
            {
                // Mask bug in BladeRunner 1.5++ for SCS 1.0
                if (e.Text == "StatusEvent " + Constants.CommTestString)
                {
                    e.EventType = (int)eventInts.PingStatusEvent;
                }
                else
                {
                    _KeepAliveArrived = true;
                    return;
                }
            }
            comStatusEvent?.Invoke(this, e);
        }

        private string[] GetBladeStrings(string[] names)
        {
            if (Obj == null) return new string[] { "" };
            const int numRetry = 3;
            // Single channel
            for (int i = 0; i < numRetry; i++)
            {
                GetBladeStringsDelegate del = new GetBladeStringsDelegate(Obj.GetStrings);
                try
                {
                    lock (oBladeInfoLockObject)
                    {
                        IAsyncResult ar = del.BeginInvoke(MakeKey(), names, null, null);
                        ar.AsyncWaitHandle.WaitOne(10000, false);
                        if (ar.IsCompleted) return del.EndInvoke(ar);
                    }
                    nlogger.Error(string.Format(
                        "GetBladeStrings timeout key={0} retry={1}/{2}",
                        string.Join("/", names), i, numRetry));
                }
                catch (WebException e)
                {
                    nlogger.ErrorException(
                        string.Format("GetBladeStrings failed key={0} retry={1}/{2}",
                       string.Join(",", names), i, numRetry),
                        e);
                    if (i + 1 == numRetry)
                    {
                        // Temporally throw exception to switch to multi channel
                        //deadBladeDelegate(string.Format("GetBladeStrings for key={0}", string.Join(",", names)));
                        throw;
                    }
                    Thread.Sleep(1000);
                }
            }
            nlogger.Error(string.Format(
                "GetBladeStrings failed names={0} retry={1} ",
                string.Join(",", names), numRetry));
            string[] ret = new string[names.Length];
            for (int j = 0; j < ret.Length; j++) ret[j] = "";
            return ret;
        }

        private void SetBladeStrings(string[] names, string[] values)
        {
            if (Obj == null) return;
            const int numRetry = 3;

            // Single channel
            for (int i = 0; i < numRetry; i++)
            {
                SetBladeStringsDelegate del = new SetBladeStringsDelegate(Obj.SetStrings);
                try
                {
                    lock (oBladeInfoLockObject)
                    {
                        IAsyncResult ar = del.BeginInvoke(MakeKey(), names, values, null, null);
                        //ar.AsyncWaitHandle.WaitOne(10000, false);  // doesn't work
                        int maxCount = 100;
                        for (int j = 0; j < maxCount; j++)
                        {
                            if (ar.IsCompleted)
                            {
                                break;
                            }
                            Thread.Sleep(100);
                            Application.DoEvents();
                        }
                        if (ar.IsCompleted)
                        {
                            del.EndInvoke(ar);
                            return;
                        }
                    }
                    nlogger.Error(string.Format(
                        "SetBladeStrings timeout key={0} retry={1}/{2}",
                        string.Join("/", names), i, numRetry));
                }
                catch (WebException e)
                {
                    nlogger.FatalException(string.Format(
                        "SetBladeStrings failed key={0} retry={1}/{2}",
                        string.Join(",", names), i, numRetry), e);
                    if (i + 1 == numRetry)
                    {
                        // Temporally thorw exception to switch to multi channel
                        //deadBladeDelegate(string.Format("SetBladeStrings for key={0}", string.Join(",", names)));
                        throw;
                    }
                    Thread.Sleep(1000);
                }
            }

            nlogger.Error(string.Format("SetBladeStrings failed key={0}", string.Join("/", names)));
        }

        private int[] GetBladeIntegers(string[] names)
        {
            if (Obj == null) return new int[] { -1 };

            const int numRetry = 3;

            // Single channel
            for (int i = 0; i < numRetry; i++)
            {
                GetBladeIntsDelegate del = new GetBladeIntsDelegate(Obj.GetIntegers);
                try
                {
                    lock (oBladeInfoLockObject)
                    {
                        IAsyncResult ar = del.BeginInvoke(MakeKey(), names, null, null);
                        ar.AsyncWaitHandle.WaitOne(10000, false);
                        if (ar.IsCompleted) return del.EndInvoke(ar);
                    }
                    nlogger.Error(string.Format(
                        "GetBladeIntegers timeout key={0} retry={1}/{2}",
                        string.Join(",", names), i, numRetry));
                }
                catch (WebException e)
                {
                    nlogger.ErrorException(
                         string.Format(
                             "GetBladeIntegers failed key={0} retry={1}/{2}",
                             string.Join("/", names), i, numRetry),
                        e);
                    if (i + 1 == numRetry)
                    {
                        // Temporarily throw exception to switch to multi channel
                        //deadBladeDelegate(string.Format("GetBladeIntegers for key={0}", string.Join(",", names)));
                        throw;
                    }
                    Thread.Sleep(1000);
                }
            }

            nlogger.Error(string.Format(
                "GetBladeIntegers failed names={0} retry={1} ", string.Join(",", names), numRetry));
            int[] ret = new int[names.Length];
            for (int j = 0; j < ret.Length; j++) ret[j] = 0;
            return ret;
        }

        private void SetBladeIntegers(string[] names, int[] values)
        {
            if (Obj == null) return;
            const int numRetry = 1;

            // Single channel
            for (int i = 0; i < numRetry; i++)
            {
                SetBladeIntsDelegate del = new SetBladeIntsDelegate(Obj.SetIntegers);
                try
                {
                    lock (oBladeInfoLockObject)
                    {
                        IAsyncResult ar = del.BeginInvoke(MakeKey(), names, values, null, null);
                        ar.AsyncWaitHandle.WaitOne(10000, false);
                        if (ar.IsCompleted)
                        {
                            try
                            {
                                del.EndInvoke(ar);
                            }
                            catch
                            {
                            }
                        }
                    }
                    nlogger.Error(string.Format(
                        "SetBladeIntegers passed key={0} retry={1}/{2}",
                        string.Join("/", names), i, numRetry));
                }
                catch (WebException e)
                {
                    nlogger.FatalException(string.Format(
                        "SetBladeIntegers failed key={0} retry={1}", string.Join("/", names), i),
                        e);
                    if (i + 1 == numRetry)
                    {
                        // Temporally throw exception to switch to multi channel
                        //deadBladeDelegate(string.Format("SetBladeIntegers for key={0}", string.Join(",", names)));
                        throw;
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    nlogger.FatalException(string.Format(
                        "SetBladeIntegers failed key={0} retry={1}", string.Join("/", names), i),
                        e);
                    if (i + 1 == numRetry)
                    {
                        // Temporally throw exception to switch to multi channel
                        //deadBladeDelegate(string.Format("SetBladeIntegers for key={0}", string.Join(",", names)));
                        throw;
                    }
                    Thread.Sleep(1000);
                }
            }

            nlogger.Error(string.Format("SetBladeIntegers failed key={0}", string.Join("/", names)));
        }

        /// <summary>
        /// Sends command to BladeRunner.
        /// Called by TclCommand via thread pool thread.
        /// </summary>
        /// <param name="passingObj"></param>
        private void ObjTclCmd(object passingObj)
        {
            if (Obj == null) return; // if nothing there return
            object[] objArray = (object[])passingObj;
            string Command = (string)objArray[0];
            bool bToTv = (bool)objArray[1];
            // send to BladeRunner computer
            try { Obj.TclCommand(Command, bToTv); }
            catch { }  // too easy to type the wrong stuff
        }
        #endregion internal support Methods

        #region dispose Methods
        /// <summary>
        /// Safe destruction
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }
            if (disposing)
            {
                // Clean up managed resources
            }

            // Clean up unmanaged resources
            // dispose timer
            if (_KeepAliveTimer != null)
            {
                _KeepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _KeepAliveTimer.Dispose();
                _KeepAliveTimer = null;
            }

            // Close TesterObject service.
            if (_TesterObject != null && ((ICommunicationObject)_TesterObject).State == CommunicationState.Opened)
            {
                try { ((ICommunicationObject)_TesterObject).Close(); }
                catch
                {
                    // ignored
                }
            }
            // Close TesterObject service.
            else if (_TesterObject != null)
            {
                try { ((ICommunicationObject)_TesterObject).Abort(); }
                catch
                {
                    // ignored
                }
            }
            _TesterObject = null;

            // Close TesterObjectStreaming service.
            if (_TesterObjectStreaming != null && ((ICommunicationObject)_TesterObjectStreaming).State == CommunicationState.Opened)
            {
                try { ((ICommunicationObject)_TesterObjectStreaming).Close(); }
                catch
                {
                    // ignored
                }
            }
            else if (_TesterObjectStreaming != null)
            {
                try { ((ICommunicationObject)_TesterObjectStreaming).Abort(); }
                catch
                {
                    // ignored
                }
            }
            _TesterObjectStreaming = null;

            // Notice sys 
            _Disposed = true;
        }

        /// <summary>
        /// Notify the GC
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion dispose Methods
    }
}
