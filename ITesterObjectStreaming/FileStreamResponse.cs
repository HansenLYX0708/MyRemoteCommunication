/******************************************************************
 * Response type for return value of write stream requests from client to host.
 * Client sends a read stream in (FileStreamRequest type) and this
 * responds withe the final filename and path on the remote computer.
 * 
 * Robert L. Kimball
 * Copyright August 2012 HGST
 * 
 * ****************************************************************/
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace Hitachi.Tester.Module
{
    /// <summary>
    /// Required to make WCF happy.
    /// This is the return value or whatever function uses it.
    /// </summary>
    [MessageContract]
    public class FileStreamResponse
    {
        /// <summary>
        /// This is the return value of the functions that use this.
        /// The value is not always a FileName.  Any string OK.
        /// </summary>
        [MessageHeader]
        public string FileName;

    } // end class

} // end namespace
