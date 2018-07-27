using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;

namespace WD.Tester.Module
{
    [Serializable]
    public class CountsClass : ISerializable
    {
        #region Fields
        public Dictionary<string, int> CountDictionary;
        public string OwnerSerialNumber;
        private int retryCountOfToString = 0;
        private readonly object writeLockObjectFile = new object();
        private readonly object writeLockObjectStream = new object();
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public CountsClass()
        {
            CountDictionary = new Dictionary<string, int>();
            OwnerSerialNumber = "NONE";
        }

        public CountsClass(string _OwnerSerialNumber)
        {
            CountDictionary = new Dictionary<string, int>();
            OwnerSerialNumber = _OwnerSerialNumber;
        }

        /// <summary>
        /// Serializable constructor
        /// </summary>
        /// <param name="si"></param>
        /// <param name="ctx"></param>
        public CountsClass(SerializationInfo si, StreamingContext ctx)
        {
            CountDictionary = new Dictionary<string, int>();
            string aKey = "";
            int aInt = 0;
            int sizeOfMe = 0;

            sizeOfMe = si.GetInt32("sizeOfMe");
            for (int i = 0; i < sizeOfMe; i++)
            {
                aKey = si.GetString("aKey" + i.ToString());
                aInt = si.GetInt32("aInt" + i.ToString());
                CountDictionary.Add(aKey, aInt);
            }
            OwnerSerialNumber = si.GetString("MeOwner");
        }

        #endregion Constructors

        #region Properties

        #endregion Properties

        #region Methods
        //[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            int sizeOfMe = CountDictionary.Count;

            // Fill up the SerializationInfo object with the formatted data.
            info.AddValue("sizeOfMe", sizeOfMe);
            int i = 0;  // index
            foreach (KeyValuePair<string, int> anEntry in CountDictionary)
            {
                info.AddValue("aKey" + i.ToString(), anEntry.Key);
                info.AddValue("aInt" + i.ToString(), (Int32)anEntry.Value);
                i++;
            }
            info.AddValue("MeOwner", OwnerSerialNumber);
        }

