/************************************************************
 * This is the stream service for WCF.
 * BladeRunner and Jade use this to transfer files.
 * 
 * 
 * Robert L. Kimball Aug, 2012
 *
 * ***************************************************************/
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Threading;
using System.ComponentModel.Composition;

using WD.Tester.Enums;

namespace WD.Tester.Module
{
    [Export(typeof(ITesterObjectStreaming)), 
        PartCreationPolicy(CreationPolicy.Any), 
        ExportMetadata("Name", "FactBladeRunner")]
    [ServiceBehavior(UseSynchronizationContext = false, 
        ConcurrencyMode = ConcurrencyMode.Reentrant, 
        InstanceContextMode = InstanceContextMode.Single)]
    public class TesterObjectStreaming : ITesterObjectStreaming
    {
        #region Fields
        /// <summary>
        /// Event from ITesterObjectStreaming
        /// </summary>
        public event ObjObjDelegate SendToTesterObjectEvent;
        private bool bExit;
        private object BladeFileReadLockObject = new object();
        private object BladeFileWriteLockObject = new object();
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public TesterObjectStreaming()
        {
            bExit = false;
        }
        #endregion Constructors

        #region Properties
        /// <summary>
        /// property returns the path to tcl
        /// It gets it from the server.
        /// </summary>
        private string TclPath
        {
            get
            {
                string tmpStr = GetStrings("", new string[] { BladeDataName.TclPath })[0];
                return tmpStr;
            }
        }

        /// <summary>
        /// property returns the path to the blades data files.
        /// It gets it from the server.
        /// </summary>
        private string bladePath
        {
            get
            {
                return GetStrings("", new string[] { BladeDataName.BladePath })[0];
            }
        }

        /// <summary>
        /// property returns the path to the FACT files.
        /// It gets it from the server.
        /// </summary>
        private string factPath
        {
            get
            {
                return GetStrings("", new string[] { BladeDataName.FactPath })[0];
            }
        }

        /// <summary>
        /// Get grade path from host.
        /// </summary>
        private string gradePath
        {
            get
            {
                return (string)(GetStrings("", new string[] { BladeDataName.GradePath })[0]);
            }
        }

        /// <summary>
        /// Get firmware path from host.
        /// </summary>
        private string firmwarePath
        {
            get
            {
                return GetStrings("", new string[] { BladeDataName.FirmwarePath })[0];
            }
        }

        /// <summary>
        /// Result path.
        /// </summary>
        private string resultPath
        {
            get
            {
                return GetStrings("", new string[] { BladeDataName.ResultPath })[0];
            }
        }

        /// <summary>
        /// Log path
        /// </summary>
        private string logPath
        {
            get
            {
                return GetStrings("", new string[] { BladeDataName.LogPath })[0];
            }
        }

        /// <summary>
        /// Debug path.
        /// </summary>
        private string debugPath
        {
            get
            {
                return GetStrings("", new string[] { BladeDataName.DebugPath })[0];
            }
        }

