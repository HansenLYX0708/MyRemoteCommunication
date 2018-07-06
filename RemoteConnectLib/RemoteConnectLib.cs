﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ServiceModel;
using System.Net;
using System.Windows.Forms;

using Hitachi.Tester;
using Hitachi.Tester.Enums;
using Hitachi.Tester.Module;

namespace Hitachi.Tester.Client
{
    public class RemoteConnectLib : IDisposable
    {
        #region Fields
        private readonly NLog.Logger nlogger = NLog.LogManager.GetLogger("RemoteConnectLibLog");

        private string _OurName;
        private bool _Disposed;

        private DuplexChannelFactory<ITesterObject> _Factory;
        //private ChannelFactory<ITesterObjectStreaming> factoryStreaming = null;
        private string _CurrentUrlAddress;
        private string _CurrentUserID;
        private string _CurrentPassword;
        private bool _CurrentNetTcpConnectFlag;
        private object _ConnectLockObject;
        private bool _BusyConnecting;

        public TesterObjectCallback _BladeEventCallbackClass = null;

        // private WCF stuff
        private ITesterObject _TesterObject;  // proxy for non-stream functions.
        // TODO : private ITesterObjectStreaming m_testerObjectStreaming; // proxy for stream funcitons.
        private object ITesterObjectLock;
        // TODO : private object ITesterObjectStreamLock = new object();
        private System.Threading.Timer _KeepAliveTimer;
        public bool _KeepAliveArrived;

        private BladeEventClass _BladeEvent;

        private bool _Connected = false; // Flag to see if we have ever connected.

        // Justin
        // TODO : Should add remove function.
        static private Dictionary<string, RemoteConnectLib> _Connections;

        // Event hander
        public event StatusEventHandler _ComStatusEvent;


        private static object oBladeInfoLockObject;

        private delegate string EventPingDelegate(string str);

        private delegate string[] GetBladeStringsDelegate(string key, string[] names);
        private delegate void SetBladeStringsDelegate(string key, string[] names, string[] strings);
        private delegate int[] GetBladeIntsDelegate(string key, string[] names);
        private delegate void SetBladeIntsDelegate(string key, string[] names, int[] numbers);
        private delegate bool DelConfigDelegate(string Key, string TestName);


        public event StatusEventHandler comProgramClosingEvent;
        public event StatusEventHandler comSequenceUpdateEvent;
        public event StatusEventHandler comSequenceAbortingEvent;
        public event StatusEventHandler comSequenceCompleteEvent;
        public event StartedEventHandler comSequenceStartedEvent;
        public event StatusEventHandler comStatusEvent;
        public event StatusEventHandler comBunnyEvent;
        public event StatusEventHandler comTestStartedEvent;
        public event CompleteEventHandler comTestCompleteEvent;
        public event EventHandler ConnectedToRemote;
        #endregion Fields

        #region Constructors
        public RemoteConnectLib()
        {
            Init();
        }

        private void Init()
        {
            Microsoft.VisualBasic.Devices.Computer computer = new Microsoft.VisualBasic.Devices.Computer();
            _OurName = computer.Name;
            _Disposed = false;

            _Factory = null;
            _CurrentUrlAddress = string.Empty;
            _CurrentUserID = string.Empty;
            _CurrentPassword = string.Empty;
            _CurrentNetTcpConnectFlag = false;
            _ConnectLockObject = new object();
            _BusyConnecting = false;

            ITesterObjectLock = new object();

            oBladeInfoLockObject = new object();

            _BladeEventCallbackClass = new TesterObjectCallback(this);
            _BladeEvent = new BladeEventClass(this);

            // After connected, timer pings host every now and then to keep the channel awake.
            _KeepAliveTimer = new System.Threading.Timer(KeepAliveTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
            _KeepAliveArrived = true;
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
        public BladeEventClass BladeEvent
        {
            get { return _BladeEvent; }
        }

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
        #endregion Properties

        #region Service methods
        public UInt32 Connect(string urlAddress, string userID, string password)
        {
            _KeepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            UInt32 value = Connect(urlAddress, userID, password, true, true, true);
            _KeepAliveTimer.Change(Constants.KeepAliveTimeout, Timeout.Infinite);
            return value;
        }

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
                // TODO : ((ICommunicationObject)m_testerObjectStreaming).Close();
            }
            catch (Exception ex)
            {
                nlogger.Info("RemoteConnectLib::Disconnect delegate error [ex.message:{0}]", ex.Message);
            }
            nlogger.Info("RemoteConnectLib::Disconnect end");
        }

