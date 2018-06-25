// ****************************************************************
// CompletedEventArgs.cs 
// This is the argunment struc that is passed with the test completed event.
//
// Hitachi Parametrics tester
// Copyright 2008
// Robert L. Kimball  March 29, 2008
// ****************************************************************
using System;

namespace Hitachi.Tester.Module
{
	/// <summary>
	/// Summary description for CompletedEventArgs.
	/// This EventArgs type returns a string and two integers.
	/// </summary>
	[Serializable]
   public class CompletedEventArgs : EventArgs
	{
      /// <summary>
      /// Constructor with parameters.
      /// </summary>
      /// <param name="strText"></param>
      /// <param name="iTestNum"></param>
      /// <param name="iTestCount"></param>
      public CompletedEventArgs(string strText, int iTestNum, int iTestCount, int iConsecutiveCount, bool bFail)
		{
         Text = strText;
         testNum = iTestNum;
         testCount = iTestCount;
         ConsecutiveCount = iConsecutiveCount;
         fail = bFail;
		}
      /// <summary>
      /// Default constructor.
      /// </summary>
      public CompletedEventArgs()
      {
         Text = "";
         testNum = 0;
         testCount = 0;
         ConsecutiveCount = 0;
         fail = false;
      }

      public string Text;
      public int testNum;
      public int testCount;
      public int ConsecutiveCount;
      public bool fail;


   } // end class
} // end namespace