        /// <summary>
        /// Converts dictionary to comma separated string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("count, " + CountDictionary.Count.ToString());
            try
            {
                foreach (KeyValuePair<string, int> aItem in CountDictionary)
                {
                    sb.Append(", " + aItem.Key + ", " + aItem.Value.ToString());
                }
                sb.Append(" ");
                sb.Append(", MeOwner, " + OwnerSerialNumber + " ");
                retryCountOfToString = 0;
                return sb.ToString();
            }
            catch (Exception e)
            {
                if (retryCountOfToString > 3)
                {
                    retryCountOfToString = 0;
                    throw e;
                }
                Thread.Sleep(100);
                retryCountOfToString++;
                return this.ToString();
            }
        }

        /// <summary>
        /// Takes input string and puts in dictionary
        /// Same string as ToString creates
        /// "count, x, DiskLoadCount, 5, PatrolCount, 6, ScanCount, 7, TestCount, 8 ..."
        /// </summary>
        /// <param name="parseString"></param>
        public void FromString(string parseString)
        {
            int startSpot = 0;
            int stopSpot;

            // Our first comma is the comma after first the token (always count)
            startSpot = parseString.IndexOf(", ", startSpot) + 2;
            if (startSpot < 0 || startSpot >= parseString.Length) return;
            // The next comma (if there) is past the count of items
            stopSpot = parseString.IndexOf(", ", startSpot);
            // if zero items then stopSpot == -1
            if (stopSpot < 0)
            {
                // set to end
                stopSpot = parseString.Length - 1;
            }
            int tmpInt;
            bool bOK;
            // Puts item count into tmpCount
            int length = stopSpot - startSpot;
            if (stopSpot < 0) { return; }
            if (length < 0) { return; }
            bOK = int.TryParse(parseString.Substring(startSpot, length), out tmpInt);
            if (!bOK) return;

            // zero out dictionary
            CountDictionary = new Dictionary<string, int>();
            // if no items then return
            if (tmpInt <= 0)
            {
                return;
            }
            int itemsLeft = tmpInt;

            // move start spot to first real token's spot
            startSpot = stopSpot + 2;

            // loop until finished (we run out of string)
            while (startSpot < parseString.Length)
            {
                // find end of next token name
                stopSpot = parseString.IndexOf(",", startSpot);
                string aKey = parseString.Substring(startSpot, stopSpot - startSpot);
                startSpot = stopSpot + 1; // start past the token name
                // find end of number
                stopSpot = parseString.IndexOf(",", startSpot);
                if (stopSpot < 0)
                {
                    stopSpot = parseString.Length - 1;
                }

                if (itemsLeft > 0)
                {
                    bOK = int.TryParse(parseString.Substring(startSpot, stopSpot - startSpot), out tmpInt);
                    if (!bOK)
                    {
                        bOK = int.TryParse(parseString.Substring(startSpot, stopSpot - startSpot + 1), out tmpInt);
                        if (!bOK) return;
                    }

                    // add one to the dictionary
                    CountDictionary.Add(aKey.Trim(), tmpInt);
                }
                else
                {
                    OwnerSerialNumber = parseString.Substring(startSpot, stopSpot - startSpot + 1).Trim();
                }
                // move to next
                startSpot = stopSpot + 1;
                itemsLeft--;
            }

        }

        /// <summary>
        /// Removes all count entries except Mandatory ones.
        /// </summary>
        public virtual void ClearCounts()
        {
            List<string> delList = new List<string>();
            List<string> zeroList = new List<string>();
            // make up list of stuff to delete
            foreach (string name in CountDictionary.Keys)
            {
                // if one of the mandatory ones then skip 
                if (name == BladeDataName.TestCount ||
                    name == BladeDataName.ScanCount ||
                    name == BladeDataName.PatrolCount ||
                    name == BladeDataName.MemsCount ||
                    name == BladeDataName.DiskLoadCount)
                {
                    zeroList.Add(name);
                    continue;
                }
                // one to delete
                delList.Add(name);
            }

            // Zero items in zero list
            foreach (string name in zeroList)
            {
                SetValue(name, 0);
            }

            // delete items in delete list
            foreach (string name in delList)
            {
                CountDictionary.Remove(name);
            }
        }

        /// <summary>
        /// Get a value from dictionary.
        /// If key not found and mandatory then add it (with 0).
        /// If key not found and not mandatory then return -1.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual int GetValue(string name)
        {
            if (CountDictionary.ContainsKey(name))
            {
                return CountDictionary[name];
            }
            else
            {
                CountDictionary.Add(name, 0);
                return 0;
            }
        }

        /// <summary>
        /// Sets a value.
        /// If key there then update.
        /// If key not there then add.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public virtual void SetValue(string name, int value)
        {
            if (CountDictionary.ContainsKey(name))
            {
                CountDictionary[name] = value;
            }
            else
            {
                CountDictionary.Add(name, value);
            }
        }

        /// <summary>
        /// Increment a value.
        /// If key there inc.
        /// If key not there then add at 1.
        /// </summary>
        /// <param name="name"></param>
        public virtual void IncValue(string name)
        {
            if (CountDictionary.ContainsKey(name))
            {
                CountDictionary[name] = CountDictionary[name] + 1;
            }
            else
            {
                CountDictionary.Add(name, 1);
            }
        }

        /// <summary>
        /// Read in from file.
        /// </summary>
        /// <param name="filename"></param>
        public void readInData(string filename)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                readInData(fs);
                //fs.Close(); closed in above function
            }
            finally
            {
                if (fs != null)
                {
                    try { fs.Dispose(); } // just in case.
                    catch { }
                }
            }
        }

        /// <summary>
        /// Read in from stream.
        /// </summary>
        /// <param name="fs"></param>
        public void readInData(Stream fs)
        {
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(fs);

                string fromString = sr.ReadLine();
                sr.Close();
                FromString(fromString);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Dispose();
                }
                if (fs != null)
                {
                    fs.Dispose();
                    fs = null;
                }
            }
        }

        /// <summary>
        /// Write out to filename
        /// </summary>
        /// <param name="filename"></param>
        public void writeOutData(string filename)
        {
            lock (writeLockObjectFile)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    writeOutData(fs);
                    //fs.Flush(); // closed in above function
                    //fs.Close(); 
                }
                finally
                {
                    if (fs != null)
                    {
                        try { fs.Dispose(); } // just in case.
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Write out with stream.
        /// </summary>
        /// <param name="fs"></param>
        public void writeOutData(FileStream fs)
        {
            lock (writeLockObjectStream)
            {
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(fs);
                    sw.WriteLine(ToString());
                    sw.Flush();
                    sw.Close();
                }
                finally
                {
                    if (sw != null)
                    {
                        try
                        {
                            sw.Dispose();
                            sw = null;
                        }
                        catch { }
                    }
                    if (fs != null)
                    {
                        try
                        {
                            fs.Dispose();
                            fs = null;
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Does a deep copy.
        /// </summary>
        /// <param name="that">CountsClass to copy from</param>
        public void Assign(CountsClass that)
        {
            this.FromString(that.ToString());
        }

        public Dictionary<string, int>.KeyCollection Keys()
        {
            return CountDictionary.Keys;
        }

        public Dictionary<string, int>.ValueCollection Values()
        {
            return CountDictionary.Values;
        }

        public bool ContainsKey(string Key)
        {
            return CountDictionary.ContainsKey(Key);
        }

        public bool CountsEqual(CountsClass compareToThis)
        {
            if (this.ToString() == compareToThis.ToString())
            {
                return true;
            }
            return false;
        }
        #endregion Methods

    } // end class
} // end namespace
