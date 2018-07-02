// ************************************************************************************
//
// Copyright 2008 Hitachi Global Storage Technologies, Inc.
//
// This code is proprietary to Hitachi Global Storage Technologies and cannot be used, 
//  modified, or copied except with the express permission of Hitachi Global Storage 
//  Technologies.
// Robert L. Kimball Aug 20, 2008
// ************************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace Hitachi.Tester.Module
{
    [Serializable]
    public class FileNameStruct
    {
        public FileNameStruct()
        {
            FileNameStr = "";
            VersionStr = "";
            DateStr = "";
            BinVerStr = "";
            SkipVerStr = "";
            DispVerStr = ""; ;
            GradeVerStr = "";
            OcrVerStr = "";
            TestCountVerStr = "";
            TrayDispoVerStr = "";
            RetryDispoVerStr = "";
         }

        public string FileNameStr;
        public string VersionStr;
        public string DateStr;
        public string BinVerStr;
        public string DispVerStr;
        public string SkipVerStr;
        public string GradeVerStr;
        public string OcrVerStr;
        public string TestCountVerStr;
        public string TrayDispoVerStr;
        public string RetryDispoVerStr;
     }
}
