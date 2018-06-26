using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ServiceModel;
using System.IO;
using System.Windows.Forms;

using Hitachi.Tester;
using Hitachi.Tester.Enums;
using Hitachi.Tester.Sequence;

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
        private List<TestSequence> _CurrentSequenceList;
        private ProxyListClass _CallbackProxyList;
        private delegate void PingDelegate(string name);
        private delegate void StartTestSequenceDelegate(string ParseString, string TestName, string GradeName, int StartingTest, bool BreakOnError, string tableStr);
        /// <summary>
        /// Use for monitor all status
        /// </summary>
        internal volatile TesterState _TesterState;

        private volatile bool _Exit = false;
        private volatile bool _Escape = false;
        private volatile bool _SavingSettings = false;
        private volatile bool _SetConfigGoing = false;

        #endregion Fields

        #region Constructors
        public TesterObject()
        {
            _Disposed = false;
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

        #region ITesterObject Methods
        #region part one
        public UInt32 Connect(string userID, string password, string computerName)
        {
            UInt32 retVal = (UInt32)ReturnValues.bladeAccessBit;
            try
            {
                WriteLine(string.Format("TesterObject::Connect Request from [userID:{0}] [ComputerName:{1}]", userID, computerName));
                ITesterObjectCallback proxy = OperationContext.Current.GetCallbackChannel<ITesterObjectCallback>();
                ProxyStruct aProxyStruct = new ProxyStruct(computerName, userID, proxy);
                _CallbackProxyList.Add(aProxyStruct);
            }
            catch (Exception e)
            {
                WriteLine("TesterObject::Connect Exception: " + makeUpExceptionString(e).ToString());
                throw e;
            }
            WriteLine(string.Format("TesterObject::Connect Granted to [userID:{0}] [ComputerName:{1}] [retVal:{2}]", userID, computerName, retVal));

            return retVal;
        }

        public void Disconnect(string userID, string computerName)
        {
            WriteLine(string.Format("TesterObject::Disconnect [userID:{0}] [ComputerName:{1}]", userID, computerName));
            if (_CallbackProxyList != null)
            {
                _CallbackProxyList.Remove(computerName, userID);
                WriteLine(string.Format("TesterObject::Disconnect success [userID:{0}] [ComputerName:{1}] [CallbackListNum:{2}]", userID, computerName, _CallbackProxyList.Count));
            }
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
        #endregion part one

        #region part two
        public string Ping(string message)
        {
            string retVal = message;
            // TODO : TesterState

            PingDelegate aPingDelegate = new PingDelegate(ping);
            aPingDelegate.BeginInvoke(message, pingCallback, aPingDelegate);
            // TODO
            return retVal;
        }

        /// <summary>
        /// Reads a test Sequence file from the grade dir and writes to TesterObject.
        /// To write file from Jade, call bladeFileWrite and then this.
        /// </summary>
        /// <param name="NewConfig"></param>
        public void SetConfig(string NewConfig)
        {
            FileStream fs1 = null;
            try
            {
                WriteLine("SetConfig called " + NewConfig);
                // Translate into a real path.
                string fromWholePath = fixUpThePaths(NewConfig);

                // Get destination dir (from our translated path).
                string destinationDir = Path.GetDirectoryName(fromWholePath);
                // Does it exist?
                if (!Directory.Exists(destinationDir))
                {
                    WriteLineContent("SetConfig Source dir does not exist " + destinationDir);
                    return;
                }
                // Open file stream.
                fs1 = new FileStream(fromWholePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                TestSequence newSequence = new TestSequence();
                // Read file into sequence struct.
                newSequence.ReadInTests((Stream)fs1, false);
                fs1.Close();

                // Set fileName (was changed when writing to temp file before getting here).
                newSequence.FileName = Path.GetFileNameWithoutExtension(fromWholePath);

                // if saving this struct then wait
                while (_SavingSettings)
                {
                    if (_Exit || _Escape) return;
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                // if already doing then wait
                while (_SetConfigGoing)
                {
                    if (_Exit || _Escape)
                    {
                        return;
                    }
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                _SetConfigGoing = true;

                // Remove any old instances of this sequence name.
                for (int i = 0; i < _CurrentSequenceList.Count; i++)
                {
                    // get one sequence from the list.
                    TestSequence testSeq = _CurrentSequenceList[i];
                    // See it this is the same name
                    if (testSeq.FileName == newSequence.FileName)
                    {
                        // If the name is the same as new one then delete old.
                        _CurrentSequenceList.RemoveAt(i);
                        i--;
                    }
                }

                // add new one
                _CurrentSequenceList.Add(newSequence);
                WriteLineContent("File read from: " + fromWholePath);
                // SaveSettings();
            }
            catch (Exception e) // Catch for debug; can comment out.
            {
                throw e;
            }
            finally
            {
                _SetConfigGoing = false;
                if (fs1 != null)
                {
                    try
                    {
                        fs1.Dispose();
                    }
                    catch { }
                }
            }
        }
        #endregion part two
        #endregion ITesterObject Methods

        #region internal sup Methods
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

        private StringBuilder makeUpExceptionString(Exception e)
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

        private string fixUpThePaths(string path)
        {
            string wholePath = "";
            if (path.Length == 0)
            {
                wholePath = ".";
            }
            // if drive == bunny then use bunny path from settings dialog
            else if (path.Trim().ToLower().StartsWith("bunny:") ||
                     path.Trim().ToLower().StartsWith("flash:") ||
                     path.Trim().ToLower().StartsWith("blade:"))
            {
                wholePath = PathCombine(BladePath, path.Trim().Substring(6));
            }
            // if drive == fact then use fact path from settings dialog
            else if (path.Trim().ToLower().IndexOf("fact:") == 0)
            {
                wholePath = PathCombine(FactPath, path.Trim().Substring(5));
            }
            // if drive == fact then use grade path from settings dialog
            else if (path.Trim().ToLower().IndexOf("grade:") == 0)
            {
                wholePath = PathCombine(GradePath, path.Trim().Substring(6));
            }
            // if drive == firm then use firmware path from settings dialog
            else if (path.Trim().ToLower().IndexOf("firmware:") == 0)
            {
                wholePath = PathCombine(FirmwarePath, path.Trim().Substring(9));
            }
            // if drive == firm then use firmware path from settings dialog
            else if (path.Trim().ToLower().IndexOf("firm:") == 0)
            {
                wholePath = PathCombine(FirmwarePath, path.Trim().Substring(5));
            }
            // if drive == counts then use counts path from settings dialog
            else if (path.Trim().ToLower().IndexOf("counts:") == 0 ||
                     path.Trim().ToLower().IndexOf("disk:") == 0)
            {
                wholePath = PathCombine(CountsPath, path.Trim().Substring(7));
            }
            else if (path.Trim().ToLower().IndexOf("stats:") == 0)
            {
                wholePath = PathCombine(CountsPath, path.Trim().Substring(6));
            }
            // Look for our special data path
            else if ((path.Trim().ToLower().StartsWith("data:\\")) || (path.Trim().ToLower().StartsWith("data:/")))
            {
                wholePath = @System.IO.Path.Combine(SpecialFileClass.SpecialFileFolder, path.Trim().Substring(6));
            }
            else if (path.Trim().ToLower().StartsWith("data:"))
            {
                wholePath = @System.IO.Path.Combine(SpecialFileClass.SpecialFileFolder, path.Trim().Substring(5));
            }
            // look for application startup
            else if ((path.Trim().ToLower().StartsWith("app:\\")) || (path.Trim().ToLower().StartsWith("app:/")))
            {
                wholePath = @System.IO.Path.Combine(Application.StartupPath, path.Trim().Substring(5));
            }
            else if (path.Trim().ToLower().StartsWith("app:"))
            {
                wholePath = @System.IO.Path.Combine(Application.StartupPath, path.Trim().Substring(4));
            }
            // look for log 
            else if ((path.Trim().ToLower().StartsWith("log:\\")) || (path.Trim().ToLower().StartsWith("log:/")))
            {
                wholePath = @System.IO.Path.Combine(LogPath, path.Trim().Substring(5));
            }
            else if (path.Trim().ToLower().StartsWith("log:"))
            {
                wholePath = @System.IO.Path.Combine(LogPath, path.Trim().Substring(4));
            }
            // look for debug
            else if ((path.Trim().ToLower().StartsWith("debug:\\")) || (path.Trim().ToLower().StartsWith("debug:/")))
            {
                wholePath = @System.IO.Path.Combine(DebugPath, path.Trim().Substring(7));
            }
            else if (path.Trim().ToLower().StartsWith("debug:"))
            {
                wholePath = @System.IO.Path.Combine(DebugPath, path.Trim().Substring(6));
            }
            // look for debug
            else if ((path.Trim().ToLower().StartsWith("result:\\")) || (path.Trim().ToLower().StartsWith("result:/")))
            {
                wholePath = @System.IO.Path.Combine(ResultPath, path.Trim().Substring(8));
            }
            else if (path.Trim().ToLower().StartsWith("result:"))
            {
                wholePath = @System.IO.Path.Combine(ResultPath, path.Trim().Substring(7));
            }
            else
            {
                @wholePath = @path.Trim();
            }

            return wholePath;
        }

        private string PathCombine(string @path1, string @path2)
        {
            bool bDone = false;
            while (!bDone)
            {
                if (path1.LastIndexOfAny(new char[] { '\\', '/' }) == path1.Length - 1)
                {
                    path1 = path1.Substring(0, path1.Length - 1);
                }
                else
                {
                    bDone = true;
                }
            }

            bDone = false;
            while (!bDone)
            {
                if (path2.IndexOfAny(new char[] { '\\', '/' }) == 0)
                {
                    path2 = path2.Substring(1);
                }
                else
                {
                    bDone = true;
                }
            }
            return path1 + "\\" + path2;
        }
        #endregion internal sup Methods

        #region Log System
        internal void WriteLine(string Text)
        {
            if (Text == null) return;
            Thread WriteLineThread = new Thread(new ParameterizedThreadStart(WriteLineThreadFunc));
            WriteLineThread.IsBackground = true;
            WriteLineThread.Start((object)Text);
        }

        private void WriteLineThreadFunc(object passingObject)
        {
            // TODO : Achieve write line function
        }

        internal void WriteLineContent(string Text)
        {
            if (Text == null) return;
            Thread WriteLineContentThread = new Thread(new ParameterizedThreadStart(WriteLineContentThreadFunc));
            WriteLineContentThread.IsBackground = true;
            WriteLineContentThread.Start((object)Text);
        }

        private void WriteLineContentThreadFunc(object passingObject)
        {
            // TODO : Achieve write line content function
        }
        #endregion Log System

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
