using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

namespace Hitachi.Tester.Module
{
    public interface ITesterObjectCallback
    {
        [OperationContract(IsOneWay = true)]
        void BladeEventCallback(BladeEventArgs e);
    }
}
