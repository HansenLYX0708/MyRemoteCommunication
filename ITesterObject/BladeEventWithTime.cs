using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hitachi.Tester.Module
{
    public class BladeEventWithTime
    {
        public DateTime ArrivalTime;
        public BladeEventArgs EventArgs;
        public object Sender;

        public BladeEventWithTime()
        {
            ArrivalTime = DateTime.Now;
            EventArgs = new BladeEventArgs();
            Sender = new object();
        }

        public BladeEventWithTime(DateTime arivalTime, object sender, BladeEventArgs e)
        {
            ArrivalTime = DateTime.Now;
            EventArgs = e;
            Sender = sender;
        }

    }
}