        public string Ping(string message)
        {
            if (Obj == null) return "";
            EventPingDelegate pingDelegate = new EventPingDelegate(Obj.Ping);
            IAsyncResult ar = pingDelegate.BeginInvoke(message, null, null);

            //ar.AsyncWaitHandle.WaitOne(20000, false);
            // for loop used instead of AsyncWaitHandle so that Twidler moves during the wait.
            for (int i = 0; i < 200 && !ar.IsCompleted; i++)
            {
                Thread.Sleep(100);
            }
            return ar.IsCompleted ? pingDelegate.EndInvoke(ar) : "Fail";
        }

        public string GetBladeString(string name)
        {
            string strValue = (GetBladeStrings(new string[] { name }))[0];
            return strValue;
        }

        public void SetBladeString(string name, string value)
        {
            SetBladeStrings(new string[] { name }, new string[] { value });
        }

        public int GetBladeInteger(string name)
        {
            if (Obj == null) return -1;
            return GetBladeIntegers(new string[] { name })[0];
        }

        public void SetBladeInteger(string name, int value)
        {
            if (Obj == null) return;
            SetBladeIntegers(new string[] { name }, new int[] { value });
        }

        public bool CopyFileOnBlade(string fromFile, string toFile)
        {
            if (Obj == null) return false;
            return Obj.CopyFileOnBlade(fromFile, toFile);
        }

        public bool BladeDelFile(string FileName)
        {
            DelConfigDelegate factFileDeleteDelegate = new DelConfigDelegate(Obj.BladeDelFile);
            IAsyncResult ar = factFileDeleteDelegate.BeginInvoke(MakeKey(), FileName, null, null);
            ar.AsyncWaitHandle.WaitOne(30000, false);
            if (ar.IsCompleted) return (bool)factFileDeleteDelegate.EndInvoke(ar);
            else throw new Exception("Cannot delete file in BladeDelFile " + FileName + ".");
        }

        public void SafelyRemove()
        {
            Obj.SafeRemoveBlade();
        }

        public void PinMotionToggle()
        {
            if (Obj == null) return;
            Obj.PinMotionToggle();
        }

        public string Name()
        {
            if (Obj != null) return Obj.Name();
            else return "";
        }
        // TODO : Consider remove follow GradeFilePath FirmwareFilePath FactFilePath
        public string GradeFilePath()
        {
            if (Obj == null) return "";
            return Obj.GetStrings("", new string[] { BladeDataName.GradePath })[0];
        }

        public string FirmwareFilePath()
        {
            if (Obj == null) return "";
            return Obj.GetStrings("", new string[] { BladeDataName.FirmwarePath })[0];
        }

        public string FactFilePath()
        {
            if (Obj == null) return "";
            return Obj.GetStrings("", new string[] { BladeDataName.FactPath })[0];
        }

        // TODO :  No use in current code
        //public void SetModuleState(TesterStateStruct testerState)
        //{
        //    if (obj == null) return;
        //    obj.SetModuleState(testerState);
        //}

        public MemsStateValues GetMemsState()
        {
            if (Obj == null) return MemsStateValues.Unknown;
            return Obj.GetMemsStatus();
        }

        #endregion Service methods

        #region Blade string and integer methods
        public string GetFwRev()
        {
            return GetBladeString(BladeDataName.FwRev);
        }

        public string GetBladeType()
        {
            return GetBladeString(BladeDataName.BladeType);
        }

        public string GetSerialNumber()
        {
            return GetBladeString(BladeDataName.BladeSN);
        }

        public string GetTclStart()
        {
            return GetBladeString(BladeDataName.TclStart);
        }

        public void CardPower(bool State)
        {
            SetBladeInteger(BladeDataName.CardPower, State ? 1 : 0);
        }

        public void SetSerialNumber(string serialNumber)
        {
            SetBladeString(BladeDataName.BladeSN, serialNumber);
        }

        public void SetBladeType(string bladeType)
        {
            SetBladeString(BladeDataName.BladeType, bladeType);
        }

        public void SetMotorBaseplateSN(string serialNumber)
        {
            SetBladeString(BladeDataName.MotorBaseplateSN, serialNumber);
        }

        public void SetMotorSN(string serialNumber)
        {
            SetBladeString(BladeDataName.MotorSN, serialNumber);
        }

        public void SetActuatorSN(string serialNumber)
        {
            SetBladeString(BladeDataName.ActuatorSN, serialNumber);
        }

        public void SetDiskSN(string serialNumber)
        {
            SetBladeString(BladeDataName.DiskSN, serialNumber);
        }

        public void SetPcbaSN(string serialNumber)
        {
            SetBladeString(BladeDataName.PcbaSN, serialNumber);
        }

        public void SetJadeSN(string serialNumber)
        {
            SetBladeString(BladeDataName.JadeSN, serialNumber);
        }