        /// <summary>
        /// Get counts path from host
        /// </summary>
        private string countsPath
        {
            get
            {
                return GetStrings("", new string[] { BladeDataName.CountsPath })[0];
            }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        /// This closes and stops everything.  
        /// </summary>
        public void onClose()
        {
            bExit = true;
        }

        /// <summary>
        /// Sends log data to host GUI (and file).
        /// </summary>
        /// <param name="Text"></param>
        internal void WriteLine(string Text)
        {
            Thread WriteLineThread = new Thread(new ParameterizedThreadStart(WriteLineThreadFunc));
            WriteLineThread.IsBackground = true;
            WriteLineThread.Start((object)Text);
        }

        /// <summary>
        /// This function sends text to the Server's left and right text boxes.
        /// </summary>
        /// <param name="Text"></param>
        private void WriteLineThreadFunc(object passingObject)
        {
            string Text = (string)passingObject;

            if (Text.Trim().StartsWith(Constants.weBeDoneString)) return;

            try
            {
                StaticServerTalker.SendLineToLCD(Text);
                StaticServerTalker.MessageString = Text;
            }
            catch
            {
                // no ui
            }
        }

        /// <summary>
        /// This function sends text to the Server's left and right text boxes.
        /// </summary>
        /// <param name="Text"></param>
        internal void WriteLineContent(string Text)
        {
            Thread WriteLineContentThread = new Thread(new ParameterizedThreadStart(WriteLineContentThreadFunc));
            WriteLineContentThread.IsBackground = true;
            WriteLineContentThread.Start((object)Text);
        }

        /// <summary>
        /// This function sends text to the Server's right text box.
        /// </summary>
        /// <param name="Text"></param>
        private void WriteLineContentThreadFunc(object passingObject)
        {
            string Text = (string)passingObject;

            if (Text.Trim().StartsWith(Constants.weBeDoneString)) return;
            try
            {
                StaticServerTalker.MessageStringContent = Text;
            }
            catch
            {
                // no UI
            }
        }

        /// <summary>
        /// Changes pre-defined strings like :grade:" to grade path from host.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string fixUpThePaths(string path)
        {
            string wholePath = "";
            if (path.Length == 0)
            {
                wholePath = ".";
            }
            // if path == bunny then use bunny path from settings dialog
            else if (path.Trim().ToLower().StartsWith("bunny:") ||
                     path.Trim().ToLower().StartsWith("flash:") ||
                     path.Trim().ToLower().StartsWith("blade:"))
            {
                try
                {
                    wholePath = PathCombine(bladePath, path.Trim().Substring(6));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        // throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidDataPath"), e);
                        throw new FaultException(StaticServerTalker.getCurrentCultureString("InvalidDataPath") + " " + e.Message);
                    }
                    catch
                    {
                        throw new FaultException("InvalidDataPath" + " " + e.Message);
                    }
                }
            }
            // if path == fact then use fact path from settings dialog
            else if (path.Trim().ToLower().IndexOf("fact:") == 0)
            {
                try
                {
                    wholePath = PathCombine(factPath, path.Trim().Substring(5));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        //throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidFactPath"), e);
                        throw new FaultException(StaticServerTalker.getCurrentCultureString("InvalidFactPath") + " " + e.Message);
                    }
                    catch
                    {
                        throw new FaultException("InvalidFactPath" + " " + e.Message);
                    }
                }
            }
            // if path == fact then use grade path from settings dialog
            else if (path.Trim().ToLower().IndexOf("grade:") == 0)
            {
                try
                {
                    wholePath = PathCombine(gradePath, path.Trim().Substring(6));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        //throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidGradePath"), e);
                        throw new FaultException(StaticServerTalker.getCurrentCultureString("InvalidGradePath") + " " + e.Message);
                    }
                    catch
                    {
                        throw new FaultException("InvalidGradePath" + " " + e.Message);
                    }
                }
            }
            // if path == firm then use firmware path from settings dialog
            else if (path.Trim().ToLower().IndexOf("firmware:") == 0)
            {
                try
                {
                    wholePath = PathCombine(firmwarePath, path.Trim().Substring(9));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        //throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidFirmwarePath"), e);
                        throw new FaultException(StaticServerTalker.getCurrentCultureString("InvalidFirmwarePath") + " " + e.Message);
                    }
                    catch
                    {
                        throw new FaultException("InvalidFirmwarePath" + " " + e.Message);
                    }
                }
            }
            // if path == firm then use firmware path from settings dialog
            else if (path.Trim().ToLower().IndexOf("firm:") == 0)
            {
                try
                {
                    wholePath = PathCombine(firmwarePath, path.Trim().Substring(5));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        //throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidFirmwarePath"), e);
                        throw new FaultException(StaticServerTalker.getCurrentCultureString("InvalidFirmwarePath") + " " + e.Message);
                    }
                    catch
                    {
                        throw new FaultException("InvalidFirmwarePath" + " " + e.Message);
                    }

                }
            }
            // if path == counts then use counts path from settings dialog
            else if (path.Trim().ToLower().IndexOf("counts:") == 0 ||
                     path.Trim().ToLower().IndexOf("disk:") == 0)
            {
                try
                {
                    wholePath = PathCombine(countsPath, path.Trim().Substring(7));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        //throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidCountsPath"), e);
                        throw new FaultException(StaticServerTalker.getCurrentCultureString("InvalidCountsPath") + " " + e.Message);
                    }
                    catch
                    {
                        throw new FaultException("InvalidCountsPath" + " " + e.Message);
                    }
                }
            }
            else if (path.Trim().ToLower().IndexOf("stats:") == 0)
            {
                try
                {
                    wholePath = PathCombine(countsPath, path.Trim().Substring(6));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        //throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidCountsPath"), e);
                        throw new FaultException(StaticServerTalker.getCurrentCultureString("InvalidCountsPath") + " " + e.Message);
                    }
                    catch
                    {
                        throw new FaultException("InvalidCountsPath" + " " + e.Message);
                    }
                }
            }
            // Look for our special data path
            else if ((path.Trim().ToLower().StartsWith("data:\\")) || (path.Trim().ToLower().StartsWith("data:/")))
            {
                return @System.IO.Path.Combine(SpecialFileClass.SpecialFileFolder, path.Trim().Substring(6));
            }
            else if (path.Trim().ToLower().StartsWith("data:"))
            {
                return @System.IO.Path.Combine(SpecialFileClass.SpecialFileFolder, path.Trim().Substring(5));
            }
            // look for application startup
            else if ((path.Trim().ToLower().StartsWith("app:\\")) || (path.Trim().ToLower().StartsWith("app:/")))
            {

                return @System.IO.Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), path.Trim().Substring(5));
            }
            else if (path.Trim().ToLower().StartsWith("app:"))
            {
                return @System.IO.Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), path.Trim().Substring(4));
            }
            // look for log 
            else if ((path.Trim().ToLower().StartsWith("log:\\")) || (path.Trim().ToLower().StartsWith("log:/")))
            {
                try
                {
                    return @System.IO.Path.Combine(logPath, path.Trim().Substring(5));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidLogsPath"), e);
                    }
                    catch
                    {
                        throw new Exception("InvalidLogsPath", e);
                    }
                }
            }
            else if (path.Trim().ToLower().StartsWith("log:"))
            {
                try
                {
                    return @System.IO.Path.Combine(logPath, path.Trim().Substring(4));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidLogsPath"), e);
                    }
                    catch
                    {
                        throw new Exception("InvalidLogsPath", e);
                    }
                }
            }
            // look for debug
            else if ((path.Trim().ToLower().StartsWith("debug:\\")) || (path.Trim().ToLower().StartsWith("debug:/")))
            {
                try
                {
                    return @System.IO.Path.Combine(debugPath, path.Trim().Substring(7));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidDebugPath"), e);
                    }
                    catch
                    {
                        throw new Exception("InvalidDebugPath", e);
                    }
                }
            }
            else if (path.Trim().ToLower().StartsWith("debug:"))
            {
                try
                {
                    return @System.IO.Path.Combine(debugPath, path.Trim().Substring(6));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidDebugPath"), e);
                    }
                    catch
                    {
                        throw new Exception("InvalidDebugPath", e);
                    }
                }
            }
            // look for result
            else if ((path.Trim().ToLower().StartsWith("result:\\")) || (path.Trim().ToLower().StartsWith("result:/")))
            {
                try
                {
                    return @System.IO.Path.Combine(resultPath, path.Trim().Substring(8));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidResultPath"), e);
                    }
                    catch
                    {
                        throw new Exception("InvalidResultPath", e);
                    }
                }
            }
            else if (path.Trim().ToLower().StartsWith("result:"))
            {
                try
                {
                    return @System.IO.Path.Combine(resultPath, path.Trim().Substring(7));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        throw new Exception(StaticServerTalker.getCurrentCultureString("InvalidResultPath"), e);
                    }
                    catch
                    {
                        throw new Exception("InvalidResultPath", e);
                    }
                }
            }
            else
            {
                wholePath = path.Trim();
            }
            return wholePath;
        }

        /// <summary>
        /// Fixes paths with correct slashes for TCL.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Takes filename of file that we know exists and waits for it to show up.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="milliSecs"></param>
        private void waitForUsbDriveToWakeUp(string fileName, int milliSecs)
        {
            int count = 0;
            int delay = 100;
            int maxCount = milliSecs / delay;
            if (maxCount < 1) maxCount = 1;

            while (!Directory.Exists(Path.GetPathRoot(fileName)))
            {
                if (bExit) return;
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
        /// Open a read stream (on this computer) and return the stream.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private FileStream FileRead(string fileName)
        {
            WriteLine("FileRead called " + fileName);

            FileStream fs = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception e)
            {
                throw e;
            }
            return fs;
        }

        /// <summary>
        /// Open a read stream (on this computer) and return the stream to the remote client.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Stream BladeFileRead(string fileRequest)
        {
            lock (BladeFileReadLockObject)
            {
                try
                {
                    fileRequest = fixUpThePaths(fileRequest);
                    WriteLine("BladeFileRead called ");
                    WriteLineContent(fileRequest);
                    waitForUsbDriveToWakeUp(fileRequest, 2000);
                    return FileRead(fileRequest);
                }
                catch (Exception e)
                {
                    //throw e;
                    throw new FaultException(e.Message);
                }
            }
        }

        /// <summary>
        /// Gets a file FullStreamRequest (contains a read stream from remote) and
        /// writes it to the disk.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private FileStreamResponse FileWrite(FileStreamRequest fileRequest)
        {
            FileStreamResponse response = new FileStreamResponse();
            response.FileName = "0";

            FileStream fs = null;
            fileRequest.FileName = fixUpThePaths(fileRequest.FileName);

            if (!Directory.Exists(Path.GetDirectoryName(fileRequest.FileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileRequest.FileName));
            }

            try
            {
                fs = new FileStream(fileRequest.FileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                response.FileName = txFile(fileRequest.FileByteStream, fs).ToString();
            }
            //catch (Exception e)
            //{
            //    throw e; // just for debug; can comment out
            //}
            finally
            {
                if (fs != null)
                {
                    try
                    {
                        fs.Flush();
                        fs.Close();
                    }
                    finally
                    {
                        try { fs.Dispose(); }
                        catch { }
                    }
                }
                if (fileRequest.FileByteStream != null)
                {
                    try
                    {
                        fileRequest.FileByteStream.Close();
                    }
                    finally
                    {
                        try { fileRequest.FileByteStream.Dispose(); }
                        catch { }
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Open a write stream (on this computer and return the stream to the remote client.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public FileStreamResponse BladeFileWrite(FileStreamRequest fileRequest)
        {
            lock (BladeFileWriteLockObject)
            {
                try
                {
                    fileRequest.FileName = fixUpThePaths(fileRequest.FileName);
                    WriteLine("BladeFileWrite called ");
                    WriteLineContent(fileRequest.FileName);
                    waitForUsbDriveToWakeUp(fileRequest.FileName, 2000);
                    return FileWrite(fileRequest);
                }
                catch (Exception e)
                {
                    //throw e;
                    throw new FaultException(e.Message);
                }
            }
        }

        /// <summary>
        /// Transfer file from fromFile stream to toFile stream.
        /// </summary>
        /// <param name="fromFile"></param>
        /// <param name="toFile"></param>
        /// <returns></returns>
        private int txFile(Stream fromFile, Stream toFile)
        {
            byte[] byteArray = new byte[8192];
            int total = 0;
            int howMany = 1;

            while (howMany > 0)
            {
                howMany = fromFile.Read(byteArray, 0, 8192);
                total += howMany;
                toFile.Write(byteArray, 0, howMany);
            }
            return total;
        }

        /// <summary>
        /// Get named string(s) from TesterObject
        /// </summary>
        /// <param name="names"></param>
        public string[] GetStrings(string key, string[] names)
        {
            try
            {
                object[] passingArgs = new object[] { HostToServiceEnums.GetStrings, key, names };
                object[] retVal = (object[])CallTesterObject(passingArgs);
                string[] strArray = new string[retVal.Length];
                for (int i = 0; i < retVal.Length; i++)
                {
                    strArray[i] = (string)retVal[i];
                }
                return strArray;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Call to TesterObject via host
        /// </summary>
        /// <param name="objArray"></param>
        /// <returns></returns>
        public object CallTesterObject(object[] objArray)
        {
            ObjObjDelegate handler = SendToTesterObjectEvent;
            if (handler != null)
            {
                return handler(objArray);
            }
            return new object[] { "" };
        }
        #endregion Methods
    } // end class
} // end namespace
