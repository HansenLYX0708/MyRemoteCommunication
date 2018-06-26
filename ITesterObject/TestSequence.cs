// ************************************************************************************
//  Reads in and writes out the xml test files and populates dictionaries with test 
// parameters.
// It is used to get the test defs for all defined tests and for reading
// individual test sequences.
//
// Copyright 2007 Hitachi Global Storage Technologies, Inc.
//
// This code is proprietary to Hitachi Global Storage Technologies and cannot be used, 
//  modified, or copied except with the express permission of Hitachi Global Storage 
//  Technologies.
// Robert L. Kimball February 26, 2007
// ************************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Drawing;
using Hitachi.Tester.Sequence;
using System.Windows.Forms;
using Hitachi.Tester.IJadeCommonTables;

namespace Hitachi.Tester.Sequence
{
    /// <summary>
    /// This class is a struct that holds one test sequence.
    /// Has methods to read in from xml config files and write out to xml config files.
    /// </summary>
    [Serializable]
    public class TestSequence
    {
        #region Fields
        public Dictionary<string, string> dictionaryHeader;
        public ArrayList ArrayListCols;  // master list of columns 
        public ArrayList ArrayListTests;  // all of the tests in this sequence
        public bool NewFile;
        public bool Modified;
        public string FileName;
        #endregion Fields

        #region Constructors
        public TestSequence()
        {
            dictionaryHeader = new Dictionary<string, string>();
            ArrayListCols = new ArrayList();
            ArrayListTests = new ArrayList();
            FileName = "";
            NewFile = false;
            Modified = false;
        }
        #endregion Constructors

        #region R/W Methods
        /// <summary>
        /// Read in tests with previously used filename.
        /// </summary>
        public void ReadInTests()
        {
            ReadInTests(FileName);
        }

