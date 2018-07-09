/***********************************************************************
 * Passing value for write stream request from client to host.
 * Client sends a read stream with this and a filename.  Host reads from 
 * this remote stream and writes to the local computer with filename given.
 * The Host uses the filename (with local path) to open a write stream
 * on host computer.
 * 
 * Robert L. Kimball
 * 
 * Copyright August 2012 HGST
 * 
 * *********************************************************************/
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace Hitachi.Tester.Module
{
    [MessageContract]
    public class FileStreamRequest 
    {
        [MessageHeader]
        public string FileName;

        [MessageBodyMember]
        public Stream FileByteStream;
    }

} // end namespace
