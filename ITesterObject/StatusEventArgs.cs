using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hitachi.Tester.Module
{
    public class StatusEventArgs : EventArgs
    {
        #region Fields
        public string Text;
        public int EventType;
        public int ConsecutiveCount;
        #endregion Fields

        #region Constructors
        public StatusEventArgs(string strText, int iType, int iConsecutive) : this(strText, iType)
        {
            Text = strText;
            EventType = iType;
            ConsecutiveCount = iConsecutive;
        }

        public StatusEventArgs(string strText, int iType) : this(strText)
        {
            Text = strText;
            EventType = iType;
        }

        public StatusEventArgs(string strText) : base()
        {
            Text = strText;
        }

        public StatusEventArgs()
        {
            Text = "";
            EventType = 0;
            ConsecutiveCount = -1;
        }
        #endregion Constructors
    }
}
