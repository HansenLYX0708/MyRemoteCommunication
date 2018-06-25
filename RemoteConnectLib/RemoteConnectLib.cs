using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ServiceModel;
using System.Net;

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
        private object ITesterObjectLock = new object();
        // TODO : private object ITesterObjectStreamLock = new object();
        private System.Threading.Timer _KeepAliveTimer;
        public bool _KeepAliveArrived;

        private BladeEventClass _BladeEvent;

        private bool _Connected = false; // Flag to see if we have ever connected.

        // Justin
        static private Dictionary<string, RemoteConnectLib> _Connections;
        
        #endregion Fields

        #region Constructors
        public RemoteConnectLib()
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

            _BladeEventCallbackClass = new TesterObjectCallback(this);
            _BladeEvent = new BladeEventClass(this);

            // After connected, timer pings host every now and then to keep the channel awake.
            _KeepAliveTimer = new System.Threading.Timer(keepAliveTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
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
                if (_TesterObject == null || ((System.ServiceModel.ICommunicationObject)_TesterObject).State != CommunicationState.Opened)
                {
                    if (_TesterObject != null)
                    {
                        ((System.ServiceModel.ICommunicationObject)_TesterObject).Abort();
                    }
                    Connect(_CurrentUrlAddress, _CurrentUserID, _CurrentPassword, _CurrentNetTcpConnectFlag, true, false);
                }
                return _TesterObject;
            }
        }
        #endregion Properties

        #region Methods
        public UInt32 Connect(string urlAddress, string userID, string password)
        {
            _KeepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            UInt32 value = Connect(urlAddress, userID, password, true, true, true);
            _KeepAliveTimer.Change(Constants.KeepAliveTimeout, Timeout.Infinite);
            return value;
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

            // TODO:Determine whether the connection has been made, or reconnect put to keep alive function.

            // _BusyConnecting keep only one connect operation in one class.
            if (!_BusyConnecting)
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
                nlogger.Info("RemoteConnectLib::Connect [strUrl:{0}] [strStreamUrl:{0}]", strUrl, strStreamUrl);
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
                        CommunicationState chanState = ((System.ServiceModel.ICommunicationObject)_TesterObject).State;
                        if (chanState != CommunicationState.Opened)
                        {
                            ((System.ServiceModel.ICommunicationObject)_TesterObject).Open();
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
                    if ((retVal & (int)ReturnValues.bladeAccessBit) == 0)
                    {
                        _Connected = true;
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
            catch (System.Exception ex)
            {
                nlogger.Info("RemoteConnectLib::Disconnect delegate error [ex.message:{0}]", ex.Message);
            }
            nlogger.Info("RemoteConnectLib::Disconnect end");
        }

        // TODO : Think about put MakeupCompleteUrl and MakeupCompleteUrl2 together.
        private string MakeupCompleteUrl( string urlAddress, bool netTcp )
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
        /// 
        /// </summary>
        /// <param name="state"></param>
        private void keepAliveTimer_Tick(object state)
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

            Disconnect();
            // TODO : Disconnect first.

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
        #endregion Methods

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
