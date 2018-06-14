using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ServiceModel;

using Hitachi.Tester;
using Hitachi.Tester.Module;
using Hitachi.Tester.Enums;

namespace Hitachi.Tester.Module
{
    /// <summary>
	/// Summary description for TesterObject.
	/// </summary>
    [ServiceBehavior(UseSynchronizationContext = false,
        ConcurrencyMode = ConcurrencyMode.Reentrant,
        InstanceContextMode = InstanceContextMode.Single)]
    public partial class TesterObject : ITesterObject, IDisposable
    {
        #region Fields
        private bool _Disposed;

        private ProxyListClass _CallbackProxyList;

        private delegate void PingDelegate(string name);
        private delegate void StartTestSequenceDelegate(string ParseString, string TestName, string GradeName, int StartingTest, bool BreakOnError, string tableStr);

        private volatile bool _Exit;
        private volatile bool _Escape;

        /// <summary>
        /// Use for monitor all status
        /// </summary>
        internal volatile TesterState _TesterState;

        #endregion Fields

        #region Constructors
        public TesterObject()
        {
            _Disposed = false;
            _Exit = false;
            _Escape = false;

            _TesterState = new TesterState();

            try
            {
                _CallbackProxyList = new ProxyListClass();
            }
            catch
            { }
            // TODO : The following complex processes should not exist in the constructor and should be considered for removal
            _BladeEventsThread = new Thread(doBladeEvents);
            _BladeEventsThread.IsBackground = true;
            _BladeEventsThread.Start();
        }

        ~TesterObject()
        {
            Dispose(false);
        }
        #endregion Constructors

        #region Properties

        #endregion Properties

        #region Methods
        public UInt32 Connect(string userID, string password, string computerName)
        {
            UInt32 retVal = (UInt32)ReturnValues.bladeAccessBit;
            try
            {
                ITesterObjectCallback proxy = OperationContext.Current.GetCallbackChannel<ITesterObjectCallback>();
                ProxyStruct aProxyStruct = new ProxyStruct(computerName, userID, proxy);
                _CallbackProxyList.Add(aProxyStruct);
            }
            catch( Exception e )
            {
                throw e;
            }
            return retVal;
        }

        public void Disconnect(string userID, string computerName)
        {
            if (_CallbackProxyList != null)
            {
                _CallbackProxyList.Remove(computerName, userID);
            }
        }

        public void Initialize(string key)
        {
            // TODO : 需要研究
        }

        public string Ping(string message)
        {
            string retVal = message;
            // TODO : TesterState 的相关操作

            PingDelegate aPingDelegate = new PingDelegate(ping);
            aPingDelegate.BeginInvoke(message, pingCallback, aPingDelegate);
            // TODO : 需要确认这里的返回值是否有必要
            return retVal;
        }

        /// <summary>
        /// Service to maintain a connection
        /// </summary>
        /// <returns>Keep Alive Timeout</returns>
        public int PingInt()
        {
            // send StatusEvent
            StatusEventArgs args = new StatusEventArgs();
            args.Text = Constants.KeepAliveString;
            args.EventType = (int)eventInts.KeepAliveEvent;
            SendStatusEvent(this, args);
            return Constants.KeepAliveTimeout;  // Just some known number.
        }

        public bool AbortSequence(string reason, bool force)
        {
            bool retVal = false;
            // TODO : Stop queue of test, restart TCL.
            return retVal;
        }

        public void StartTest(string ParseString, string TestName, string GradeName, string tableStr)
        {
            StartTestSequenceDelegate startTestDelegate = new StartTestSequenceDelegate(StartTestSequence);
            startTestDelegate.BeginInvoke(ParseString, TestName, GradeName, 0, false, tableStr, new AsyncCallback(delegate (IAsyncResult ar) { startTestDelegate.EndInvoke(ar); }), startTestDelegate);
        }

        public StringBuilder makeUpExceptionString(Exception e)
        {
            // build up error message
            StringBuilder message = new StringBuilder();
            for (Exception ee = e; ee != null; ee = ee.InnerException)
            {
                try { message.Append(ee.Message); message.Append(Environment.NewLine); }
                catch { }
                try { message.Append(ee.Source); message.Append(Environment.NewLine); }
                catch { }
                try { message.Append(ee.TargetSite.ToString()); message.Append(Environment.NewLine); }
                catch { }
                try { message.Append(ee.StackTrace); message.Append(Environment.NewLine); }
                catch { }
            }
            return message;
        }
        #endregion Methods

        #region Internal Methods
        private void ping(string name)
        {
            // send StatusEvent
            // TODO : Test all event type
        }

        private void pingCallback(IAsyncResult ar)
        {
            try
            {
                PingDelegate aPingDelegate = (PingDelegate)ar.AsyncState;
                aPingDelegate.EndInvoke(ar);
            }
            catch
            { }
        }

        private void StartTestSequence(string ParseString, string TestName, string GradeName, int StartingTest, bool BreakOnError, string tableStr)
        {
            // TODO : If doing test queue now, refer to the old code?
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
