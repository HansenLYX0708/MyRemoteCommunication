// ************************************************************************************
//
// Copyright 2008 Hitachi Global Storage Technologies, Inc.
//
// This code is proprietary to Hitachi Global Storage Technologies and cannot be used, 
//  modified, or copied except with the express permission of Hitachi Global Storage 
//  Technologies.
// Robert L. Kimball July 8, 2008
// ************************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Hitachi.Tester.IJadeCommonTables
{
    /// <summary>
    /// This is what is called the Grade Specification.
    /// There is an instance of this class for each measurement.
    /// This class contains the upper and lower limits for each rank.
    /// Rank is like a pre-grade.
    /// </summary>
   [Serializable] 
   public class GradeTestObject //: ISerializable
   {
       /// <summary>
       /// Default constructor.
       /// </summary>
      public GradeTestObject()
      {
         image = "";
         comment= "";
         name = "";
         function = "";
         units = "";
         unitType = "";
         useIt = "";
         tableNo = "";
         gradeNamesList = new List<string>();
         upperLimitList = new List<string>();
         lowerLimitList = new List<string>();
         rankFunctionList = new List<string>();
      }

      public string image;
      public string comment;
      public string name;
      public string function;
      public string units;
      public string unitType;
      public string useIt;
      public string tableNo;
      public List<string> gradeNamesList;
      public List<string> upperLimitList;
      public List<string> lowerLimitList;
      public List<string> rankFunctionList;


      /// <summary>
      /// Returns a deep copy of this.
      /// </summary>
      /// <returns></returns>
      public GradeTestObject Clone()
      {
         GradeTestObject aClone = new GradeTestObject();
         try
         {
            aClone.comment = this.comment;
            aClone.function = this.function;
            aClone.image = this.image;
            aClone.name = this.name;
            aClone.units = this.units;
            aClone.unitType = this.unitType;
            aClone.useIt = this.useIt;
            aClone.tableNo = this.tableNo;

            for (int i = 0; i < this.gradeNamesList.Count; i++)
            {
               aClone.gradeNamesList.Add(this.gradeNamesList[i]);
            }
            for (int i = 0; i < this.lowerLimitList.Count; i++)
            {
               aClone.lowerLimitList.Add(this.lowerLimitList[i]);
            }
            for (int i = 0; i < this.upperLimitList.Count; i++)
            {
               aClone.upperLimitList.Add(this.upperLimitList[i]);
            }
            for (int i = 0; i < this.rankFunctionList.Count; i++)
            {
               aClone.rankFunctionList.Add(this.rankFunctionList[i]);
            }
         }
         catch
         { }
         return aClone;
      }

       /// <summary>
       /// Public property returns count of ranks.
       /// </summary>
      public int Count
      {
          get
          {
              int count = gradeNamesList.Count;
              if (upperLimitList.Count < count) count = upperLimitList.Count;
              if (lowerLimitList.Count < count) count = lowerLimitList.Count;
              if (rankFunctionList.Count < count) count = rankFunctionList.Count;
              return count;
          }
      }

      //public GradeTestObject(SerializationInfo info, StreamingContext context)
      //{
      //   comment = info.GetString("comment");
      //   function = info.GetString("function");
      //   image = info.GetString("image");
      //   name = info.GetString("name");
      //   units = info.GetString("units");
      //   unitType = info.GetString("unitType");
      //   useIt = info.GetString("useIt");
      //   tableNo = info.GetString("tableNo");

      //   int count = info.GetInt32("gradeNamesListCount");
      //   for (int i = 0; i < count; i++)
      //   {
      //      this.gradeNamesList.Add(info.GetString("gnl" + i.ToString()));
      //   }

      //   count = info.GetInt32("lowerLimitListCount");
      //   for (int i = 0; i < count; i++)
      //   {
      //      this.lowerLimitList.Add(info.GetString("lll" + i.ToString()));
      //   }

      //   count = info.GetInt32("upperLimitListCount");
      //   for (int i = 0; i < count; i++)
      //   {
      //      this.upperLimitList.Add(info.GetString("ull" + i.ToString()));
      //   }

      //   count = info.GetInt32("rankFunctionListCount");
      //   for (int i = 0; i < count; i++)
      //   {
      //      this.rankFunctionList.Add(info.GetString("rfl" + i.ToString()));
      //   }

      //}

      //public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
      //{
      //   info.AddValue("comment", comment);
      //   info.AddValue("function", function);
      //   info.AddValue("image", image);
      //   info.AddValue("name", name);
      //   info.AddValue("units", units);
      //   info.AddValue("unitType", unitType);
      //   info.AddValue("useIt", useIt);
      //   info.AddValue("tableNo", tableNo);

      //   info.AddValue("gradeNamesListCount", gradeNamesList.Count);
      //   for (int i = 0; i < this.gradeNamesList.Count; i++)
      //   {
      //      info.AddValue("gnl" + i.ToString(), this.gradeNamesList[i]);
      //   }

      //   info.AddValue("lowerLimitListCount", lowerLimitList.Count);
      //   for (int i = 0; i < this.lowerLimitList.Count; i++)
      //   {
      //      info.AddValue("lll" + i.ToString(), this.lowerLimitList[i]);
      //   }

      //   info.AddValue("upperLimitListCount", upperLimitList.Count);
      //   for (int i = 0; i < this.upperLimitList.Count; i++)
      //   {
      //      info.AddValue("ull" + i.ToString(), this.upperLimitList[i]);
      //   }

      //   info.AddValue("rankFunctionListCount", rankFunctionList.Count);
      //   for (int i = 0; i < this.rankFunctionList.Count; i++)
      //   {
      //      info.AddValue("rfl" + i.ToString(), this.rankFunctionList[i]);
      //   }
      //}

      /// <summary>
      /// Kind a like a limited SQL SELECT statement for this "table." 
      /// For Where Use C# logic symbols  
      /// AND is &&; OR is ||; Equal is ==
      /// Greater than less than use C# symbols. 
      /// Colunms are defined by a combination of Hitachi.Tester.Enumns.GradeTestObjectRankEnum
      ///  and Hitachi.Tester.Enums.GradeTestObjectCommonEnum. Use ToString() for correct spelling.
      /// 
      /// This function returns data from tables.
      /// </summary>
      /// <param name="columns">Comma seperated list of columns to return</param>
      /// <param name="where">String of conditions</param>
      /// If you ask for something that does not exist it does not error out or pop exception;
      ///  it instead returns an empty string.
      ///
      /// <returns>Dictionary of Data you requested</returns>
      public Dictionary<string, string> SELECT(string columns, string where)
      {
          // List of columns that we will try to return
          string[] columnTokens = columns.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
          // dictionary for our result (if any).
          Dictionary<string, string> result = new Dictionary<string, string>();
          // dictionary for this instance's columns.
          Dictionary<string, string> thisData = new Dictionary<string, string>();


          //public enum GradeTestObjectRankEnum
          //public enum GradeTestObjectCommonEnum


          // Put this (row) into a dictionary of columns
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.NAME.ToString(), name);
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.IMAGE.ToString(), image);
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.COMMENT.ToString(), comment);
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.FUNCTION.ToString(), function);
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.UNITS.ToString(), units);
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.TYPES.ToString(), unitType);
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.USE.ToString(), useIt);
          thisData.Add(Hitachi.Tester.Enums.GradeTestObjectCommonEnum.TABLENO.ToString(), tableNo);

          // Get the rank count
          int count = Count;

          // Loop for each rank.
          for(int i = 0; i < count; i++)
          {
              thisData.Add(Hitachi.Tester.Enums.GradeTestObjectRankEnum.RANKNAME.ToString() + (i + 1).ToString(), gradeNamesList[i]);
              thisData.Add(Hitachi.Tester.Enums.GradeTestObjectRankEnum.UPPER.ToString() + (i + 1).ToString(), upperLimitList[i]);
              thisData.Add(Hitachi.Tester.Enums.GradeTestObjectRankEnum.LOWER.ToString() + (i + 1).ToString(), lowerLimitList[i]);
              thisData.Add(Hitachi.Tester.Enums.GradeTestObjectRankEnum.FUNCTION.ToString() + (i + 1).ToString(), rankFunctionList[i]);
          }

          ConditionParser cp = new ConditionParser();
          // See if the conditions match
          if (cp.IsOk(where, thisData))
          {
              // Put requested column data in the result dictionary.
              foreach (string col in columnTokens)
              {
                  try
                  {
                      result.Add(col.Trim(), thisData[col.Trim()]);
                  }
                  catch { }
              }
          }
          return result;
      }


   
   
   } // end class
} // end namespace
