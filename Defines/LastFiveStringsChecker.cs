using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace HGST.Defines
{
    public class LastSeveralStringsChecker
    {
        private LastSeveralStringsChecker()
        {
        }

        public LastSeveralStringsChecker(int Count)
        {
            whichOne = 0;
            count = Count;
            readerWriterLock = new ReaderWriterLock();
            lastFiveMessages = new List<string>();
            for (int i = 0; i < count; i++)
            {
                lastFiveMessages.Add("");
            }
        }

        private ReaderWriterLock readerWriterLock;
        int whichOne;
        int count;
        List<string> lastFiveMessages;

        public void InsertMessage(string message)
        {
            try
            {
                readerWriterLock.AcquireWriterLock(Timeout.Infinite);
                lastFiveMessages[whichOne] = message;
                whichOne++;
                if (whichOne >= count) whichOne = 0;
            }
            finally
            {
                readerWriterLock.ReleaseWriterLock();
            }
            
        }

        public bool IsThisStringThereSomewhere(string searchString)
        {
            bool answer = false;
            try
            {
                readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                foreach (string aStr in lastFiveMessages)
                {
                    if (aStr.Contains(searchString))
                    {
                        answer = true;
                        break;
                    }
                }
                return answer;
            }
            finally
            {
                readerWriterLock.ReleaseReaderLock();
            }
        }
    }
}
