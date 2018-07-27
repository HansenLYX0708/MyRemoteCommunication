// ************************************************************************************
// This struct holds one test.  Many of these make up a sequence.
//
// Copyright 2007 Hitachi Global Storage Technologies, Inc.
//
// This code is proprietary to Hitachi Global Storage Technologies and cannot be used, 
//  modified, or copied except with the express permission of Hitachi Global Storage 
//  Technologies.
// Robert L. Kimball March 1, 2007
// ************************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace WD.Tester.Sequence
{
    /// <summary>
    /// This class is a struct that holds one test.
    /// </summary>
    [Serializable]
    public class ATest
    {
        #region Fields
        public Dictionary<string, Dictionary<string, string>> dictionaryOfDefVals;
        public List<string> listOfSubSections;
        public string Name;
        public string Image;
        public string ToolTip;
        #endregion Fields

        #region Constructors
        public ATest()
        {
            dictionaryOfDefVals = new Dictionary<string, Dictionary<string, string>>();
            listOfSubSections = new List<string>();
            Name = "";
            Image = "";
            ToolTip = "";
        }
        #endregion Constructors

        #region Methods
        /// <summary>
        /// Returns a deep copy of this.
        /// </summary>
        /// <returns></returns>
        public ATest Clone()
        {
            ATest aClone = new ATest();
            try
            {
                aClone.Image = this.Image;
                aClone.Name = this.Name;
                aClone.ToolTip = this.ToolTip;

                for (int i = 0; i < this.listOfSubSections.Count; i++)
                {
                    aClone.listOfSubSections.Add(this.listOfSubSections[i]);
                }

                foreach (string topTey in this.dictionaryOfDefVals.Keys)
                {
                    Dictionary<string, string> dct = new Dictionary<string, string>();
                    foreach (string subKey in this.dictionaryOfDefVals[topTey].Keys)
                    {
                        dct.Add(subKey, this.dictionaryOfDefVals[topTey][subKey]);
                    }
                    aClone.dictionaryOfDefVals.Add(topTey, dct);
                }
            }
            catch
            { }
            return aClone;
        }

        /// <summary>
        /// Deep copies aTest to us.
        /// </summary>
        /// <param name="aTest"></param>
        public void Assign(ATest aTest)
        {
            if (aTest == null) aTest = new ATest();

            this.Image = aTest.Image;
            this.Name = aTest.Name;
            this.ToolTip = aTest.ToolTip;

            this.listOfSubSections.Clear();
            foreach (string str in aTest.listOfSubSections)
            {
                this.listOfSubSections.Add(str);
            }
            this.dictionaryOfDefVals.Clear();
            foreach (string topTey in aTest.dictionaryOfDefVals.Keys)
            {
                Dictionary<string, string> dct = new Dictionary<string, string>();
                foreach (string subKey in aTest.dictionaryOfDefVals[topTey].Keys)
                {
                    dct.Add(subKey, aTest.dictionaryOfDefVals[topTey][subKey]);
                }
                this.dictionaryOfDefVals.Add(topTey, dct);
            }
        } // end assign
        #endregion Methods
    }
} // end namespace
