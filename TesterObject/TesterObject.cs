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
using System.Diagnostics;

using WD.Tester.Enums;
using WD.Tester.Sequence;
using WD.Tester.IJadeCommonTables;
using HGST.Blades;
using RemoveDriveByLetter;
using NLog;

namespace WD.Tester.Module
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
        private readonly Logger StdOutLog = LogManager.GetLogger("StdOutLog");
        private volatile string lastStdOut;
        private volatile string lastLastStdOut;
        private volatile string lastStdOut2;
        private volatile string lastStdErr;

        private bool constructorFinished;

        // TODO : TclProcess TclStreamWriter init in Tclstart function
        private Process TclProcess;
        private StreamWriter TclStreamWriter;

        private bool _Disposed;
        private List<TestSequence> _CurrentSequenceList;
        private ProxyListClass _CallbackProxyList;
        private delegate void PingDelegate(string name);
        private delegate void StartTestSequenceDelegate(string ParseString, string TestName, string GradeName, int StartingTest, bool BreakOnError, string tableStr);
        private delegate void TclCommandDelegate(string Command, bool bToTv);
        private delegate void CardPowerDelegate(bool State);

        /// <summary>
        /// Use for monitor all status
        /// </summary>
        internal volatile TesterState _TesterState;
        private List<SequenceExecutionObject> pausedTestList;

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

        private bool tclIsNowGoing;  // set to false by constructor.
        private bool tclshGoing; // set to false in constructor.
        private int tclSleepTime;

        private bool bTimeoutEnabled;
        #endregion Fields

        #region Constructors
        public TesterObject()
        {
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
            Init();
        }

        private void Init()
        {
            constructorFinished = false;

            lastStdOut = "";
            lastLastStdOut = "";
            lastStdOut2 = "";
            lastStdErr = "";

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

            tclIsNowGoing = false;  // Set true at start of tcl cmd; cleared by last stdout.
            tclshGoing = false;     // Set true at start of tcl cmd; cleared when we parse the result.
            bTimeoutEnabled = false;
            tclSleepTime = 5;
            pausedTestList = new List<SequenceExecutionObject>();

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

            // Start TCL  TODO : TCL 
            // TODO : TCL  StartTclsh();
            try
            {
                GetMemsType();
            }
            catch (Exception e)
            {
                WriteLine("Exception: " + e.Message);
                if (e.InnerException != null)
                {
                    WriteLineContent(e.Message + Environment.NewLine + e.InnerException.Message);
                }
            }

            // TODO : CallBack should be add
            timerDelegate = new TimerCallback(readTheStatusCallback);
            readStatusTimer = new System.Threading.Timer(timerDelegate, null, 5000, readStatusTime);
            // TODO : CallBack should be add
            sequenceTimeoutDelegate = new TimerCallback(sequenceTimeoutCallback);
            sequenceTimeoutTimer = new System.Threading.Timer(sequenceTimeoutDelegate, null, 60000, 60000);

            watchdogDelegate = new TimerCallback(MemsOpenWatchdogTimerCallback);
            memsOpenWatchdogTimer = new System.Threading.Timer(watchdogDelegate, null, Timeout.Infinite, Timeout.Infinite);

            constructorFinished = true;
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

        public string PingAllEvent(string message)
        {
            // TODO : // if (_TesterState.PauseEvents) PauseEvents(false);
            WriteLine(string.Format("TesterObject::Ping"));
            WriteLineContent("TesterObject::Ping" + message);
            PingDelegate aPingDelegate = new PingDelegate(PingOperation);
            aPingDelegate.BeginInvoke(message, PingCallback, aPingDelegate);
            return message;
        }

        /// <summary>
        /// Service to maintain a connection
        /// </summary>
        /// <returns>Keep Alive Timeout</returns>
        public int KeepAliveChannel()
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

        /// <summary>
        /// Runs the initial TCL sctipt.
        /// Sources all of FACT code.
        /// Copies FACT config file to Config dir.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public void InitializeTCL(String Key)
        {
            WriteLine("InitTCL " + TclStart);
            // source TCL files
            SendToTclAndGetResult(TclStart, true);
        }

        public string GetFileCRC(string FileName)
        {
            string tmpString = "0";
            FileStream fs = null;
            try
            {
                FileName = FixUpThePaths(FileName);
                WriteLine("GetFileCRC called ");
                WriteLineContent(FileName);
                WaitForUsbDriveToWakeUp(FileName, 2000);

                fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                tmpString = WhatIsTheCRC(fs);
                fs.Close();
            }
            catch
            {
                tmpString = "0";
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
            return tmpString;
        }

        /// <summary>
        /// Public property.  Jade uses this to send LogInValue (permissions) to BladeRunner.
        /// </summary>
        public void StaticTalkLogInValue(Int32 value)
        {
            // Send to static property (goes to out server TesterModule).
            try
            {
                StaticServerTalker.LogInValueProp = (ReturnValues)value;
            }
            catch { }
        }
        #endregion ITesterObject base function

        /// <summary>
        /// Send text to LCD
        /// </summary>
        /// <param name="text"></param>
        public void SetLcdText(string text)
        {
            SendLineToLCD(text);
        }

        /// <summary>
        /// Sends Text to LCD one line at a time.
        /// Scrolls first line to second line each call.
        /// </summary>
        /// <param name="Text"></param>
        private void SendLineToLCD(string Text)
        {
            _BunnyCard.LcdScrollText(Text);
        }

        /// <summary>
        /// This closes and stops everything.  Saves settings.
        /// </summary>
        public void OnClose()
        {
            // send ProgramClosingEvent - Tell Jade.exe that we are going away.
            StatusEventArgs args = new StatusEventArgs("ProgramClosingEvent TesterModule", 0);
            SendProgramClosingEvent(this, args);
            WriteLine("ProgramClosingEvent - fired");
            WriteLineContent(args.EventType + " " + args.Text);

            DateTime closeTime = DateTime.Now;
            TimeSpan maxCloseDelay = TimeSpan.FromSeconds(15.0);

            //Close the Mems blocking on a thread.
            // We join later.
            MethodInvoker del = delegate
            {
                OpenCloseMems(0);
            };
            IAsyncResult memsIAsyncResult = del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { try { del.EndInvoke(ar); } catch { } }), del);

            int waitFor = 50;
            // Wait until all events are sent
            for (int t = 0; t < 1000; t += waitFor)
            {
                if (_BladeEventQueue.Count == 0) break;
                Application.DoEvents();
                Thread.Sleep(waitFor);
            }

            // tell TCL to go away
            if (TclProcess != null)
            {
                try
                {
                    if (!TclProcess.HasExited)
                    {
                        SendToTclAndGetResult("exit", true);
                        Thread.Sleep(500);
                    }
                }
                catch { }
                try
                {
                    if (!TclProcess.HasExited)
                    {
                        TclProcess.Kill();
                    }
                }
                catch { }
                try
                {
                    TclProcess.Close();
                }
                catch { }
            }
            // save current settings
            SaveSettings();

            // join mems thread
            while (!memsIAsyncResult.IsCompleted || closeTime + maxCloseDelay > DateTime.Now)
            {
                Thread.Sleep(100);
                // Is it closed yet?
                if (GetMemsStatus() == WD.Tester.Enums.MemsStateValues.Closed) break;
                //_memsState
            }

            // tell our helper threads to go away (we do not want any more help).
            _Exit = true;
            _Escape = true;
        } // end onClose

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
        /// Returns a directory of the sequence files in TesterObject's memory.
        /// Returns an ArrayList of FileNameStructs.
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public FileNameStruct[] DirConfig(string FileName)
        {
            WriteLine("DirConfig called ");
            WriteLineContent(FileName);
            //throw new Exception("it broke");
            ArrayList seqList = new ArrayList();

            if (_CurrentSequenceList.Count == 0)
            {
                return (FileNameStruct[])seqList.ToArray(typeof(FileNameStruct));
            }

            for (int i = 0; i < _CurrentSequenceList.Count; i++)
            {
                FileNameStruct oneFile = new FileNameStruct
                {
                    FileNameStr = _CurrentSequenceList[i].dictionaryHeader["Name"],
                    VersionStr = _CurrentSequenceList[i].dictionaryHeader["Version"],
                    DateStr = _CurrentSequenceList[i].dictionaryHeader["date"]
                };

                if (FileName.Length == 0 || FileName == oneFile.FileNameStr)
                {
                    seqList.Add(oneFile);
                    if (FileName.Length > 0)
                    {
                        break;
                    }
                }
            }
            return (FileNameStruct[])seqList.ToArray(typeof(FileNameStruct));
        }

        /// <summary>
        /// Starts and runs the requested sequence given in TestName.
        /// Should be called StartSequence.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="TestName"></param>
        /// <returns></returns>
        public void StartTest(string ParseString, string TestName, string GradeName, string tableStr)
        {
            // TODO : StartTest
            _TesterState.GradeName = GradeName;
            // tell TCL to start testing
            StartTestSequenceDelegate startTestDelegate = new StartTestSequenceDelegate(StartTestSequence);
            startTestDelegate.BeginInvoke(ParseString, TestName, GradeName, 0, false, tableStr, new AsyncCallback(delegate (IAsyncResult ar) { startTestDelegate.EndInvoke(ar); }), startTestDelegate);
        }

        /// <summary>
        /// Jade exe can tell blade to pause testing.
        /// Jade exe can tell blade to restart testing.
        /// </summary>
        /// <param name="bwait"></param>
        public void PauseTests(bool bwait)
        {
            WriteLine("PauseTests called ");
            WriteLineContent(bwait.ToString());
            _TesterState.PauseTests = bwait;
            if (bwait)
            {
                return;
            }

            // If no longer paused
            // see if there are any pending tests to do.
            while (pausedTestList.Count > 0)
            {
                _TesterState.NowTestsArePaused = false;
                StartTestSequence("key",
                   pausedTestList[0].TheSequence.FileName,
                   pausedTestList[0].GradeName,
                   pausedTestList[0].StartingTest,
                   pausedTestList[0].BreakOnError,
                   pausedTestList[0].TableString);
                pausedTestList.RemoveAt(0);
                Application.DoEvents();
            }
        } // end pauseTests

        #endregion Tester function

        #region TCL methods
        /// <summary>
        /// Jade exe can tell the blade to stop sending events for a while.
        ///   When true, Blade runner stops sending events and instead queues 
        ///   them up.
        ///   When false blade runner will start emptying the event queue.
        /// </summary>
        /// <param name="bwait"></param>
        // 2009/8/19 Akishi Murata: This function is based on multi remoting channel, obsolate and will be modified.
        public void PauseEvents(bool bwait)
        {
            WriteLine("PauseEvents called ");
            WriteLineContent("PauseEvents called " + bwait.ToString());
            // TODO : consider remove it
            _TesterState.PauseEvents = bwait;
            //if (bwait == false)
            //{
            //    Thread pendingEventsThread = new Thread(new ThreadStart(doPendingEvents));
            //    pendingEventsThread.IsBackground = true;
            //    pendingEventsThread.Start();
            //}
            _TesterState.PauseEvents = false; // Now does not work.
        }

        /// <summary>
        /// Send abort to tclDosBox
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Force"></param>
        /// <returns></returns>
        public bool AbortTCL(string Reason, bool Force)
        {
            bool bInitial = _TesterState.SeqGoing;

            WriteLine("Abort called");
            WriteLineContent(Reason + " " + Force.ToString());

            // see if we are already aborting or not going
            if ((_TesterState.PleaseStop && !Force) || (!_TesterState.SeqGoing && !Force))
            {
                _Escape = false;
                // say we did it.
                return true;
            }
            if (!_TesterState.PleaseStop)
            {
                // tell the remote(s) that this sequence is aborting
                StatusEventArgs abortingThis = new StatusEventArgs((_TesterState.SeqGoing ? _TesterState.SequenceName : "") + "::" + Reason, Force ? 1 : 0);
                SendSequenceAbortingEvent(this, abortingThis);
                WriteLine("Sequence Aborting Event fired.");
                WriteLineContent(abortingThis.Text);
            }
            // Stop it!
            if (_TesterState.SeqGoing)
            {
                _TesterState.PleaseStop = true;  // tells the sequence to stop going.
            }

            _Escape = true;

            // if force we abort immediately 
            if (Force)
            {

                // Make sure that all timing loops exit (they check bEscape which is now true).
                Thread.Sleep(tclSleepTime);
                Application.DoEvents();
                Thread.Sleep(tclSleepTime);
                Application.DoEvents();
                Thread.Sleep(tclSleepTime);
                Application.DoEvents();

                try { TclProcess.Kill(); }
                catch { /* Force kill, ignore any exceptions */ }

                // send message to caller
                string AbortStr = Environment.NewLine + Environment.NewLine +
                   " ------ " + "TclRestart" + " ------" +
                   Environment.NewLine + Environment.NewLine;
                StatusEventArgs args = new StatusEventArgs(AbortStr, (int)Enums.eventInts.cmdWinDone);

                //WriteLine("Abort message to queue ");
                SendStatusEvent(this, args);
                _TesterState.CmdBusy = false;
                _TesterState.NowTestsArePaused = false;
                _TesterState.PauseEvents = false;
                _TesterState.PauseTests = false;
                _TesterState.PleaseStop = false;
                _TesterState.SeqGoing = false;
                _TesterState.TestNumber = 0;
                tclIsNowGoing = false;
                tclshGoing = false;
                // restart tcl
                StartTclsh();
            }

            Application.DoEvents();
            Thread.Sleep(500);

            // tell the remote that this sequence is aborted.
            StatusEventArgs finishedWithThis = new StatusEventArgs("aborted");
            SendSequenceCompletedEvent(this, finishedWithThis);
            WriteLine("Sequence completed Event fired ");
            WriteLineContent(finishedWithThis.Text);

            // Wait a bit for the sequence completed to do it's thing.
            Application.DoEvents();
            Thread.Sleep(250);
            // say we did it
            _Escape = false;
            PauseEvents(false);

            // New requirement from HICAP
            InitializeTCL("");

            return true;
        }

        public void TclInput(string inputString, bool bToTv)
        {
            TclCommandDelegate tclInputDelegate = new TclCommandDelegate(TclInputThreadFunc);
            // send string async
            tclInputDelegate.BeginInvoke(inputString, bToTv, new AsyncCallback(delegate (IAsyncResult ar) { tclInputDelegate.EndInvoke(ar); }), tclInputDelegate);
        }

        /// <summary>
        /// Public function specified in ITesterObject.
        /// This is used by Jade to execute TCL instructions.
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="bToTv"></param>
        public void TclCommand(string Command, bool bToTv)
        {
            TclCommandDelegate tclCommandDelegate = new TclCommandDelegate(TclCommandThreadFunc);
            // send string async
            tclCommandDelegate.BeginInvoke(Command, bToTv, new AsyncCallback(delegate (IAsyncResult ar) { tclCommandDelegate.EndInvoke(ar); }), tclCommandDelegate);
        }

        /// <summary>
        /// Send string to tclsh and get return value.
        /// </summary>
        /// <param name="cmdStr"></param>
        /// <returns></returns>
        private string SendToTclAndGetResult(string cmdStr, bool bToTv)
        {
            try
            {
                // In case previous command still going, wait for it to finish.
                WaitForCommandToFinish();
                // If aborting 
                if (_Escape) return "";

                tclshGoing = true;
                // Flag to tell us that tclsh is busy.
                // This is cleared when STDOUT receives onstants.weBeDoneString.
                tclIsNowGoing = true;

                StdOutLog.Info("sendTcl start: {0} ", cmdStr);

                lastStdErr = "";
                lastStdOut = "";
                lastLastStdOut = "";


                // send to tcl (we get the result below).
                SendTclCmdString(cmdStr, bToTv);

                // Wait for this to return from tclsh sending function.
                // The command is not finished until Constants.weBeDoneString
                // Shows up in stdout handler wwhich clears tclIsNowGoing.
                WaitForTclToFinish();

                // If aborting 
                if (_Escape) return "";

                // Get the return value from TCL.
                string retVal;
                // Sometimes TCL gives the answer on previous stdout and
                //   sometines on the last stdout.
                if (lastStdOut.Trim().Length == 0)
                {
                    retVal = lastLastStdOut;  // Previous stdout
                }
                else
                {
                    retVal = lastStdOut;  // the last stdout
                }
                StdOutLog.Info("sendTcl RetVal: {0} ", retVal);

                return retVal;
            }
            finally
            {
                // Now we are really done.
                tclshGoing = false;
            }
        } // end sendToTclAndGetResult

        /// <summary>
        /// Wait for RunTest to finish.
        /// This exits when tcl is finsihed and we have processed the result.
        /// </summary>
        private void WaitForCommandToFinish()
        {
            while (!_Exit && !_Escape)
            {
                if (!tclshGoing)
                {
                    break;
                }
                Thread.Sleep(tclSleepTime);
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Wait for nothing going.
        /// </summary>
        private void WaitForEveryTclThingNotBusy()
        {
            while (!_Exit && !_Escape)
            {
                if (!tclshGoing && !_TesterState.SeqGoing && !_TesterState.CmdBusy)
                {
                    break;
                }
                Thread.Sleep(tclSleepTime);
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Wait for TCL to finish.
        /// This is just TCL.  We have not processed the TCL result yet.
        /// </summary>
        private void WaitForTclToFinish()
        {
            int count = 0;
            while (!_Exit && !_Escape)
            {
                // Break when TCL instruction finishes.
                if (!tclIsNowGoing)
                {
                    break;
                }
                // If TesterModule's constructor gets an exception, it stops TCL.
                // This prevents a hang on this condition.
                if (!constructorFinished)
                {
                    Thread.Sleep(100);
                    if (count > 1) break;
                }
                Thread.Sleep(tclSleepTime);
                Application.DoEvents();
                count++;
            }
        }

        /// <summary>
        /// Sends a command string to TCL.  TCL will execute string.
        ///   TCL returns result via stdout events.
        ///    Actually sends:  puts "[command] Constants.weBeDoneString";
        ///    Sends puts "Constants.weBeDoneString"  This tells Jade.Exe that it has the answer.
        /// </summary>
        /// <param name="Command"></param>
        private void SendTclCmdString(string Command, bool bToTv)
        {
            try
            {
                lastStdOut2 = "";

                if (TclProcess.HasExited)
                {
                    if (Command == "exit")
                    {
                        return;
                    }
                    StartTclsh();
                }

                //bTclError = false;   // if something goes wr_ong std err event previously set this.

                if (bToTv)
                {
                    StatusEventArgs args = new StatusEventArgs("% " + Command, (int)Enums.eventInts.toTv);
                    SendStatusEvent(this, args);
                }

                if (!IsCmdStringPossiblyOK(Command)) return;
                lastLastStdOut = lastStdOut = lastStdErr = "";
                TclStreamWriter.WriteLine("puts \"[" + Command.Trim() + "]" + Constants.weBeDoneString + "\";");
            }
            catch (Exception e)
            {
                TclStreamWriter.WriteLine("puts stderr \"" + MakeUpExceptionString(e) + Constants.weBeDoneString + "\"");
            }
        }

        private void StartTclsh()
        {
            // TODO : !!!!Tcl Start
        }

        /// <summary>
        /// Checks for "\\" at end of string and for matching TCL quotes.
        /// If OK returns True.
        /// If something is wrong returns false and sends error message to tcl window. 
        /// This is in case someone types something wrong on the command line.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private bool IsCmdStringPossiblyOK(string cmd)
        {
            int singleQuote = 0;
            int doubleQuote = 0;
            int curlyQuote = 0;
            char[] charArray = cmd.Trim().ToCharArray();
            for (int i = 0; i < charArray.Length; i++)
            {
                char aChar = charArray[i];
                // check the char for the various quotes
                if (aChar == '\'') singleQuote++;
                if (aChar == '\"') doubleQuote++;
                if (aChar == '{') curlyQuote++;
                if (aChar == '}') curlyQuote--;

                // if curlyQuote ever goes negative
                if (curlyQuote < 0)
                {
                    ErrorSender("Error: Ending curly quote \"}\" before a starting curly quote \"{.\"");
                    tclIsNowGoing = false;
                    return false;
                }

                // check for ending backslash
                if (aChar == '\\' && i >= charArray.Length - 1)
                {
                    ErrorSender("Error: Line ends with backslash.");
                    tclIsNowGoing = false;
                    return false;
                }
            } // end for all chars

            if (singleQuote % 2 != 0)
            {
                ErrorSender("Error: Mismatched single quotes.");
                tclIsNowGoing = false;
                return false;
            }
            if (doubleQuote % 2 != 0)
            {
                ErrorSender("Error: Mismatched double quotes.");
                tclIsNowGoing = false;
                return false;
            }

            if (curlyQuote != 0)
            {
                ErrorSender("Error: Mismatched curly quotes.");
                tclIsNowGoing = false;
                return false;
            }

            return true;
        }
        #endregion TCL methods

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
                    bf.Serialize(fs, TclPath);
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
                    bf.Serialize(fs, TclStart);

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

            WaitForEveryTclThingNotBusy();
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

        private void ErrorSender(string errorStr)
        {
            //bWasError = true;
            //bTclError = true;

            // send event to client's DOS and Run windows
            StatusEventArgs args = new StatusEventArgs(errorStr + Environment.NewLine, (int)Enums.eventInts.error);
            StdOutLog.Info("Done ERROR ||{0}|| ", errorStr);
            WriteLine("TCL Err:" + args.EventType + " " + args.Text);
            WriteLineContent("TCL Err:" + args.EventType + " " + args.Text);
            SendStatusEvent(this, args);

        }

        public void TclInputThreadFunc(string inputString, bool bToTv)
        {
            if (inputString.EndsWith(Environment.NewLine))
            {
                inputString = inputString.Substring(0, inputString.Length - 2);
            }

            WriteLine("TclIn " + inputString);
            WriteLineContent(inputString);
            if (bToTv)
            {
                StatusEventArgs args = new StatusEventArgs(inputString, (int)Enums.eventInts.toTv);
                SendStatusEvent(this, args);
            }

            if (tclshGoing || _TesterState.SeqGoing || _TesterState.CmdBusy || tclIsNowGoing)
            {
                TclStreamWriter.WriteLine(inputString);
            }
        }

        /// <summary>
        /// Runs on a thread pool thread; this sends command line commands to TCL.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Command"></param>
        private void TclCommandThreadFunc(string Command, bool bToTv)
        {
            try
            {
                //FactCmd.Info("TesterObject TclCommand thread: {0} ", Command);
                WriteLine(Command);
                WriteLineContent("TclCmd: " + Command + " " + bToTv.ToString());

                // if nothing there 
                if (Command.Length == 0)
                {
                    return;
                }
                // if exit requested
                if (Command == "exit")
                {
                    //Abort("Exiting", true);
                    Application.Exit();
                    return;
                }

                WaitForEveryTclThingNotBusy();
                if (_Escape) return;

                _TesterState.CmdBusy = true;

                StdOutLog.Info("TclCmd Start: {0} ", Command);

                string resultStr = "{" + SendToTclAndGetResult(Command, bToTv) + "} {" + lastStdErr + "}";
                StdOutLog.Info("TclCmd Result: {0} ", resultStr);

                StatusEventArgs e = new StatusEventArgs(resultStr, (int)eventInts.tclResult);
                SendStatusEvent(this, e);
            }
            finally
            {
                _TesterState.CmdBusy = false;
            }
        }

        /// <summary>
        /// Takes filename of file that we know exists and waits for it to show up.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="milliSecs"></param>
        private void WaitForUsbDriveToWakeUp(string fileName, int milliSecs)
        {
            int count = 0;
            int delay = 100;
            int maxCount = milliSecs / delay;
            if (maxCount < 1) maxCount = 1;

            while (!Directory.Exists(Path.GetPathRoot(fileName)))
            {
                if (_Exit) return;
                // sleep while USB wakes up
                Thread.Sleep(delay);
                count++;
                if (count > maxCount)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Takes a stream, adds up the CRC, and returns a hex number in string format.
        /// Does not close the stream when finished.
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        private string WhatIsTheCRC(Stream fs)
        {
            StreamCrcClass streamCrcClass = new StreamCrcClass();
            string whatCRC = streamCrcClass.GetCrcString(fs);
            return whatCRC;
        }

        private void readTheStatusCallback(object state)
        {
            try
            {
                readStatusTimer.Change(Timeout.Infinite, Timeout.Infinite);
                if (_BunnyCard != null && _TesterState.BunnyGood)
                {
                    bool bunnyStat = false;
                    bool isOK = true;
                    ReadBunnyStatusAndUpdateFlags(out bunnyStat);
                    _TesterState.BunnyGood = bunnyStat;
                    if (!bunnyStat)
                    {
                        isOK = false;
                        NotifyWorldBunnyStatus(false, "read status invalid");
                    }

                    if (FirmwareRev.Length == 0)
                    {
                        if (isOK)
                        {
                            _BunnyCard.LcdScrollText("Bunny On-Line   ");
                            StaticServerTalker.SendLineToLCD("Bunny On-Line   ");

                        }
                        NotifyWorldBunnyStatus(bunnyStat, "read status callback ");
                        if (isOK)
                        {
                            GetFwRev();
                            GetDriverVer();
                        }
                    }
                }
                else
                {
                    bool status = UsbReset(0);
                    if (status)
                    {
                        ReadBunnyStatusAndUpdateFlags();
                    }
                    else
                    {
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
                        if (_Exit) return;
                        Application.DoEvents();
                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                if (_TesterState.BunnyGood)
                {
                    if (_VoltageCheckBlade)
                    {
                        // if voltage check blade then do it as quickly as possible.
                        readStatusTimer.Change(1, Timeout.Infinite);
                    }
                    else
                    {
                        // if not voltage check blade then do regular rate.
                        readStatusTimer.Change(readStatusTime, Timeout.Infinite);
                    }
                }
                else // broke
                {
                    readStatusTimer.Change(readStatusTime * 5, Timeout.Infinite);
                }
            }
        }

        private void sequenceTimeoutCallback(object state)
        {
            if (!bTimeoutEnabled) return;

            _Escape = true;
            AbortTCL(Constants.SeqTimeout, false);
        }

        private void CardPower(bool State)
        {
            CardPowerDelegate cardPowerDelegate = new CardPowerDelegate(CardPowerTreadFunc);
            cardPowerDelegate.BeginInvoke(State, new AsyncCallback(delegate (IAsyncResult ar) { cardPowerDelegate.EndInvoke(ar); }), cardPowerDelegate);
        }

        /// <summary>
        /// Turns PCB power on or off via bunny card.
        /// </summary>
        /// <param name="State"></param>
        private void CardPowerTreadFunc(bool State)
        {
            try
            {
                SetAux12VDC(State);
                SetAux5VDC(State);
            }
            catch (Exception e)
            {
                WriteLine("Exception in CardPower: " + e.Message);
            }
        }

        /// <summary>
        /// Turn 12V on or off
        /// </summary>
        /// <param name="state"></param>
        private void SetAux12VDC(bool value)
        {
            SendBunnyEvent(this, new StatusEventArgs(value ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Pwr12V));
            if (_BunnyCard != null)
            {
                _BunnyCard.Set12vdc(value);
                ReadBunnyStatusAndUpdateFlags();
            }
        }

        /// <summary>
        /// Turn 5V on or off
        /// </summary>
        /// <param name="state"></param>
        private void SetAux5VDC(bool value)
        {
            SendBunnyEvent(this, new StatusEventArgs(value ? ((int)OnOffValues.On).ToString() : ((int)OnOffValues.Off).ToString(), (int)BunnyEvents.Pwr5V));
            if (_BunnyCard != null)
            {
                _BunnyCard.Set5vdc(value);
                ReadBunnyStatusAndUpdateFlags();
            }
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
            try
            {
                string text = (string)passingObject;
                if (text.Trim().StartsWith(Constants.weBeDoneString)) return;
                if (text.Contains("StatusEvent")) return;

                // TODO :   To LCD
                // SendLineToLCD(text);

                // To blade runner service and client
                StatusEventArgs e = new StatusEventArgs(text, (int)eventInts.Notify);
                SendStatusEvent(this, e);

                // To Host
                StaticServerTalker.MessageString = text;
            }
            catch
            { }
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
            try
            {
                string text = (string)passingObject;
                if (text.Trim().StartsWith(Constants.weBeDoneString)) return;

                // To balde runner service and client
                StatusEventArgs e = new StatusEventArgs(text, (int)eventInts.NotifyWithContent);
                SendStatusEvent(this, e);

                // To host
                StaticServerTalker.MessageStringContent = text;
            }
            catch
            { }
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
