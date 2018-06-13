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

namespace Hitachi.Tester.Module
{
    public class BladeEventWithTime
    {
        #region Fields
        private DateTime _ArrivalTime;
        private BladeEventArgs _EventArgs;
        private object _Sender;
        #endregion Fields

        #region Constructors
        public BladeEventWithTime()
        {
            _ArrivalTime = DateTime.Now;
            _EventArgs = new BladeEventArgs();
            _Sender = new object();
        }

        public BladeEventWithTime(DateTime arivalTime, object sender, BladeEventArgs e)
        {
            _ArrivalTime = arivalTime;
            _EventArgs = e;
            _Sender = sender;
        }
        #endregion Constructors

        #region Properties
        public DateTime ArrivalTime
        {
            get { return _ArrivalTime; }
            set { _ArrivalTime = value; }
        }

        public BladeEventArgs EventArgs
        {
            get { return _EventArgs; }
            set { _EventArgs = value; }
        }
        public object Sender
        {
            get { return _Sender; }
            set { _Sender = value; }
        }
        #endregion Properties
    }
}
