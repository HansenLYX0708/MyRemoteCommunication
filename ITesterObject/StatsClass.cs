using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using WD.Tester.Enums;

namespace WD.Tester.Module
{
    //[Serializable]
    public class StatsClass
    {
        #region Fields
        private enum StatsOperations
        {
            Add,
            GetAvg,
            GetFirstLast,
            StartStopTime,
            DGR,
        }
        public DateTime FirstRecordedTime;
        public DateTime LastRecordedTime;
        public string PathToFiles;
        private string pathToApplication;
        private Int64 lastTotalInAllFiles;
        private Int64 firstTotalInAllFiles;
        private Int64 lastIndexInAllFiles;
        private Int64 firstIndexInAllFiles;
        private XmlDocument anXmlDocument;

        private string[] fileList;
        private List<string> misTestContactList;
        private List<string> misTestInitializeList;
        private List<string> misTestMeasurementList;
        private List<string> passTestList;
        private List<string> failTestList;

        /// <summary>
        /// Sends DGR info back to Jade exe.
        /// </summary>
        public event StatusEventHandler StatisticsDGREvent;

        /// <summary>
        /// Sends Asynchronous events of avg results back to Jade.
        /// </summary>
        public event StatusEventHandler StatisticsValueEvent;

        public event StatusEventHandler StatisticsMinMaxEvent;

        public event StatusEventHandler StatisticsFromToTimeEvent;

        private object doItLockObject = new object();
        private delegate void clearStatsDelegate();
        private object clearLockObject = new object();
        #endregion Fields

        #region Constructors
        public StatsClass(string statsPath, string applicationPath)
        {
            PathToFiles = statsPath;
            pathToApplication = applicationPath;
            //misTestContactList = new List<string>();
            //misTestInitializeList = new List<string>();
            //misTestMeasurementList = new List<string>();
            anXmlDocument = new XmlDocument();
            try
            {
                InitFileList();
            }
            catch { }
            Int64 tmpInt = 0;
            Int64 tmpInt2 = 0;
            FirstRecordedTime = getFirstRecordedTime(out tmpInt, out tmpInt2);
            firstTotalInAllFiles = tmpInt;
            firstIndexInAllFiles = tmpInt2;
            LastRecordedTime = getLastRecordedTime(out tmpInt, out tmpInt2);
            lastTotalInAllFiles = tmpInt;
            lastIndexInAllFiles = tmpInt2;
        }
        #endregion Constructors

        #region Properties

        #endregion Properties

        #region Methods
        private void SendStatisticsDGREvent(string text)
        {
            // "<Row>::<Name>::<value>"   
            StatusEventArgs args = new StatusEventArgs(text, (int)eventInts.StatisticsDGREvent);
            StatusEventHandler handler = StatisticsDGREvent;
            if (handler != null)
            {
                handler(null, args);
            }
        }



        /// <summary>
        /// Send statistics data to Jade.
        /// </summary>
        /// <param name="varName"></param>
        /// <param name="result"></param>
        private void SendStatisticsValueEvent(string text)
        {
            // "Grade_A1::98::100::False"    <count name> :: <value> :: <total> :: <estimate>
            //  <value> / <total> = average
            StatusEventArgs args = new StatusEventArgs(text, (int)eventInts.StatisticsValueEvent);
            StatusEventHandler handler = StatisticsValueEvent;
            if (handler != null)
            {
                handler(null, args);
            }
        }



        private void SendStatisticsMinMaxEvent(string text)
        {
            // "<first index>::<first total>::<time date>::<last index>::<last total>::<time date>"
            StatusEventArgs args = new StatusEventArgs(text, (int)eventInts.StatisticsMinMaxEvent);
            StatusEventHandler handler = StatisticsMinMaxEvent;
            if (handler != null)
            {
                handler(this, args);
            }
        }



