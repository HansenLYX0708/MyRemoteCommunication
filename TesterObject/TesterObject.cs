using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ServiceModel;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Xml;

using Hitachi.Tester.Enums;
using Hitachi.Tester.Sequence;
using HGST.Blades;
using RemoveDriveByLetter;

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
        private Thread _BladeEventsThread;
        private BunnyBoard _BunnyCard;
        private List<BunnyBoard> _Boards;

        private volatile bool _Exit;
        private volatile bool _Escape;
        private volatile bool _SavingSettings;
        private volatile bool _SetConfigGoing;
        private volatile bool _DelConfigGoing;

        private string _FactPath;
        private string _GradePath;
        private string _FirmwarePath;
        private string _ResultPath;
        private string _LogPath;
        private string _DebugPath;
        private string _CountsPath;
        private string _TclPath;
        private string _BladePath;
        #endregion Fields

        #region Constructors
        public TesterObject()
        {
            // Read only fields start
            // Read only fileds must in constructor
            // Read voltage limits from App.config file.
            _In5vMin = GetLimitFromAppConfig("Vcc5NoLoadMin", 4.25);
            _In5vMax = GetLimitFromAppConfig("Vcc5NoLoadMax", 5.75);
            _InLoad5vMin = GetLimitFromAppConfig("Vcc5LoadMin", 4.25);
            _InLoad5vMax = GetLimitFromAppConfig("Vcc5LoadMax", 5.75);
            _Sw5vMin = GetLimitFromAppConfig("Vcc5SwitchMin", 4.25);
            _Sw5vMax = GetLimitFromAppConfig("Vcc5SwitchMax", 5.75);
            _In12vMin = GetLimitFromAppConfig("Vcc12NoLoadMin", 11.0);
            _In12vMax = GetLimitFromAppConfig("Vcc12NoLoadMax", 13.0);
            _InLoad12vMin = GetLimitFromAppConfig("Vcc12LoadMin", 11.0);
            _InLoad12vMax = GetLimitFromAppConfig("Vcc12LoadMax", 13.0);
            _Sw12vMin = GetLimitFromAppConfig("Vcc12SwitchMin", 11.0);
            _Sw12vMax = GetLimitFromAppConfig("Vcc12SwitchMax", 13.0);

            _RequestedLockObj = new object();
            _MemsStatusLockObj = new object();
            // Read only fields end

            Init();
        }

        private void Init()
        {
            _CurrentSequenceList = new List<TestSequence>();  // holds all of our defined sequences
            _Disposed = false;
            _BladePath = "";
            _FactPath = "";
            _GradePath = "";
            _FirmwarePath = "";
            _ResultPath = "";
            _LogPath = "";
            _DebugPath = "";
            _CountsPath = "";

            _Exit = false;
            _Escape = false;
            _SavingSettings = false;
            _SetConfigGoing = false;
            _DelConfigGoing = false;

            SimpleBladeInfoInit();

            _TesterState = new TesterState();
            try
            {
                _CallbackProxyList = new ProxyListClass();
            }
            catch
            { }
            // TODO : The following complex processes should not exist in the constructor and should be considered for removal
            _BladeEventsThread = new Thread(DoBladeEvents)
            {
                IsBackground = true
            };
            _BladeEventsThread.Start();
        }

        ~TesterObject()
        {
            Dispose(false);
        }
        #endregion Constructors

        #region Properties
        private string BladePath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_BladePath))
                {
                    _BladePath = "F:";
                }
                return _BladePath;
            }
            set
            {
                // Used to set MOCK flash drive if no Bunny card
                _BladePath = value;
            }
        }

        private string FactPath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_FactPath))
                {
                    _FactPath = "C:\\FACT";
                }
                return _FactPath;
            }

            set
            {
                _FactPath = value;
            }
        }

        private string GradePath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_GradePath))
                {
                    _GradePath = "C:\\Grade";
                }
                return _GradePath;
            }

            set
            {
                _GradePath = value;
            }
        }

        private string FirmwarePath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_FirmwarePath))
                {
                    _FirmwarePath = "C:\\FW";
                }
                return _FirmwarePath;
            }

            set
            {
                _FirmwarePath = value;
            }
        }

        private string ResultPath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_ResultPath))
                {
                    _ResultPath = "C:\\Result";
                }
                return _ResultPath;
            }

            set
            {
                _ResultPath = value;
            }
        }

        private string LogPath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_LogPath))
                {
                    _LogPath = "C:\\Log";
                }
                return _LogPath;
            }

            set
            {
                _LogPath = value;
            }
        }

        private string DebugPath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_DebugPath))
                {
                    _DebugPath = "C:\\Debug";
                }
                return _DebugPath;
            }

            set
            {
                _DebugPath = value;
            }
        }

        public string CountsPath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_CountsPath))
                {
                    _CountsPath = "C:\\Counts";
                }
                return _CountsPath;
            }
            set
            {
                _CountsPath = value;
            }
        }

        private string TclPath
        {
            get
            {
                // if not set then assign default.
                if (string.IsNullOrEmpty(_TclPath))
                {
                    _TclPath = "C:\\Tcl\\bin\\tclsh85.exe";
                }
                return _TclPath;
            }

            set
            {
                _TclPath = value;
            }
        }

        private string TclStart { get; set; }
        #endregion Properties

        #region ITesterObject base function
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
                WriteLine("TesterObject::Connect Exception: " + MakeUpExceptionString(e).ToString());
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

        public string Ping(string message)
        {
            string retVal = message;
            // TODO : TesterState

            WriteLine(string.Format("TesterObject::Ping"));
            WriteLineContent("TesterObject::Ping" + message);

            PingDelegate aPingDelegate = new PingDelegate(PingOperation);
            aPingDelegate.BeginInvoke(message, PingCallback, aPingDelegate);
            // TODO
            return retVal;
        }

        /// <summary>
        /// Service to maintain a connection
        /// </summary>
        /// <returns>Keep Alive Timeout</returns>
        public int PingInt()
        {
            // send StatusEvent
            StatusEventArgs args = new StatusEventArgs
            {
                Text = Constants.KeepAliveString,
                EventType = (int)eventInts.KeepAliveEvent
            };
            SendStatusEvent(this, args);
            return Constants.KeepAliveTimeout;  // Just some known number.
        }

        ///<summary>
        /// Returns directory list to client.  
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Filter"></param>
        /// <returns></returns>
        public FileNameStruct[] BladeFileDir(string path, string Filter)
        {
            WriteLine("TesterObject::BladeFileDir called ");
            WriteLineContent("TesterObject::BladeFileDir called " + Filter);
            ArrayList gradeList = new ArrayList();
            try
            {
                // get directory from Windows.
                string[] fileList;
                //string[] dirList;
                string wholePath = FixUpThePaths(path);

                try
                {
                    fileList = Directory.GetFiles(wholePath, Filter);
                }
                catch (DirectoryNotFoundException e)
                {
                    throw new Exception("Invalid path: Check \"Path to Grade files\" in BladeRunner.", e);
                }
                // open each file and get internal version and date
                for (int i = 0; i < fileList.Length; i++)
                {
                    FileStream fs = null;
                    try
                    {
                        FileNameStruct oneFile = new FileNameStruct
                        {
                            FileNameStr = Path.GetFileName(fileList[i])
                        };
                        FileInfo fi = new FileInfo(fileList[i]);
                        try
                        {
                            fs = new FileStream(fileList[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                            // we parse all of the xml stuff with this
                            XmlDocument xmlTestDoc = new XmlDocument();
                            // load the file
                            xmlTestDoc.Load(fs);
                            oneFile.VersionStr = xmlTestDoc.ChildNodes[1].Attributes[2].Value;
                            for (int j = 0; j < xmlTestDoc.ChildNodes[1].ChildNodes.Count; j++)
                            {
                                switch (xmlTestDoc.ChildNodes[1].ChildNodes[j].Name)
                                {
                                    case "date":
                                        oneFile.DateStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].InnerText + " " + fi.LastWriteTime.ToShortTimeString();
                                        break;
                                    case "binSpecTable":
                                        oneFile.BinVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                    case "skipSpecTable":
                                        oneFile.SkipVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                    case "ocrSpecTable":
                                        oneFile.OcrVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                    case "gradeSpecTable":
                                        oneFile.GradeVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                    case "rankDispositionTable":
                                        oneFile.DispVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                    case "RetryTable":
                                        oneFile.RetryDispoVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                    case "testerLimitsTable":
                                        oneFile.TestCountVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                    case "trayDispositionTable":
                                        oneFile.TrayDispoVerStr = xmlTestDoc.ChildNodes[1].ChildNodes[j].Attributes[0].Value;
                                        break;
                                } // end switch
                            } // end for
                        }
                        catch
                        {
                            oneFile.DateStr = fi.LastWriteTime.ToShortDateString() + " " + fi.LastWriteTime.ToShortTimeString();
                        }
                        gradeList.Add(oneFile);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        try { fs.Close(); }
                        catch { }
                        try { fs.Dispose(); }
                        catch { }
                    }
                }
            }
            catch (Exception ee)
            {
                throw ee;
            }
            return (FileNameStruct[])gradeList.ToArray(typeof(FileNameStruct));
        }

        /// <summary>
        /// Takes a source path and a destination path and copies a file.
        /// </summary>
        /// <param name="fromFile"></param>
        /// <param name="toFile"></param>
        /// <returns></returns>
        public bool CopyFileOnBlade(string fromFile, string toFile)
        {
            bool retVal = false;
            FileStream fs1 = null;
            FileStream fs2 = null;

            // Check path
            string fromWholePath = FixUpThePaths(fromFile);
            string toWholePath = FixUpThePaths(toFile);
            string destinationDir = Path.GetDirectoryName(toWholePath);
            if (!Directory.Exists(destinationDir))
            {
                // Make dir.
                Directory.CreateDirectory(destinationDir);
            }
            try
            {
                fs1 = new FileStream(fromWholePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs2 = new FileStream(toWholePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                TxFile(fs1, fs2);
                retVal = true;
            }
            catch (Exception e)
            {
                WriteLineContent("TesterObject::CopyFileOnBlade exception");
                throw new Exception("CopyFileOnBlade " + fromWholePath + " " + toWholePath + " ", e);
            }
            finally
            {
                if (fs2 != null)
                {
                    fs2.Flush();
                    fs2.Close();
                    fs2.Dispose();
                }
                if (fs1 != null)
                {
                    fs1.Close();
                    fs1.Dispose();
                }
            }
            return retVal;
        }

        /// <summary>
        /// Delete file in this remote module.
        /// Returns true if file not there after delete.
        /// Returns false if something wrong.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool BladeDelFile(string Key, string FileName)
        {
            try
            {
                WriteLine("TesterObject::BladeDelFile called ");
                string wholePath = FixUpThePaths("TesterObject::BladeDelFile:" + FileName);
                WriteLineContent(wholePath);
                File.Delete(wholePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Officially "eject" blade flash drive.
        /// </summary>
        public void SafeRemoveBlade()
        {
            //TODO: Do not hard code F:
            RemoveDriveTools.RemoveDrive("F:");
        }

        public bool BunnyReInit()
        {
            WriteLine("TesterObject::BunnyReInit called ");
            bool bunnyOK = false;
            try
            {
                bunnyOK = UsbReset(0);
                if (!bunnyOK)
                {
                    throw new Exception("Blade controller Init failed.");
                }

                // if possible reload all of the stuff
                ReadBunnyStatusAndUpdateFlags();
            }
            catch (Exception e)
            {
                WriteLineContent(MakeUpExceptionString(e).ToString());
            }
            return bunnyOK;
        }

        /// <summary>
        /// Returns the current state of the remote module.  
        /// Clients can connect at any time to this singleton module.  A new client must call 
        /// this to see what is happening.
        /// </summary>
        /// <returns></returns>
        public TesterState GetModuleState()
        {
            WriteLine("TesterObject::GetModuleState called ");
            WriteLineContent("TesterObject::GetModuleState called " + _TesterState.ToString());
            return _TesterState;
        }

        public void SetModuleState(TesterState testerState)
        {
            WriteLine("TesterObject::SetModuleState called ");
            WriteLineContent("TesterObject::SetModuleState called " + _TesterState.ToString());
            Thread setModuleThread = new Thread(new ParameterizedThreadStart(SetModuleStateThreadFunc))
            {
                IsBackground = true
            };
            setModuleThread.Start(testerState);
        }

        // TODO : no use in old code
        public void GetBunnyStatus()
        {
            Thread getBunnyStatusThread = new Thread(new ThreadStart(GetBunnyStatusThreadFunc))
            {
                IsBackground = true
            };
            getBunnyStatusThread.Start();
        }

        public Enums.MemsStateValues GetMemsStatus()
        {
            return (Enums.MemsStateValues)MemsStatus;
        }

        #endregion ITesterObject base function

        #region Tester function
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
                WriteLine("TesterObject::SetConfig called " + NewConfig);
                // Translate into a real path.
                string fromWholePath = FixUpThePaths(NewConfig);

                // Get destination dir (from our translated path).
                string destinationDir = Path.GetDirectoryName(fromWholePath);
                // Does it exist?
                if (!Directory.Exists(destinationDir))
                {
                    WriteLineContent("TesterObject::SetConfig Source dir does not exist " + destinationDir);
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
                SaveSettings();
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

        /// <summary>
        /// This is a remotable function (see ITesterObject)
        /// This function reads sequence file from TesterObject and writes to the grade directory.
        /// Call this and then call BladeFileRead to read file into Jade.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="TestName"></param>
        /// <returns></returns>
        public void GetConfig(string TestName)
        {
            WriteLine("GetConfig called " + TestName);

            string translatedPath = FixUpThePaths(TestName);
            string justFilename = Path.GetFileNameWithoutExtension(translatedPath);
            if (justFilename.Length >= 0)
            {
                try
                {
                    // See if the requested TestName is in the list
                    for (int i = 0; i < _CurrentSequenceList.Count; i++)
                    {
                        // Is this the one?
                        if (_CurrentSequenceList[i].dictionaryHeader["Name"].Trim().ToLower() == justFilename.Trim().ToLower())
                        {
                            // Write file to requested location.
                            _CurrentSequenceList[i].WriteTests(translatedPath, false);
                            WriteLineContent("File saved at: " + translatedPath);
                            return;
                        }
                    }
                }
                catch (Exception e) // for debug; can comment out.
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// This is a remotable function (see ITesterObject)
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="TestName"></param>
        /// <returns></returns>
        public bool DelConfig(string Key, string TestName)
        {
            WriteLine("DelConfig called ");
            WriteLineContent(TestName);

            // if saving this struct then wait
            while (_SavingSettings)
            {
                if (_Exit || _Escape) return true;
                Application.DoEvents();
                Thread.Sleep(10);
            }
            // if already doing then wait
            while (_DelConfigGoing)
            {
                if (_Exit || _Escape)
                {
                    return true;
                }
                Application.DoEvents();
                Thread.Sleep(10);
            }
            try
            {
                _DelConfigGoing = true;

                // Loop through all existing ones and see if it is already there.
                for (int i = 0; i < _CurrentSequenceList.Count; i++)
                {
                    // Is this the same one?
                    if (_CurrentSequenceList[i].dictionaryHeader["Name"].Trim().ToLower() == TestName.Trim().ToLower())
                    {
                        // Remove old instance.
                        _CurrentSequenceList.RemoveAt(i);
                        return true;
                    }
                }
                SaveSettings();
            }
            finally
            {
                _DelConfigGoing = false;
            }
            return false;
        }

        /// <summary>
        /// Starts and runs the requested sequence given in TestName.
        ///  Should be called StartSequence.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="TestName"></param>
        /// <returns></returns>
        public void StartTest(string ParseString, string TestName, string GradeName, string tableStr)
        {
            //testerState.gradeName = GradeName;
            // tell TCL to start testing
            //return StartTestSequence(ParseString, TestName, GradeName, 0, false, tableStr);
            StartTestSequenceDelegate startTestDelegate = new StartTestSequenceDelegate(StartTestSequence);
            startTestDelegate.BeginInvoke(ParseString, TestName, GradeName, 0, false, tableStr, new AsyncCallback(delegate (IAsyncResult ar) { startTestDelegate.EndInvoke(ar); }), startTestDelegate);
        }

        #endregion Tester function

        #region internal sup Methods
        private void PingOperation(string name)
        {
            try
            {
                // send StatusEvent
                StatusEventArgs args = new StatusEventArgs
                {
                    Text = "StatusEvent " + name,
                    EventType = (int)eventInts.PingStatusEvent
                };
                SendStatusEvent(this, args);
                WriteLine("TestObject::Ping:PingOperation StatusEvent - fired");
                WriteLineContent("TestObject::Ping-PingOperation StatusEvent " + args.EventType + " " + args.Text);

                // send SequenceStartedEvent
                StartedEventArgs sargs = new StartedEventArgs("SequenceStartedEvent " + name, "", 0, 0);
                SendSequenceStartedEvent(this, sargs);
                WriteLine("TestObject::Ping:PingOperation SequenceStartedEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation SequenceStartedEvent" + args.EventType + " " + sargs.seqName);

                // send TestStartedEvent
                args = new StatusEventArgs("TestStartedEvent " + name, 0);
                SendTestStartedEvent(this, args);
                WriteLine("TestObject::Ping:PingOperation TestStartedEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation TestStartedEvent " + args.EventType + " " + args.Text);

                // send BunnyEvent
                args = new StatusEventArgs("BunnyEvent " + name, 0);
                SendBunnyEvent(this, args);
                WriteLine("TestObject::Ping:PingOperation BunnyEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation BunnyEvent " + args.EventType + " " + args.Text);

                // send TestCompletedEvent
                CompletedEventArgs cargs = new CompletedEventArgs("TestCompletedEvent " + name, 0, 1, 0, false);
                SendTestCompletedEvent(this, cargs);
                WriteLine("TestObject::Ping:PingOperation TestCompletedEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation TestCompletedEvent " + cargs.testNum + " " + cargs.testCount + " " + cargs.fail.ToString() + " " + cargs.Text);

                // send SequenceAbortingEvent
                args = new StatusEventArgs("SequenceAbortingEvent " + name, 0);
                SendSequenceAbortingEvent(this, args);
                WriteLine("TestObject::Ping:PingOperation SequenceAbortingEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation SequenceAbortingEvent " + args.EventType + " " + args.Text);

                // send SequenceCompleteEvent
                args = new StatusEventArgs("SequenceCompleteEvent " + name, 0);
                this.SendSequenceCompletedEvent(this, args);
                WriteLine("TestObject::Ping:PingOperation SequenceCompleteEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation SequenceCompleteEvent " + args.EventType + " " + args.Text);

                // send SequenceUpdateEvent
                args = new StatusEventArgs("SequenceUpdateEvent " + name, 0);
                SendSequenceUpdateEvent(this, args);
                WriteLine("TestObject::Ping:PingOperation SequenceUpdateEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation SequenceUpdateEvent " + args.EventType + " " + args.Text);

                // send ProgramClosingEvent
                args = new StatusEventArgs("ProgramClosingEvent " + name, 0);
                SendProgramClosingEvent(this, args);
                WriteLine("TestObject::Ping:PingOperation ProgramClosingEvent - fired");
                WriteLineContent("TestObject::Ping:PingOperation ProgramClosingEvent " + args.EventType + " " + args.Text);
            }
            catch (Exception e)
            {
                WriteLine("TestObject::Ping:PingOperation Exception ");
                WriteLineContent("TestObject::Ping:PingOperation Exception " + MakeUpExceptionString(e) + Environment.NewLine);
            }
        }

        private void PingCallback(IAsyncResult ar)
        {
            try
            {
                PingDelegate aPingDelegate = (PingDelegate)ar.AsyncState;
                aPingDelegate.EndInvoke(ar);
            }
            catch
            { }
        }

        private StringBuilder MakeUpExceptionString(Exception e)
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

        private string FixUpThePaths(string path)
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

        private void SaveSettings()
        {
            try
            {
                // if already doing then wait
                while (_SavingSettings)
                {
                    if (_Exit || _Escape) return;
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                // set flag
                _SavingSettings = true;
                if (_CurrentSequenceList == null)
                {
                    return;
                }

                // make local copy of data
                List<TestSequence> locSequenceList = new List<TestSequence>(_CurrentSequenceList);
                TesterState locTesterStateStruct = new TesterState(_TesterState);

                FileStream fs = null;
                try
                {
                    fs = new FileStream(
                       System.IO.Path.Combine(SpecialFileClass.SpecialFileFolder, Constants.TesterObjectDAT),
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None);
                    BinaryFormatter bf = new BinaryFormatter();

                    // write out 
                    bf.Serialize(fs, locSequenceList);
                    bf.Serialize(fs, locTesterStateStruct);
                    // TODO : bf.Serialize(fs, TclPath);
                    bf.Serialize(fs, BladePath);
                    bf.Serialize(fs, FactPath);
                    bf.Serialize(fs, GradePath);
                    bf.Serialize(fs, FirmwarePath);
                    bf.Serialize(fs, ResultPath);
                    bf.Serialize(fs, LogPath);
                    bf.Serialize(fs, DebugPath);

                    bf.Serialize(fs, CountsPath);
                    bf.Serialize(fs, OpenDelay);
                    bf.Serialize(fs, CloseDelay);
                    // TODO : bf.Serialize(fs, TclStart);

                    fs.Flush();
                    fs.Close();
                }
                catch
                {
                    // Disregard
                }
                finally
                {
                    try { fs.Dispose(); }
                    catch { }
                }
                try
                {
                    MethodInvoker del = delegate
                    {
                        WriteOutCountsData();
                        _CountStateFromDisk.writeOutData(System.IO.Path.Combine(BladePath, Constants.TesterCountsTXT));
                    };
                    del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { try { del.EndInvoke(ar); } catch { } }), del);
                }
                catch { }
            }
            finally
            {
                _SavingSettings = false;
            }
        }

        private void WriteOutCountsData()
        {
            lock (_ReadInCountsLockObject)
            {
                _CountStateFromDisk.writeOutData(System.IO.Path.Combine(CountsPath, Constants.TesterCountsTXT));
            }
        }

        /// <summary>
        /// Transfer file from fromFile stream to toFile stream. 
        /// Byte limit : 8192
        /// </summary>
        /// <param name="fromFile"></param>
        /// <param name="toFile"></param>
        private void TxFile(Stream fromFile, Stream toFile)
        {
            byte[] byteArray = new byte[8192];
            int howMany = 1;
            while (howMany > 0)
            {
                howMany = fromFile.Read(byteArray, 0, 8192);
                toFile.Write(byteArray, 0, howMany);
            }
        }

        /// <summary>
        /// Starts and runs the requested sequence given in TestName.
        /// BreakOnError flag is no longer used for anything.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="TestName"></param>
        /// <returns></returns>
        private void StartTestSequence(string ParseString, string TestName, string GradeName, int StartingTest, bool BreakOnError, string tableStr)
        {
            if ((_TesterState.SeqGoing) || (_TesterState.PleaseStop))
            {
                WriteLine("TesterObject::StartTestSequence -- tester busy.");
                WriteLineContent("TesterObject::StartTestSequence busy, test name:" + TestName);
                StatusEventArgs args = new StatusEventArgs("SequenceAbortingEvent -- tester busy ", 1);
                SendSequenceAbortingEvent(this, args);
                Application.DoEvents();
                Thread.Sleep(1000);
                StatusEventArgs finishedWithThis = new StatusEventArgs("Aborted");
                SendSequenceCompletedEvent(this, finishedWithThis);
                return;
            }

            // TODO :  TCL need remove  waitForEveryTclThingNotBusy();
            if (_Escape) return;

            _TesterState.SeqGoing = true;
            _TesterState.CmdBusy = true;

            string testName = TestName.Replace("{", "").Replace("}", "");
            WriteLine("StartTest " + testName);
            WriteLineContent(" " + testName);
            string[] bladeTypeArray = BladeType.Trim().Split(new char[] { ' ' }, StringSplitOptions.None);
            bool arrayOk = bladeTypeArray.Length > 1;

            // Append blade info to the parse string.
            StringBuilder parseStringBuilder = new StringBuilder();
            parseStringBuilder.Append(ParseString + ";");
            if (MyLocation.Length > 5)
            {
                parseStringBuilder.Append("set FACTTest(bladeSlot) {" + MyLocation.Substring(5) + "}; ");
            }
            else
            {
                parseStringBuilder.Append("set FACTTest(bladeSlot) { 1 }; ");
            }
            parseStringBuilder.Append("set FACTDrive(bladeSerialNumber) {" + BladeSN + "}; ");
            parseStringBuilder.Append("set FACTDrive(jadeSerialNumber) {" + JadeSN + "}; ");
            parseStringBuilder.Append("set FACTDrive(bladeType) {" + (arrayOk ? bladeTypeArray[0] : BladeType) + "}; ");
            parseStringBuilder.Append("set FACTDrive(MEMSSerialNumber) {" + MemsSn + "}; ");
            parseStringBuilder.Append("set FACTDrive(diskSerialNumber) {" + DiskSn + "}; ");
            parseStringBuilder.Append("set FACTDrive(PCBASerialNumber) {" + PcbaSn + "}; ");
            parseStringBuilder.Append("set FACTDrive(motorBaseplateSerialNumber) {" + MotorBaseSn + "}; ");
            parseStringBuilder.Append("set FACTDrive(actuatorSerialNumber) {" + ActuatorSN + "}; ");
            parseStringBuilder.Append("set FACTDrive(flexSerialNumber) {" + FlexSN + "}; ");

            SequenceExecutionObject runThis = new SequenceExecutionObject();

            // find requested sequence in our list of sequences
            for (int i = 0; i < _CurrentSequenceList.Count; i++)
            {
                try
                {
                    // Is this the one we are looking for?
                    if (testName.Trim().ToLower() == _CurrentSequenceList[i].dictionaryHeader["Name"].Trim().ToLower())
                    {
                        // we found it
                        runThis.TheSequence = _CurrentSequenceList[i];
                        runThis.GradeName = GradeName.Replace("{", "").Replace("}", "");
                        runThis.StartingTest = StartingTest;
                        runThis.BreakOnError = BreakOnError;
                        //runThis.ParseString = parseStringBuilder.ToString().Replace("{", "").Replace("}", "");
                        runThis.TableString = tableStr.Replace("{", "").Replace("}", "");
                        _TesterState.SequenceName = runThis.TheSequence.dictionaryHeader["Name"];
                        break;
                    }
                }
                catch
                {
                    StatusEventArgs args = new StatusEventArgs("SequenceAbortingEvent -- Sequence defective or not found. ", 1);
                    SendSequenceAbortingEvent(this, args);
                    Application.DoEvents();
                    Thread.Sleep(1000);
                    StatusEventArgs finishedWithThis = new StatusEventArgs("Aborted");
                    SendSequenceCompletedEvent(this, finishedWithThis);
                    _TesterState.SeqGoing = false;
                    _TesterState.CmdBusy = false;
                    return;
                }
            }
            //if requested test sequence not found or missing something then return
            if (runThis.TheSequence == null || _TesterState.SequenceName.Length == 0 || runThis.TheSequence.ArrayListTests.Count == 0)
            {
                WriteLine("StartTest exited  Either Sequence Not Found or defective.");
                StatusEventArgs args = new StatusEventArgs("SequenceAbortingEvent -- Sequence Not Found. " + TestName, 1);
                SendSequenceAbortingEvent(this, args);
                Application.DoEvents();
                Thread.Sleep(1000);
                WriteLine("Calling sequence complete.");
                StatusEventArgs finishedWithThis = new StatusEventArgs("Aborted");
                SendSequenceCompletedEvent(this, finishedWithThis);
                _TesterState.SeqGoing = false;
                _TesterState.CmdBusy = false;
                return;
            }

            StringBuilder sb = new StringBuilder();

            if (runThis.GradeName.Length > 0)
            {
                _TesterState.GradeName = runThis.GradeName;
                sb.Append(Constants.SetupGradeFile + " " + System.IO.Path.Combine(GradePath, Path.GetFileName(_TesterState.GradeName)).Replace("\\", "/") + "; ");
            }
            else
            {
                _TesterState.GradeName = "";
            }

            if (runThis.TableString.Length > 0)
            {
                sb.Append(Constants.SetupGradeTable + " " + runThis.TableString + "; ");
            }
            else
            {
                // nothing
            }

            // set current values for grade and seq files.
            _TesterState.GradeName = GradeName;
            _TesterState.SequenceName = TestName;

            // Add the above to parsestring 
            //runThis.ParseString = sb.ToString() + parseStringBuilder.ToString().Replace("{", "").Replace("}", "");
            runThis.ParseString = sb.ToString() + parseStringBuilder.ToString();

            //Go
            // TODO : RunTheSequence(runThis);
            return;
        }

        private void SetModuleStateThreadFunc(object testerStateObj)
        {
            _TesterState = (TesterState)testerStateObj;

            _FormerStatusRead = -1;
            ReadBunnyStatusAndUpdateFlags();
            WriteLine("TesterObject::SetModuleStateThreadFunc called ");
            WriteLineContent(_TesterState.ToString());
        }

        private void GetBunnyStatusThreadFunc()
        {
            _FormerStatusRead = -1;
            ReadBunnyStatusAndUpdateFlags();
            WriteLine("TesterObject::GetBunnyStatusThreadFunc called ");
            WriteLineContent("TesterObject::GetBunnyStatusThreadFunc called " + _TesterState.ToString());
        }

        #endregion internal sup Methods

        #region Log System
        internal void WriteLine(string Text)
        {
            if (Text == null) return;
            Thread WriteLineThread = new Thread(new ParameterizedThreadStart(WriteLineThreadFunc))
            {
                IsBackground = true
            };
            WriteLineThread.Start((object)Text);
        }

        private void WriteLineThreadFunc(object passingObject)
        {
            // TODO : Achieve write line function
        }

        internal void WriteLineContent(string Text)
        {
            if (Text == null) return;
            Thread WriteLineContentThread = new Thread(new ParameterizedThreadStart(WriteLineContentThreadFunc))
            {
                IsBackground = true
            };
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