        public void SetBladeLoc(string serialNumber)
        {
            SetBladeString(BladeDataName.BladeLoc, serialNumber);
        }

        public void SetMemsOpenDelay(string delayMs)
        {
            SetBladeString(BladeDataName.MemsOpenDelay, delayMs);
        }

        public void SetMemsCloseDelay(string delayMs)
        {
            SetBladeString(BladeDataName.MemsCloseDelay, delayMs);
        }

        public void SetFlexSN(string serialNumber)
        {
            SetBladeString(BladeDataName.FlexSN, serialNumber);
        }

        public void SetMemsSN(string serialNumber)
        {
            SetBladeString(BladeDataName.MemsSN, serialNumber);
        }

        public void SetTclStart(string command)
        {
            SetBladeString(BladeDataName.TclStart, command);
        }

        public void PinMotion(bool State)
        {
            SetBladeInteger(BladeDataName.PinMotion, State ? 1 : 0);
        }

        public void BackLight(bool State)
        {
            SetBladeInteger(BladeDataName.BackLight, State ? 1 : 0);
        }

        public void AuxOut0(int output)
        {
            SetBladeInteger(BladeDataName.AuxOut0, output);
        }

        public void AuxOut1(int output)
        {
            SetBladeInteger(BladeDataName.AuxOut1, output);
        }

        #endregion Blade string and integer methods

        #region internal Methods
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
                            // TODO : testObjectStreaming
                        }
                    }
                    catch (WebException ex)
                    {
                        nlogger.Error("RemoteConnectLib::Connect Create Channel WebException [Exception :{0}]", ex.Message);
                        retVal = (UInt32)ReturnValues.connectionBad;
                    }
                    catch (Exception ex)
                    {
                        // TODO : Add log information.
                        nlogger.Error("RemoteConnectLib::Connect Create Channel exception [Exception :{0}]", ex.Message);
                        retVal = (UInt32)ReturnValues.connectionBad;
                    }

                    // TODO : Add tester object streaming
                    if (_Factory == null || _TesterObject == null)
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
                            //string what = ((System.ServiceModel.ICommunicationObject)Obj).State.ToString();
                        }
                        catch (WebException ex)
                        {
                            nlogger.Error("RemoteConnectLib::Connect Create Channel WebException [Exception :{0}]", ex.Message);
                            retVal = (UInt32)ReturnValues.connectionBad;
                        }
                        catch (Exception ex)
                        {
                            // TODO : Add log information.
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

        // TODO : Think about put MakeupCompleteUrl and MakeupCompleteUrl2 together.
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
            // TODO : Check string url, streamingUrl null
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
                    // TODO : object Streaming
                }
            }
            else
            {
                // Pipe model, may be no use
                if (tobj)
                {
                    //NetNamedPipeBinding binding = new NetNamedPipeBinding();
                    //binding.MaxReceivedMessageSize = 524288;
                    //binding.MaxBufferSize = 524288;
                    //binding.MaxBufferPoolSize = 524288;
                    //binding.ReaderQuotas.MaxArrayLength = 524288;
                    //binding.ReaderQuotas.MaxBytesPerRead = 524288;
                    //binding.ReaderQuotas.MaxNameTableCharCount = 524288;
                    //binding.ReaderQuotas.MaxStringContentLength = 524288;
                    //_Factory = new DuplexChannelFactory<ITesterObject>(context, binding, endpoint);
                }
                if (tobjStr)
                {
                    // TODO : object Streaming
                }
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
                int value = _TesterObject.PingInt();
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

            // TODO : Disconnect first.
            Disconnect();

            // Close TesterObject service.
            if (_TesterObject != null && ((ICommunicationObject)_TesterObject).State == CommunicationState.Opened)
            {
                try { ((ICommunicationObject)_TesterObject).Close(); }
                catch
                {
                    // TODO :ignored
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

            // TODO :Close TesterObjectStreaming service.

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
            _ComStatusEvent?.Invoke(this, e);
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

        #endregion internal Methods

        #region dispose Methods
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
            //if (_TesterObjectStreaming != null && ((ICommunicationObject)m_testerObjectStreaming).State == CommunicationState.Opened)
            //{
            //    try { ((ICommunicationObject)m_testerObjectStreaming).Close(); }
            //    catch
            //    {
            //        // ignored
            //    }
            //}
            //else if (m_testerObjectStreaming != null)
            //{
            //    try { ((ICommunicationObject)m_testerObjectStreaming).Abort(); }
            //    catch
            //    {
            //        // ignored
            //    }
            //}
            //m_testerObjectStreaming = null;

            // Notice sys 
            _Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion dispose Methods
    }
}