        /// <summary>
        ///Read in tests with given filename.
        /// </summary>
        /// <param name="TestDefFile"></param>
        public void ReadInTests(string TestDefFile)
        {
            FileStream fs = null;
            try
            {
                FileName = TestDefFile;

                fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                ReadInTests(fs, false);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Read in tests with Stream.
        /// </summary>
        /// <param name="TestDef"></param>
        /// <param name="bRepair"></param>
        public void ReadInTests(Stream TestDef, bool bRepair)
        {
            try
            {
                TestDef.Seek(0, SeekOrigin.Begin);
                ReadAndVerifyFile(TestDef);
            }
            catch (Exception e)
            {
                if (bRepair)
                {
                    Modified = true;
                }
                else
                {
                    throw e;
                }
            }
            try
            {
                TestDef.Seek(0, SeekOrigin.Begin);
                ReadAndParseFile(TestDef);
                CheckForValidVersion(bRepair);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Write this test sequence to the previously read filename.
        /// </summary>
        /// <param name="Today"></param>
        public void WriteTests(bool Today)
        {
            WriteTests(this.FileName, Today);
        }

        /// <summary>
        /// Write this test sequence to the passed in filename.
        /// </summary>
        /// <param name="WriteFileName"></param>
        /// <param name="Today"></param>
        public void WriteTests(string WriteFileName, bool Today)
        {
            FileStream fs = null;
            try
            {
                // open up the file stream
                fs = new FileStream(WriteFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                // send stream to the function overload that writes out the sequence
                WriteTests(fs, Today);
                // close and flush
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

        /// <summary>
        /// Write this test sequence to the passed in stream.
        /// </summary>
        /// <param name="XmlStream"></param>
        /// <param name="Today"></param>
        public void WriteTests(Stream XmlStream, bool Today)
        {
            dictionaryHeader["Version"] = this.AddUpVersion();

            StreamWriter sw = null;
            try
            {
                using (sw = new StreamWriter(XmlStream, System.Text.Encoding.Unicode))
                {
                    // Add beginning stuff.
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-16\" ?>");
                    sw.WriteLine("<testSequence Name=\"" + dictionaryHeader["Name"] + "\" Version=\"" + dictionaryHeader["Version"] + "\">");

                    string dateStr;
                    StringBuilder dateStrBuilder = new StringBuilder();
                    if (Today)
                    {
                        dateStrBuilder.Append(DateTime.Today.Year.ToString() + "-");
                        if (DateTime.Today.Month.ToString().Length == 1)
                        {
                            dateStrBuilder.Append("0");
                        }
                        dateStrBuilder.Append(DateTime.Today.Month.ToString() + "-");
                        if (DateTime.Today.Day.ToString().Length == 1)
                        {
                            dateStrBuilder.Append("0");
                        }
                        dateStrBuilder.Append(DateTime.Today.Day.ToString());

                        dateStr = dictionaryHeader["date"] = dateStrBuilder.ToString();
                    }
                    else
                    {
                        dateStr = dictionaryHeader["date"];
                    }
                    sw.WriteLine("   <date type=\"date\">" + dateStr + "</date>");

                    // loop for each test
                    for (int i = 0; i < ArrayListTests.Count; i++)
                    {
                        ATest aTest = (ATest)ArrayListTests[i];

                        sw.WriteLine("   <test Name=\"" + aTest.Name + "\" Image=\"" + aTest.Image + "\" ToolTip=\"" + aTest.ToolTip + "\">");
                        // loop for each subsection
                        for (int j = 0; j < aTest.listOfSubSections.Count; j++)
                        {
                            string TmpKey = aTest.listOfSubSections[j];
                            sw.WriteLine("      <" + TmpKey + ">");

                            // loop for each item in subsection
                            foreach (KeyValuePair<string, string> Entry in aTest.dictionaryOfDefVals[TmpKey])
                            {
                                sw.WriteLine("         <" + Entry.Key + ">" + Entry.Value + "</" + Entry.Key + ">");
                            }
                            sw.WriteLine("      </" + TmpKey + ">");
                        } // end for j subsections
                        sw.WriteLine("   </test>");
                    } // end for i each test

                    sw.WriteLine("</testSequence>");
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                    sw = null;
                } // end using
                Modified = false;
                NewFile = false;
            }
            catch { }
            finally
            {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
        }

        /// <summary>
        /// Calculate CRC32 for data portion of file.
        /// </summary>
        /// <returns>CRC32 string.</returns>
        public string AddUpVersion()
        {
            MemoryStream seqStream = new MemoryStream();
            byte[] data = Encoding.Unicode.GetBytes("hello"); // Salt
            seqStream.Write(data, 0, data.Length);

            for (int i = 0; i < ArrayListTests.Count; i++)
            {
                ATest aTest = (ATest)ArrayListTests[i];
                byte[] data1 = Encoding.Unicode.GetBytes(
                   this.ArrayListCols[0].ToString().Trim() + aTest.Name.Trim() +
                   this.ArrayListCols[1].ToString().Trim() + aTest.Image.Trim() +
                   this.ArrayListCols[2].ToString().Trim() + aTest.ToolTip.Trim()
                );
                seqStream.Write(data1, 0, data1.Length);

                // loop for each subsection
                for (int j = 0; j < aTest.listOfSubSections.Count; j++)
                {
                    string TmpKey = aTest.listOfSubSections[j];

                    // loop for each item in subsection
                    foreach (KeyValuePair<string, string> Entry in aTest.dictionaryOfDefVals[TmpKey])
                    {
                        byte[] data2 = Encoding.Unicode.GetBytes(
                           Entry.Key.Trim() + Entry.Value.Trim()
                        );
                        seqStream.Write(data2, 0, data2.Length);
                    }
                }
            }
            seqStream.Seek(0, SeekOrigin.Begin);
            StreamCrcClass streamCrcClass = new StreamCrcClass();
            string theCRC = streamCrcClass.GetCrcString(seqStream);
            seqStream.Close();
            seqStream.Dispose();
            return theCRC;
        }
        #endregion Methods

        #region internal Methods
        /// <summary>
        /// Checks if the Version(CRC code) is OK.
        /// Throws Exception if not OK.  Returns if OK.
        /// </summary>
        /// <param name="bRepair"></param>
        private void CheckForValidVersion(bool bRepair)
        {
            if (bRepair)
            {
                Modified = true;
                return;
            }

            if (dictionaryHeader["Version"] != this.AddUpVersion())
            {
                throw new Exception("Bad test sequence file.  Invalid version number.");
            }
        }

        /// <summary>
        /// Verifies if this is a valid configuration file.  It uses "TestSequence.xsd" to verify the file using
        /// .NET's schema checking classes.
        /// </summary>
        /// <param name="fs"></param>
        private void ReadAndVerifyFile(Stream fs)
        {
            string xsdFile = System.IO.Path.Combine(Application.StartupPath, Constants.TestSequenceXsd);

            // Create the XmlSchemaSet class.
            XmlSchemaSet sc = new XmlSchemaSet();
            // add out schema
            sc.Add(null, xsdFile);

            // Set the validation settings.
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = sc;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationEventHandler);

            // Create the XmlReader object.
            XmlReader reader = XmlReader.Create(fs, settings);

            // Parse the file. 
            while (reader.Read()) { };

        } // end ReadAndVerifyFile

        /// <summary>
        /// This is the callback func for ReadAndVerifyFile.  If the schema checker finds a problem, then this is called.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
        {
            throw new Exception("Bad test sequence file.  Wrong format. Please select another file.  ", e.Exception);
        }

        private void ReadAndParseFile(Stream fs)
        {
            try
            {
                //dictionaryTestImages.Clear();
                // dictionaryToolTips.Clear(); ;
                dictionaryHeader.Clear();
                ArrayListCols.Clear();
                ArrayListTests.Clear();

                // we parse all of the xml stuff with this
                XmlDocument xmlTestDoc = new XmlDocument();
                // load tmp file
                xmlTestDoc.Load(fs);

                // get Header stuff (name, version)
                for (int i = 0; i < xmlTestDoc.ChildNodes[1].Attributes.Count; i++)
                {
                    dictionaryHeader.Add(
                       xmlTestDoc.ChildNodes[1].Attributes[i].Name,
                       xmlTestDoc.ChildNodes[1].Attributes[i].Value
                    );
                }

                // get date
                dictionaryHeader.Add(
                   xmlTestDoc.ChildNodes[1].ChildNodes[0].Name,
                   xmlTestDoc.ChildNodes[1].ChildNodes[0].InnerText
                );

                // fill image and Tests structures
                //  local test struct
                ATest aTest;

                // loop once for each test (we start at one because the first element is the date)
                for (int i = 1; i < xmlTestDoc.ChildNodes[1].ChildNodes.Count; i++)
                {
                    // one test struct
                    aTest = new ATest();
                    // get the test name
                    aTest.Name = xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[0].Value;
                    // get the test image
                    aTest.Image = xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[1].Value;
                    // get the toolTip
                    aTest.ToolTip = xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[2].Value;

                    // loop once for each subtype (testState, testFlags, ...)
                    for (int j = 0; j < xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes.Count; j++)
                    {
                        aTest.listOfSubSections.Add(xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes[j].Name);
                        Dictionary<string, string> tmpDir = new Dictionary<string, string>();
                        // loop once for each item is the subtypes (these are the ListView cols)
                        for (int k = 0; k < xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes[j].ChildNodes.Count; k++)
                        {
                            tmpDir.Add(xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes[j].ChildNodes[k].Name,
                               xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes[j].ChildNodes[k].InnerText);

                            int l;
                            for (l = 0; l < ArrayListCols.Count; l++)
                            {
                                // see if this one already is there
                                if ((string)(ArrayListCols[l]) == xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes[j].ChildNodes[k].Name)
                                {
                                    break;
                                }
                            } // end for l (each item in the global list)
                            if (l >= ArrayListCols.Count) // then the for did not find it
                            {
                                // so we add it
                                ArrayListCols.Add(xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes[j].ChildNodes[k].Name);
                            }
                        } // end for k (each item in each subtype)
                        // add to the Columns dir for this test [i]
                        aTest.dictionaryOfDefVals.Add(
                           xmlTestDoc.ChildNodes[1].ChildNodes[i].ChildNodes[j].Name,
                           tmpDir
                        );

                    } // end for j (each subitem)

                    // put this test in the list
                    ArrayListTests.Add(aTest);

                    //// put this image in the list
                    //if(!dictionaryTestImages.ContainsKey(xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[0].Value))
                    //{
                    //   dictionaryTestImages.Add(
                    //      xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[0].Value,
                    //      xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[1].Value
                    //   );
                    //}

                    //// put this tool tip in the list
                    //if (!dictionaryToolTips.ContainsKey(xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[0].Value))
                    //{
                    //   dictionaryToolTips.Add(
                    //      xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[0].Value,
                    //      xmlTestDoc.ChildNodes[1].ChildNodes[i].Attributes[2].Value
                    //   );
                    //}
                } // end for i (each test)
            }
            catch (Exception e)
            {
                throw e;
            }
        } // end read and parse
        #endregion internal Methods
    } // end class
} // end namespace
