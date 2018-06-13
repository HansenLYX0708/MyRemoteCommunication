using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hitachi.Tester.Module
{
    public struct BladeEventStruct
    {
        public BladeEventStruct(object sender, BladeEventArgs e)
        {
            Sender = sender;
            EE = e;
        }

        public object Sender;
        public BladeEventArgs EE;
    }
}
