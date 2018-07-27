// ==========================================================================================
// Copyright ©                                                       
//                                                                                          
// Classification           :                  
// Date                     :                                               
// Author                   : Hansen Liu                                             
// Purpose                  : 
// ==========================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD.Tester.Module
{
    /// <summary>
    /// The args for callback in status event.
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        #region Fields
        private string _Text;
        private int _EventType;
        private int _ConsecutiveCount;
        #endregion Fields

        #region Constructors
        public StatusEventArgs(string strText, int iType, int iConsecutive) : this(strText, iType)
        {
            _Text = strText;
            _EventType = iType;
            _ConsecutiveCount = iConsecutive;
        }

        public StatusEventArgs(string strText, int iType) : this(strText)
        {
            _Text = strText;
            _EventType = iType;
        }

        public StatusEventArgs(string strText) : base()
        {
            _Text = strText;
        }

        public StatusEventArgs()
        {
            _Text = string.Empty;
            _EventType = 0;
            _ConsecutiveCount = -1;
        }
        #endregion Constructors

        #region Properties
        public string Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        public int EventType
        {
            get { return _EventType; }
            set { _EventType = value; }
        }

        public int ConsecutiveCount
        {
            get { return _ConsecutiveCount; }
            set { _ConsecutiveCount = value; }
        }
        #endregion Properties
    }
}
