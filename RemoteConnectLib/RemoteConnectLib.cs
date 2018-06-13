using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hitachi.Tester.Enums;
using System.ServiceModel;
using Hitachi.Tester.Module;

namespace Hitachi.Tester.Client
{
    public class RemoteConnectLib : IDisposable
    {
        #region Fields
        private bool _Disposed;

        private DuplexChannelFactory<ITesterObject> factory = null;
        //private ChannelFactory<ITesterObjectStreaming> factoryStreaming = null;
        private string _CurrentUrlAddress;
        private string _CurrentUserID;
        private string _CurrentPassword;
        private bool _CurrentNetTcpConnectFlag;
        private object _ConnectLockObject;
        private bool _BusyConnecting;

        public TesterObjectCallback _BladeEventCallbackClass = null;
        public BladeEventClass BladeEvent;
        #endregion Fields

        #region Constructors
        public RemoteConnectLib()
        {
            _Disposed = false;

            _CurrentUrlAddress = string.Empty;
            _CurrentUserID = string.Empty;
            _CurrentPassword = string.Empty;
            _CurrentNetTcpConnectFlag = false;
            _ConnectLockObject = new object();
            _BusyConnecting = false;

            _BladeEventCallbackClass = new TesterObjectCallback(this);

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

        #endregion Properties

        #region Methods
        public UInt32 Connect(string urlAddress, string userID, string password, bool netTcp, bool tobj, bool tobjStr)
        {
            UInt32 retVal = (UInt32)ReturnValues.connectionBad;

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
                string strUrl2 = MakeupCompleteUrl2(strUrl, netTcp);

                InstanceContext context;
                EndpointAddress endpoint;
                EndpointAddress endpoint2;

                // TODO : Add connect function
                
            }
            return retVal;
        }

        // TODO : 
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

        private string MakeupCompleteUrl2( string strUrl, bool netTcp )
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
