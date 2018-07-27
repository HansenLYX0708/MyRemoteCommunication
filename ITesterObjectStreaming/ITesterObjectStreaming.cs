/*****************************************************************************
 * Stream function definition for WCF stream service.
 * 
 * Robert L. Kimball
 * Copyright HGST August, 2012
 * 
 * ***************************************************************************/
using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace WD.Tester.Module
{
    [ServiceContract(Name = "TesterObjectStreamingContract", Namespace = "Hitachi.Tester.Module")]
    public interface ITesterObjectStreaming
    {
        [OperationContract]
        Stream BladeFileRead(string fileRequest);

        [OperationContract]
        FileStreamResponse BladeFileWrite(FileStreamRequest fileRequest);

        event ObjObjDelegate SendToTesterObjectEvent;

    } // end interface

} // end namespace