        private void SendStatisticsFromToTimeEvent(string text)
        {
            // "<start time date>::<end time date>"
            StatusEventArgs args = new StatusEventArgs(text, (int)eventInts.StatisticsFromToTimeEvent);
            StatusEventHandler handler = StatisticsFromToTimeEvent;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public void GetFirstLastIndexViaEvent()
        {
            doit(StatsOperations.GetFirstLast, "", DateTime.Now, 0, 0);
        }

        private void getFirstLastIndexViaEvent()
        {
            Int64 tmpInt = 0;
            Int64 tmpInt2 = 0;
            FirstRecordedTime = getFirstRecordedTime(out tmpInt, out tmpInt2);
            firstTotalInAllFiles = tmpInt;
            firstIndexInAllFiles = tmpInt2;
            LastRecordedTime = getLastRecordedTime(out tmpInt, out tmpInt2);
            lastTotalInAllFiles = tmpInt;
            lastIndexInAllFiles = tmpInt2;
            SendStatisticsMinMaxEvent(
               firstIndexInAllFiles.ToString() + "::" +
               firstTotalInAllFiles.ToString() + "::" +
               FirstRecordedTime.Ticks.ToString() + "::" +
               lastIndexInAllFiles.ToString() + "::" +
               lastTotalInAllFiles.ToString() + "::" +
               LastRecordedTime.Ticks.ToString());
        }


        /// <summary>
        /// Get a list of all the stats files in the stats directory.
        /// </summary>
        private void InitFileList()
        {
            try
            {
                fileList = Directory.GetFiles(PathToFiles, "*." + Constants.StatsExt);
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(PathToFiles);
                fileList = Directory.GetFiles(PathToFiles, "*." + Constants.StatsExt);
            }

            initMisTestFileLists();
        }

        /// <summary>
        /// This function opens the XML error definition file (this file matches error codes to error types),
        ///  finds matching error history files, and assembles lists or error (or pass) types.
        /// 
        /// </summary>
        private void initMisTestFileLists()
        {
            FileStream fs = null;
            try
            {
                // Init the five error/pass categorie lists.
                misTestContactList = new List<string>();
                misTestInitializeList = new List<string>();
                misTestMeasurementList = new List<string>();
                passTestList = new List<string>();
                failTestList = new List<string>();

                // Open XML "error code" map file.
                fs = new FileStream(System.IO.Path.Combine(pathToApplication, Constants.ErrorCodeMap), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                anXmlDocument.Load(fs);
                fs.Close();

                // Loop through each node (one for each error code).
                for (int i = 0; i < anXmlDocument.DocumentElement.ChildNodes.Count; i++)
                {
                    string fieldCode = anXmlDocument.DocumentElement.ChildNodes[i].Attributes[0].Value;    // error code
                                                                                                           //string fieldName = anXmlDocument.DocumentElement.ChildNodes[i].Attributes[1].Value;  // text description
                    string fieldType = anXmlDocument.DocumentElement.ChildNodes[i].Attributes[2].Value;    // error category/type

                    // Assemble a filename and path.
                    string fileName = System.IO.Path.Combine(PathToFiles, Constants.ErrorToken + fieldCode + "." + Constants.StatsExt);

                    // Does this file exist?
                    if (File.Exists(fileName))
                    {
                        // If file exists, add the the proper list.
                        if (fieldType.ToLower() == Constants.InitializeAttrib)
                        {
                            misTestInitializeList.Add(fileName);
                        }
                        else if (fieldType.ToLower() == Constants.ContactAttrib)
                        {
                            misTestContactList.Add(fileName);
                        }
                        else if (fieldType.ToLower() == Constants.MeasureAttrib)
                        {
                            misTestMeasurementList.Add(fileName);
                        }
                        else if (fieldType.ToLower() == Constants.PassAttrib)
                        {
                            passTestList.Add(fileName);
                        }
                        else if (fieldType.ToLower() == Constants.FailAttrib)
                        {
                            failTestList.Add(fileName);
                        }
                    } // end if matching error code file is there.
                }  // end for each node
            }
            catch
            {
                // We are toast, but please don't crash the program.
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }


        /// <summary>
        /// When was the first entry recorded.  
        /// </summary>
        /// <returns></returns>
        private DateTime getFirstRecordedTime(out Int64 firstTotal, out Int64 firstConsecutiveIndex)
        {
            firstTotal = 0;
            firstConsecutiveIndex = 0;
            DateTime firstOne = DateTime.Now;
            FileStream fs = null;
            BinaryReader br = null;
            foreach (string aFile in fileList)
            {
                try
                {
                    fs = new FileStream(aFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    br = new BinaryReader(fs);
                    StatsRecord firstRecord = new StatsRecord();
                    firstRecord.Read(br);
                    if (firstRecord.RecordTime < firstOne)
                    {
                        firstOne = firstRecord.RecordTime;
                        firstTotal = firstRecord.TotalCount;
                        firstConsecutiveIndex = firstRecord.RecordIndex;
                    }
                }
                catch
                {
                }
                finally
                {
                    if (br != null)
                    {
                        br.Close();
                        br = null;
                    }
                    if (fs != null)
                    {
                        fs.Dispose();
                        fs = null;
                    }
                }
            }

            return firstOne;
        }

        /// <summary>
        /// What was the last index recorded.
        /// What was the last recorded time.
        /// </summary>
        /// <returns></returns>
        private DateTime getLastRecordedTime(out Int64 lastTotal, out Int64 lastIndex)
        {
            lastTotal = 0;
            lastIndex = 0;

            DateTime theLastTime = new DateTime(0);
            foreach (string aFile in fileList)
            {
                try
                {
                    StatsRecord lastRecord = getLastRecordInFile(aFile);
                    if (lastRecord.RecordIndex > lastIndex || theLastTime < lastRecord.RecordTime)
                    {
                        theLastTime = lastRecord.RecordTime;
                        lastTotal = lastRecord.TotalCount;
                        lastIndex = lastRecord.RecordIndex;
                    }
                }
                catch { }
            }

            return theLastTime;
        }

        public void AddAnItemToFile(string which, DateTime when, Int64 total, Int64 value)
        {
            doit(StatsOperations.Add, which, when, total, value);
        }

        private void addAnItemToFiles(string which, DateTime when, Int64 total, Int64 value)
        {
            if (total < 1) total = 1;
            LastRecordedTime = when;

            string theFilename = System.IO.Path.Combine(PathToFiles, which + "." + Constants.StatsExt);
            if (!File.Exists(theFilename))
            {
                addAnItemToFile(which, when, total, value, false);
            }

            SendStatisticsMinMaxEvent(
               firstIndexInAllFiles.ToString() + "::" +
               firstTotalInAllFiles.ToString() + "::" +
               FirstRecordedTime.Ticks.ToString() + "::" +
               lastIndexInAllFiles.ToString() + "::" +
               lastTotalInAllFiles.ToString() + "::" +
               LastRecordedTime.Ticks.ToString());

            foreach (string pathStr in fileList)
            {
                string countName = Path.GetFileNameWithoutExtension(pathStr);

                StatsRecord formerOne = getLastRecordInFile(pathStr);
                if (formerOne.TotalCount != total)
                {
                    if (countName == which && BladeDataName.TestCount == which)
                    {
                        addAnItemToFile(countName, when, total, value, true);
                    }
                    else
                    {
                        addAnItemToFile(countName, when, total, formerOne.Value, true);
                    }
                }
                else
                {
                    if (countName == which)
                    {
                        updateAnItemInFile(countName, when, total, value);
                    }
                    //else
                    //{
                    //   updateAnItemInFile(countName, when, formerOne.Value);
                    //}
                }
            }
        }

        private StatsRecord getLastRecordInFile(string aFile)
        {
            FileStream fs = null;
            BinaryReader br = null;
            StatsRecord lastRecord = new StatsRecord();
            try
            {
                fs = new FileStream(aFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Seek(-StatsRecord.RecordSize, SeekOrigin.End);
                br = new BinaryReader(fs);
                lastRecord.Read(br);
            }
            catch
            {
                // Please leave this catch.
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                    br = null;
                }
                if (fs != null)
                {
                    fs.Dispose();
                    fs = null;
                }
            }
            return lastRecord;
        }

        private void updateAnItemInFile(string which, DateTime when, Int64 total, Int64 value)
        {
            FileStream fs = null;
            BinaryWriter bw = null;
            BinaryReader br = null;
            string filename = System.IO.Path.Combine(PathToFiles, which + "." + Constants.StatsExt);
            try
            {
                fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                bw = new BinaryWriter(fs);
                br = new BinaryReader(fs);
                StatsRecord lastRecord = new StatsRecord();

                fs.Seek(-StatsRecord.RecordSize, SeekOrigin.End);
                lastRecord.Read(br);
                lastRecord.Value = value;
                lastRecord.RecordTime = when;
                lastRecord.TotalCount = total;
                lastRecord.Write(bw);
                bw.Flush();
            }
            finally
            {
                if (bw != null)
                {
                    bw.Close();
                }
                if (br != null)
                {
                    br.Close();
                }
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        private void addAnItemToFile(string which, DateTime when, Int64 total, Int64 value, bool next)
        {
            FileStream fs = null;
            BinaryWriter bw = null;
            BinaryReader br = null;
            string filename = System.IO.Path.Combine(PathToFiles, which + "." + Constants.StatsExt);
            bool found = File.Exists(filename);
            try
            {
                fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                bw = new BinaryWriter(fs);
                br = new BinaryReader(fs);
                StatsRecord lastRecord = new StatsRecord();
                if (fs.Length >= StatsRecord.RecordSize)
                {
                    fs.Seek(-StatsRecord.RecordSize, SeekOrigin.End);
                    lastRecord.Read(br);
                }
                else
                {
                    lastRecord.RecordIndex = lastIndexInAllFiles;
                }


                Int64 tmpIndex = lastRecord.RecordIndex;
                if (next)
                {
                    tmpIndex++;
                }

                StatsRecord statsRecord = new StatsRecord(total, value, tmpIndex);
                fs.Seek(0, SeekOrigin.End);
                statsRecord.Write(bw);
                bw.Flush();

                if (tmpIndex > lastIndexInAllFiles)
                {
                    lastIndexInAllFiles = tmpIndex;
                }
            }
            finally
            {
                if (bw != null)
                {
                    bw.Close();
                }
                if (br != null)
                {
                    br.Close();
                }
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
            if (!found) InitFileList();
        }

        public void GetAverage(string what, Int64 from, Int64 to)
        {
            doit(StatsOperations.GetAvg, what, DateTime.Now, from, to);
        }

        private void getAverage(string what, Int64 from, Int64 to)
        {
            bool trunkRange = false;

            Int64 firstRecordIndex = 0;
            Int64 lastRecordIndex = 0;
            FileStream fs = null;
            BinaryReader br = null;
            string filename = System.IO.Path.Combine(PathToFiles, what + "." + Constants.StatsExt);
            if (!File.Exists(filename)) return;
            try
            {
                // open file
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                br = new BinaryReader(fs);

                // Get first record index.
                StatsRecord firstRecordInFile = new StatsRecord();
                fs.Seek(0, SeekOrigin.Begin);
                firstRecordInFile.Read(br);
                firstRecordIndex = firstRecordInFile.RecordIndex;

                // Get last record index.
                fs.Seek(-StatsRecord.RecordSize, SeekOrigin.End);
                StatsRecord lastRecordInFile = new StatsRecord();
                lastRecordInFile.Read(br);
                lastRecordIndex = lastRecordInFile.RecordIndex + 1;

                // If asking for more than what is there, trunk to what is there.
                if (firstRecordIndex > from)
                {
                    from = firstRecordIndex;
                    trunkRange = true;
                }
                if (lastRecordIndex < from)
                {
                    from = lastRecordIndex;
                    trunkRange = true;
                }
                if (lastRecordIndex < to)
                {
                    to = lastRecordIndex;
                    trunkRange = true;
                }

                // Get ending record that we are seeking
                StatsRecord lastRecord = new StatsRecord();
                Int64 offset = to - firstRecordIndex;
                if (offset < 0) offset = 0;
                lastRecord = getSomeRecord(fs, br, offset);

                // Get one previous to start.
                if ((from - firstRecordIndex) > 0)
                {
                    from--;
                }
                else
                {
                    trunkRange = true;
                }

                Int64 longNumerator;
                Int64 longDivisor;

                // Calculate average.  
                if (trunkRange) // If trunked use end only.
                {
                    longNumerator = lastRecord.Value;
                    longDivisor = lastRecord.TotalCount;
                }
                else // All there, so do regular calculation.
                {
                    StatsRecord onePreviousRecord = new StatsRecord();
                    offset = from - firstRecordIndex;
                    if (offset < 0) offset = 0;
                    onePreviousRecord = getSomeRecord(fs, br, offset);
                    longNumerator = lastRecord.Value - onePreviousRecord.Value;
                    longDivisor = lastRecord.TotalCount - onePreviousRecord.TotalCount;
                }

                // Avoid divide by zero
                if (longDivisor < 1)
                {
                    longDivisor = 1;
                }

                SendStatisticsValueEvent(what + "::" + longNumerator.ToString() + "::" + longDivisor.ToString() + "::" + trunkRange.ToString());
            }
            catch
            {
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        private StatsRecord getSomeRecord(FileStream fs, BinaryReader br, Int64 index)
        {
            //if(index > 0) index--;
            fs.Seek(index * StatsRecord.RecordSize, SeekOrigin.Begin);
            StatsRecord aRec = new StatsRecord();
            aRec.Read(br);
            return aRec;
        }

        private void doit(StatsOperations whatToDo, string which, DateTime when, Int64 item1, Int64 item2)
        {
            lock (doItLockObject)
            {
                switch (whatToDo)
                {
                    case StatsOperations.Add:
                        addAnItemToFiles(which, when, item1, item2);
                        break;
                    case StatsOperations.GetAvg:
                        getAverage(which, item1, item2);
                        break;
                    case StatsOperations.GetFirstLast:
                        getFirstLastIndexViaEvent();
                        break;
                    case StatsOperations.StartStopTime:
                        startStopTime(item1, item2);
                        break;
                    case StatsOperations.DGR:
                        getDGRValues((TimeSpan)(DateTime.Now - when));
                        break;
                }
            }
        }

        public void ClearStats()
        {
            lock (clearLockObject)
            {
                clearStatsDelegate clearIt = new clearStatsDelegate(clearStats);
                clearIt.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { clearIt.EndInvoke(ar); }), clearIt);
            }
        }

        private void clearStats()
        {
            foreach (string filename in fileList)
            {
                try
                {
                    File.Delete(filename);
                }
                catch { };
            }
        }

        public void StartStopTime(Int64 from, Int64 to)
        {
            doit(StatsOperations.StartStopTime, "", DateTime.Now, from, to);
        }

        private void startStopTime(Int64 from, Int64 to)
        {
            Int64 firstRecordIndex = 0;
            Int64 lastRecordIndex = 0;
            FileStream fs = null;
            BinaryReader br = null;
            string filename = System.IO.Path.Combine(PathToFiles, BladeDataName.TestCount + "." + Constants.StatsExt);
            if (!File.Exists(filename)) return;
            try
            {
                // open file
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                br = new BinaryReader(fs);

                // Get first record index.
                StatsRecord firstRecordInFile = new StatsRecord();
                fs.Seek(0, SeekOrigin.Begin);
                firstRecordInFile.Read(br);
                firstRecordIndex = firstRecordInFile.RecordIndex;

                // Get last record index.
                fs.Seek(-StatsRecord.RecordSize, SeekOrigin.End);
                StatsRecord lastRecordInFile = new StatsRecord();
                lastRecordInFile.Read(br);
                lastRecordIndex = lastRecordInFile.RecordIndex + 1;

                // If asking for more than what is there, trunk to what is there.
                if (firstRecordIndex > from)
                {
                    from = firstRecordIndex;
                }
                if (lastRecordIndex < from)
                {
                    from = lastRecordIndex;
                }
                if (lastRecordIndex < to)
                {
                    to = lastRecordIndex;
                }

                // Get ending record that we are seeking
                StatsRecord lastRecord = new StatsRecord();
                Int64 offset = to - firstRecordIndex + 1;
                if (offset < 0) offset = 0;
                lastRecord = getSomeRecord(fs, br, offset);

                DateTime fromTime;
                DateTime untilTime;

                StatsRecord startRecord = new StatsRecord();
                offset = from - firstRecordIndex;
                if (offset < 0) offset = 0;
                startRecord = getSomeRecord(fs, br, offset);
                fromTime = startRecord.RecordTime;
                untilTime = lastRecord.RecordTime;

                SendStatisticsFromToTimeEvent(fromTime.Ticks.ToString() + "::" + untilTime.Ticks.ToString());
            }
            catch
            {
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        public void GetDGRValues(TimeSpan timeSpan)
        {
            doit(StatsOperations.DGR, "", DateTime.Now - timeSpan, 0, 0);
        }

        /// <summary>
        /// Read error log files, calculate various DGR parameters, and send events to Jade with results.
        /// </summary>
        private void getDGRValues(TimeSpan spanOfTime)
        {
            TimeSpan originalSpanOfTime = spanOfTime;
            initMisTestFileLists();

            Int64 dmrContactCount = 0;
            Int64 dgrTotalCount = 0;
            Int64 dgrPassCount = 0;
            Int64 dgrFailCount = 0;
            Int64 dmrMeasureCount = 0;
            Int64 dmrInitCount = 0;
            Int64 dgrOutputCount = 0;
            bool bProjected = false;
            if (spanOfTime < new TimeSpan(0, 0, 30))
            {
                bProjected = true;
                spanOfTime = new TimeSpan(0, 30, 00);
            }

            dgrPassCount = getSomeDgrCount(passTestList, spanOfTime);
            dgrFailCount = getSomeDgrCount(failTestList, spanOfTime);
            dmrContactCount = getSomeDgrCount(misTestContactList, spanOfTime);
            dmrInitCount = getSomeDgrCount(misTestInitializeList, spanOfTime);
            dmrMeasureCount = getSomeDgrCount(misTestMeasurementList, spanOfTime);

            if (bProjected)
            {   // Half-hour value times 48 is the projected 24 hour value.
                dgrPassCount *= 48;
                dgrFailCount *= 48;
                dmrContactCount *= 48;
                dmrInitCount *= 48;
                dmrMeasureCount *= 48;
            }

            dgrTotalCount = dmrContactCount + dgrPassCount + dgrFailCount + dmrMeasureCount + dmrInitCount;
            dgrOutputCount = dgrPassCount + dgrFailCount;

            // "<Row>::<Name>::<value>"   name == key in Jade's Strings class.
            int rowNumber = 0;
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "DgrOutput" + "::" + dgrOutputCount.ToString() + "::" + originalSpanOfTime);  //DgrOutput is key in Jade's Strings class
            rowNumber++;
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "DGRTotal" + "::" + dgrTotalCount.ToString() + "::" + originalSpanOfTime);  //TotalDGR is key in Jade's Strings class
            rowNumber++;
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "DGRPass" + "::" + dgrPassCount.ToString() + "::" + originalSpanOfTime);
            rowNumber++;
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "DGRFail" + "::" + dgrFailCount.ToString() + "::" + originalSpanOfTime);
            rowNumber++;
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "DGRContactMisTest" + "::" + dmrContactCount.ToString() + "::" + originalSpanOfTime);
            rowNumber++;
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "DGRInitMisTest" + "::" + dmrInitCount.ToString() + "::" + originalSpanOfTime);
            rowNumber++;
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "DGRMeasureMisTest" + "::" + dmrMeasureCount.ToString() + "::" + originalSpanOfTime);
            rowNumber++;

            // Utilization place holder.
            SendStatisticsDGREvent(rowNumber.ToString() + "::" + "UTIL" + "::" + " " + "::" + originalSpanOfTime);
        }

        /// <summary>
        /// Takes a start time (stop time is now)and adds up the counts from 
        ///  each history file in the passed in list.
        /// </summary>
        /// <param name="whichList"></param>
        /// <param name="aTimeSpan"></param>
        /// <returns></returns>
        private Int64 getSomeDgrCount(List<string> whichList, TimeSpan aTimeSpan)
        {
            FileStream fsDgrCount = null;
            BinaryReader brDgrCount = null;
            Int64 tmpInt = 0;

            try
            {
                // Do this for each file in the list.
                foreach (string filename in whichList)
                {
                    try
                    {
                        fsDgrCount = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        brDgrCount = new BinaryReader(fsDgrCount);

                        // Read end record.
                        StatsRecord lastRecord = getLastRecordInFile(filename);
                        // Get start time
                        DateTime startTime = lastRecord.RecordTime.Subtract(aTimeSpan);
                        // Get index to starting record.
                        Int64 start = findTimeRecordWithBinarySearch(fsDgrCount, brDgrCount, startTime);
                        // Read start record.
                        StatsRecord startRecord = getSomeRecord(fsDgrCount, brDgrCount, start);
                        // Subtract start from stop (we only look at the last xxx counts).
                        tmpInt += lastRecord.Value - startRecord.Value;  // count of items for this time span.
                    }
                    catch
                    {
                        // Please leave this catch.
                    }
                    finally
                    {
                        if (brDgrCount != null)
                        {
                            brDgrCount.Close();
                        }
                    }
                } // end for each file in list
            }
            finally
            {
                if (fsDgrCount != null)
                {
                    try { fsDgrCount.Dispose(); }
                    catch { }
                }
            }
            return tmpInt;
        }

        /// <summary>
        /// Returns index to record of Closest record to requested time.
        /// </summary>
        /// <param name="when">DateTime to look for.</param>
        /// <returns></returns>
        private Int64 findTimeRecordWithBinarySearch(FileStream fs, BinaryReader br, DateTime when)
        {
            Int64 recToFind = 0;
            try
            {
                // Divide file size by record length = record count.
                Int64 recordCount = fs.Length / StatsRecord.RecordSize;

                // define initial search range.
                Int64 upperRecord = recordCount;
                Int64 lowerRecord = 0;
                bool done = false;
                int realCloseCount = 0;
                recToFind = (upperRecord - lowerRecord) / 2;
                while (!done)
                {
                    StatsRecord aRec = getSomeRecord(fs, br, recToFind);
                    if (roughlyEquals(aRec.RecordTime, when, 15))
                    {
                        done = true;
                    }
                    else if (aRec.RecordTime > when)
                    {
                        upperRecord = recToFind;
                    }
                    else
                    {
                        lowerRecord = recToFind;
                    }
                    recToFind = lowerRecord + ((upperRecord - lowerRecord) / 2);
                    if (recToFind >= recordCount) recToFind = recordCount - 1;
                    if (recToFind < 0) recToFind = 0;

                    if (upperRecord - lowerRecord <= 2)
                    {
                        realCloseCount++;
                    }

                    if (realCloseCount > 3) done = true;

                } // end while
            }
            catch
            {
                recToFind = -1;
            }
            return recToFind;
        }


        private bool roughlyEquals(DateTime refTime, DateTime compareTime, int windowInSeconds)
        {
            long delta = (long)((TimeSpan)(refTime - compareTime)).TotalSeconds;
            return System.Math.Abs(delta) < windowInSeconds;
        }
        #endregion Methods
    } // end class
} // end namaspace
