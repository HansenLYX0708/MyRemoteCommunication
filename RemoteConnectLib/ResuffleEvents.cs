// ----------------------------------------------------------------------------------------
// Author:                    RKimb702174
// Company:                   
// Assembly version:          
// Date:                      01/05/2013
// Time:                      15:58
// Solution Name:             JadeMaster
// Solution Filename:         JadeMaster.sln
// Solution FullFilename:     C:\Tester\BunnyTestBoard\JadeMaster.sln
// Project Name:              RemoteConnectLib
// Project Filename:          RemoteConnectLib.csproj
// Project FullFilename:      C:\Tester\BunnyTestBoard\RemoteConnectLib\RemoteConnectLib.csproj
// Project Item Name:         ResuffleEvents
// Project Item Filename:     ResuffleEvents.cs
// Project Item FullFilename: C:\Tester\BunnyTestBoard\RemoteConnectLib\ResuffleEvents.cs
// Project Item Kind:         Code
// Purpose:                   Execute async events in proper order.
//     Sequence status events are sent asynchronously from the BladeRunner in unknown order.  This reorders the events and
//     sends them out in the proper order.
// ----------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Diagnostics;

using WD.Tester.Module;

namespace WD.Tester.Client
{
    /// <summary>
    /// Sequence status events are sent asynchronously from the BladeRunner in unknown order.  This reorders the events and
    ///  sends them out in the proper order.
    /// </summary>
    public class ResuffleEvents
    {
        #region Fields
        private readonly NLog.Logger ShuffleEventLog = NLog.LogManager.GetLogger("ShuffleEventLog");
        private int _SequenceCount;
        /// <summary>
        /// Which operation for the bottleneck functions.
        /// </summary>
        private enum operationEnum
        {
            ADD,
            REMOVE,
            QUERY,
            FLUSH,
            FLUSHOLD
        }

        private volatile bool sequenceAborted;
        private volatile List<TestStartedEventStruct> testStartedEvents;
        private volatile List<TestCompletedEventStruct> testCompletedEvents;
        private volatile SequenceStartedEventStruct sequenceStartedEvent;
        private volatile SequenceCompleteStruct squenceCompleteEvent;
        private volatile int nextItemToProcess;
        private volatile int testCount;
        private System.Timers.Timer processItemsTimer;
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public ResuffleEvents()
        {
            _SequenceCount = 0;
            processItemsTimer = new System.Timers.Timer(25);
            processItemsTimer.AutoReset = false;
            processItemsTimer.Elapsed += new ElapsedEventHandler(processItemsTimer_Elapsed);
            processItemsTimer.Start();

            sequenceAborted = false;
            testStartedEvents = new List<TestStartedEventStruct>();
            testCompletedEvents = new List<TestCompletedEventStruct>();
            sequenceStartedEvent = null;
            squenceCompleteEvent = null;
            nextItemToProcess = -1;
            testCount = -1;
        }

        /// <summary>
        /// Just to make sure.  Likely no need.
        /// </summary>
        ~ResuffleEvents()
        {
            try
            {
                processItemsTimer.Dispose();
            }
            catch { }
        }
        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Puts an async SequenceStartedEvent into our class for prossesing and immediately sends out
        /// this event.  so, we do not really queue it up; this is always the first one that goes out.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void QueueUpSequenceStartedEvent(object sender, StartedEventArgs e)
        {
            ShuffleEventLog.Info("Queue SeqStart event e.con:{0} e.seqName:{1} SeqCount:{2} nextToProcess:{3} StartSeq:{4} StopSeq:{5}",
                e.iConsecutive, e.seqName, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);

            sequenceAborted = false;
            nextItemToProcess = e.iStartTest; // This is the first one in the sequence to execute.
            testCount = -1;  // We do not know how many tests there are yet.
                             // TestCompleteEvents give us this.
            _SequenceCount++;  // Start sequence so inc to new one.
            sequenceStartedEvent = new SequenceStartedEventStruct(sender, e, _SequenceCount); // remember this one.

            // Send it out.
            sendSequenceStartedEvent(sender, e);
        }

        /// <summary>
        /// Puts an async SequenceCompleteEvent into our class for processing.
        /// We will send it out later.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void QueueUpSequenceCompletedEvent(object sender, StatusEventArgs e)
        {
            if (squenceCompleteEvent != null)
            {
                // Send any old Test Start/Stop ones.
                testStartBottleneckFunction(operationEnum.FLUSHOLD, _SequenceCount, null, null);
                testCompleteBottleneckFunction(operationEnum.FLUSHOLD, _SequenceCount, null, null);

                // Send old Sequence complete.
                if (squenceCompleteEvent != null && squenceCompleteEvent.SequenceCount < _SequenceCount)
                {
                    ShuffleEventLog.Info("Send old SeqComplete e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}", squenceCompleteEvent.SequenceCompleteArg.ConsecutiveCount,
                        squenceCompleteEvent.SequenceCompleteArg.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);

                    sequenceStartedEvent = null;
                    testCount = -1;
                    nextItemToProcess = -1;
                    sequenceAborted = false;
                    sendSequenceCompletedEvent(squenceCompleteEvent.Sender, squenceCompleteEvent.SequenceCompleteArg);
                    squenceCompleteEvent = null;
                }
            }

            ShuffleEventLog.Info("Queue NEW SeqComplete e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                e.ConsecutiveCount, e.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
            squenceCompleteEvent = new SequenceCompleteStruct(sender, e, _SequenceCount); // remember this one.
        }

        /// <summary>
        /// Puts an async TestStartedEvent into out class for processing.
        /// We will send it out later.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void QueueUpTestStartedEvent(object sender, StatusEventArgs e)
        {
            ShuffleEventLog.Info("Queue TestStart e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                e.ConsecutiveCount, e.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);

            // Secret: e.eventType * 2 is the index in execution order.

            //Add to queue
            testStartBottleneckFunction(operationEnum.ADD, e.EventType * 2, sender, e);         
        }

        /// <summary>
        /// Puts an async TestCompleteEvent into our class for processing.
        /// We will send it out later.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void QueueUpTestCompletedEvent(object sender, CompletedEventArgs e)
        {
            ShuffleEventLog.Info("Queue TestComplete e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                e.ConsecutiveCount, e.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
            //Secret: e.testNum * 2 + 1 is the index in execution order.

            // Put it in the queue
            testCompleteBottleneckFunction(operationEnum.ADD, (e.testNum * 2) + 1, sender, e);
            // This is the index of the last test complete.
            testCount = (e.testCount * 2) + 1;
        }

        /// <summary>
        /// Puts an async SequenceAbortingEvent into our class for processing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void QueueUpSequenceAbortingEvent(object sender, StatusEventArgs e)
        {
            try
            {
                ShuffleEventLog.Info("Queue Abort e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                    e.ConsecutiveCount, e.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
                processItemsTimer.Stop();

                sequenceAborted = true;
                testStartBottleneckFunction(operationEnum.FLUSH, 0, null, null);
                testCompleteBottleneckFunction(operationEnum.FLUSH, 0, null, null);
                sendSequenceAbortEvent(sender, e);
            }
            finally
            {
                processItemsTimer.Start();
            }
        }

        #endregion Public Methods

        #region Public Events
        /// <summary>
        /// The status handle represents the start of the test event.
        /// </summary>
        public event StatusEventHandler TestStartedEvent;

        /// <summary>
        /// The status handle represents the sequence completed event
        /// </summary>
        public event StatusEventHandler SequenceCompletedEvent;

        /// <summary>
        /// The status handle represents the sequence started event
        /// </summary>
        public event StartedEventHandler SequenceStartedEvent;

        /// <summary>
        /// The status handle represents the test completed event
        /// </summary>
        public event CompleteEventHandler TestCompletedEvent;

        /// <summary>
        /// The status handle represents the sequence abort event
        /// </summary>
        public event StatusEventHandler SequenceAbortEvent;

        private void sendSequenceStartedEvent(object sender, StartedEventArgs e)
        {
            ShuffleEventLog.Info("Send SeqStart event e.con:{0} e.seqName:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                e.iConsecutive, e.seqName, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
            StartedEventHandler handler = SequenceStartedEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void sendSequenceCompletedEvent(object sender, StatusEventArgs e)
        {
            ShuffleEventLog.Info("Send SeqComplete e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                squenceCompleteEvent.SequenceCompleteArg.ConsecutiveCount, squenceCompleteEvent.SequenceCompleteArg.Text, _SequenceCount, 
                nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
            StatusEventHandler handler = SequenceCompletedEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void sendTestStartedEvent(object sender, StatusEventArgs e)
        {
            ShuffleEventLog.Info("Send TestStart e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                e.ConsecutiveCount, e.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
            StatusEventHandler handler = TestStartedEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void sendTestCompletedEvent(object sender, CompletedEventArgs e)
        {
            ShuffleEventLog.Info("Send TestComplete e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}", 
                e.ConsecutiveCount, e.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
            CompleteEventHandler handler = TestCompletedEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void sendSequenceAbortEvent(object sender, StatusEventArgs e)
        {
            ShuffleEventLog.Info("Send SeqAbort e.con:{0} e.Text:{1} SeqCount:{2} nextToProcess:{3}  StartSeq:{4} StopSeq:{5}",
                e.ConsecutiveCount, e.Text, _SequenceCount, nextItemToProcess, sequenceStartedEvent != null, squenceCompleteEvent != null);
            StatusEventHandler handler = SequenceAbortEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }
        #endregion Public Events

        #region privateMethods
        private object testStartBottleNeckObj = new object();

        /// <summary>
        /// Function so that multiple threads can share testStartedEvents. Locks until each call is complete.
        /// </summary>
        /// <param name="what"></param>
        /// <param name="which"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private object testStartBottleneckFunction(operationEnum what, int which, object sender, StatusEventArgs e)
        {
            object retVal = null;
            lock (testStartBottleNeckObj)
            {
                switch (what)
                {
                    case operationEnum.ADD:
                        TestStartedEventStruct newOne = new TestStartedEventStruct(sender, e, _SequenceCount);
                        newOne.Index = which;
                        testStartedEvents.Add(newOne);
                        break;
                    case operationEnum.QUERY:
                        foreach (TestStartedEventStruct anItem in testStartedEvents)
                        {
                            if (anItem.Index == which)
                            {
                                retVal = anItem;
                                break;
                            }
                        }
                        break;
                    case operationEnum.REMOVE:
                        foreach (TestStartedEventStruct anItem in testStartedEvents)
                        {
                            if (anItem.Index == which)
                            {
                                testStartedEvents.Remove(anItem);
                                break;
                            }
                        }
                        break;
                    case operationEnum.FLUSH:
                        while (testCompletedEvents.Count > 0 || testStartedEvents.Count > 0)
                        {
                            if (testStartedEvents.Count > 0)
                            {
                                sendTestStartedEvent(testStartedEvents[0].Sender, testStartedEvents[0].TestStartedArg);
                                testStartedEvents.RemoveAt(0);
                            }
                            if (testCompletedEvents.Count > 0)
                            {
                                sendTestCompletedEvent(testCompletedEvents[0].Sender, testCompletedEvents[0].TestCompleteArg);
                                testCompletedEvents.RemoveAt(0);
                            }
                        }
                        break;
                    case operationEnum.FLUSHOLD:
                        int startOffset = 0;
                        int completeOffset = 0;
                        while (testCompletedEvents.Count - completeOffset > 0 || testStartedEvents.Count - startOffset > 0)
                        {
                            if (testStartedEvents.Count - startOffset > 0)
                            {
                                if (testStartedEvents[startOffset].SequenceCount < which)
                                {
                                    sendTestStartedEvent(testStartedEvents[startOffset].Sender, testStartedEvents[startOffset].TestStartedArg);
                                    testStartedEvents.RemoveAt(startOffset);
                                }
                                else
                                {
                                    startOffset++;
                                }
                            }
                            if (testCompletedEvents.Count - completeOffset > 0)
                            {
                                if (testCompletedEvents[completeOffset].SequenceCount < which)
                                {
                                    sendTestCompletedEvent(testCompletedEvents[completeOffset].Sender, testCompletedEvents[completeOffset].TestCompleteArg);
                                    testCompletedEvents.RemoveAt(completeOffset);
                                }
                                else
                                {
                                    completeOffset++;
                                }
                            }
                        }
                        break;
                } // end switch
            } // end lock

            return retVal;
        }

        private object testcompleteBottleNeckObj = new object();

        /// <summary>
        /// Function so that multiple threads can share testCompletedEvents. Locks until each call is complete.
        /// </summary>
        /// <param name="what"></param>
        /// <param name="which"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private object testCompleteBottleneckFunction(operationEnum what, int which, object sender, CompletedEventArgs e)
        {
            object retVal = null;
            lock (testcompleteBottleNeckObj)
            {
                switch(what)
                {
                    case operationEnum.ADD:
                        TestCompletedEventStruct newOne = new TestCompletedEventStruct(sender, e, _SequenceCount);
                        newOne.Index = which;
                        testCompletedEvents.Add(newOne);
                        break;
                    case operationEnum.QUERY:
                        //Sorry it should have been a dictionary.
                        foreach (TestCompletedEventStruct anItem in testCompletedEvents)
                        {
                            if (anItem.Index == which)
                            {
                                retVal = anItem;
                                break;
                            }
                        }
                        break;
                    case operationEnum.REMOVE:
                        foreach (TestCompletedEventStruct anItem in testCompletedEvents)
                        {
                            if (anItem.Index == which)
                            {
                                testCompletedEvents.Remove(anItem);
                                break;
                            }
                        }
                        break;
                    case operationEnum.FLUSH:
                        while (testCompletedEvents.Count > 0 || testStartedEvents.Count > 0)
                        {
                            if (testStartedEvents.Count > 0)
                            {
                                sendTestStartedEvent(testStartedEvents[0].Sender, testStartedEvents[0].TestStartedArg);
                                testStartedEvents.RemoveAt(0);
                            }
                            if (testCompletedEvents.Count > 0)
                            {
                                sendTestCompletedEvent(testCompletedEvents[0].Sender, testCompletedEvents[0].TestCompleteArg);
                                testCompletedEvents.RemoveAt(0);
                            }
                        }
                        break;
                    case operationEnum.FLUSHOLD:
                        int startOffset = 0;
                        int completeOffset = 0;
                        while (testCompletedEvents.Count - completeOffset > 0 || testStartedEvents.Count - startOffset > 0)
                        {
                            if (testStartedEvents.Count - startOffset > 0)
                            {
                                if (testStartedEvents[startOffset].SequenceCount < which)
                                {
                                    sendTestStartedEvent(testStartedEvents[startOffset].Sender, testStartedEvents[startOffset].TestStartedArg);
                                    testStartedEvents.RemoveAt(startOffset);
                                }
                                else
                                {
                                    startOffset++;
                                }
                            }
                            if (testCompletedEvents.Count - completeOffset > 0)
                            {
                                if (testCompletedEvents[completeOffset].SequenceCount < which)
                                {
                                    sendTestCompletedEvent(testCompletedEvents[completeOffset].Sender, testCompletedEvents[completeOffset].TestCompleteArg);
                                    testCompletedEvents.RemoveAt(completeOffset);
                                }
                                else
                                {
                                    completeOffset++;
                                }
                            }
                        }
                        break;
                } // end switch
            } // end lock

            return retVal;
        }

        /// <summary>
        /// Timer callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void processItemsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                processItemsTimer.Stop();
                bool foundOne = false;

                if (testCompletedEvents.Count == 0 && testStartedEvents.Count == 0 && sequenceAborted && squenceCompleteEvent == null)
                {
                    //sequenceAborted = false;
                }
                else if (sequenceAborted)
                {
                    testStartBottleneckFunction(operationEnum.FLUSH, 0, null, null);
                    testCompleteBottleneckFunction(operationEnum.FLUSH, 0, null, null);
                }

                // Send any old Test Start/Stop ones.
                //testCompleteBottleneckFunction(operationEnum.FLUSHOLD, _SequenceCount, null, null);

                // Send old Sequence complete.
                if (squenceCompleteEvent != null && squenceCompleteEvent.SequenceCount < _SequenceCount)
                {
                    sequenceStartedEvent = null;
                    testCount = -1;
                    nextItemToProcess = -1;
                    sequenceAborted = false;
                    sendSequenceCompletedEvent(squenceCompleteEvent.Sender, squenceCompleteEvent.SequenceCompleteArg);
                    squenceCompleteEvent = null;
                }
 
                // Do all we can do now.
                do
                {
                    foundOne = false;

                    // See if we can send any TestStarted or TestComplete events out
                    if (nextItemToProcess % 2 == 0 && testStartedEvents.Count > 0 && sequenceStartedEvent != null && sequenceStartedEvent.SequenceCount == _SequenceCount) // even number so it is time for TestStartedEvents
                    {
                        TestStartedEventStruct anItem = (TestStartedEventStruct)testStartBottleneckFunction(operationEnum.QUERY, nextItemToProcess, null, null);
                        if (anItem != null) // found one
                        {
                            sendTestStartedEvent(anItem.Sender, anItem.TestStartedArg);
                            testStartBottleneckFunction(operationEnum.REMOVE, nextItemToProcess, null, null);
                            nextItemToProcess++;
                            foundOne = true;
                        }
                    }
                    else if (testCompletedEvents.Count > 0 && sequenceStartedEvent != null && sequenceStartedEvent.SequenceCount == _SequenceCount) // must be odd so do TestCompleteEvents
                    {
                        TestCompletedEventStruct anItem = (TestCompletedEventStruct)testCompleteBottleneckFunction(operationEnum.QUERY, nextItemToProcess, null, null);
                        if (anItem != null) // found one
                        {
                            sendTestCompletedEvent(anItem.Sender, anItem.TestCompleteArg);
                            testCompleteBottleneckFunction(operationEnum.REMOVE, nextItemToProcess, null, null);
                            nextItemToProcess++;
                            foundOne = true;
                        }
                    }

                    Thread.Sleep(10); // give it a rest.
                } while (foundOne);

                // can we send out the sequence complete event?
                if ((squenceCompleteEvent != null && nextItemToProcess >= testCount - 1 && testCount > 0) ||
                    (squenceCompleteEvent != null && squenceCompleteEvent.SequenceCount < _SequenceCount) ||
                    (squenceCompleteEvent != null && sequenceAborted))
                {
                    if (sequenceAborted)
                    {
                        testStartBottleneckFunction(operationEnum.FLUSH, 0, null, null);
                        testCompleteBottleneckFunction(operationEnum.FLUSH, 0, null, null);
                        sequenceAborted = false;
                    }

                    sequenceStartedEvent = null;
                    testCount = -1;
                    nextItemToProcess = -1;
                    sequenceAborted = false;
                    sendSequenceCompletedEvent(squenceCompleteEvent.Sender, squenceCompleteEvent.SequenceCompleteArg);
                    squenceCompleteEvent = null;
                }
            }
            finally
            {
                processItemsTimer.Start();
            }
        }


        #endregion privateMethods

    } // end ResuffleEvents class

    /// <summary>
    /// Structure to hold TestStartedEvents
    /// </summary>
    public class TestStartedEventStruct
    {
        private TestStartedEventStruct()
        {
        }

        /// <summary>
        /// Construct TestStartedEventStruct with specific parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="sequenceCount"></param>
        public TestStartedEventStruct(object sender, StatusEventArgs e, int sequenceCount)
        {
            TestStartedArg = e;
            Sender = sender;
            Index = -1;
            SequenceCount = sequenceCount;
        }

        /// <summary>
        /// The args for callback in Test Started event.
        /// </summary>
        public StatusEventArgs TestStartedArg;
        /// <summary>
        /// The Sender of test start events
        /// </summary>
        public object Sender;
        /// <summary>
        /// The index of test start events
        /// </summary>
        public int Index;
        /// <summary>
        /// The Sequence Count of test start events
        /// </summary>
        public int SequenceCount;
    } // end TestStartedEventStruct class

    /// <summary>
    /// Structure to hold TestCompletedEvents
    /// </summary>
    public class TestCompletedEventStruct
    {
        private TestCompletedEventStruct()
        {
        }

        /// <summary>
        /// Construct TestCompletedEventStruct with specific parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="sequenceCount"></param>
        public TestCompletedEventStruct(object sender, CompletedEventArgs e, int sequenceCount)
        {
            TestCompleteArg = e;
            Sender = sender;
            Index = -1;
            SequenceCount = sequenceCount;
        }
        /// <summary>
        /// The Test Complete Args of test complete events
        /// </summary>
        public CompletedEventArgs TestCompleteArg;
        /// <summary>
        /// The Sender of test complete events
        /// </summary>
        public object Sender;
        /// <summary>
        /// The Index of test complete events
        /// </summary>
        public int Index;
        /// <summary>
        /// The SequenceCount of test complete events
        /// </summary>
        public int SequenceCount;
    } // end TestCompletedEventStruct class

    /// <summary>
    /// Structure to hold SequenceStarted events
    /// </summary>
    public class SequenceStartedEventStruct
    {

        private SequenceStartedEventStruct()
        {
        }

        /// <summary>
        /// Construct SequenceStartedEventStruct with specific parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="sequenceCount"></param>
        public SequenceStartedEventStruct(object sender, StartedEventArgs e, int sequenceCount)
        {
            SequenceStartedArg = e;
            Sender = sender;
            SequenceCount = sequenceCount;
        }
        /// <summary>
        /// The Sequence Started Arg of Sequence Started events
        /// </summary>
        public StartedEventArgs SequenceStartedArg;
        /// <summary>
        /// The Sender of Sequence Started events
        /// </summary>
        public object Sender;
        /// <summary>
        /// The Sequence Count of Sequence Started events
        /// </summary>
        public int SequenceCount;
    } // end SequenceStartedEventStruct class

    /// <summary>
    /// Structure to hold SequenceCompleteEvents.
    /// </summary>
    public class SequenceCompleteStruct
    {

        private SequenceCompleteStruct()
        {
        }

        /// <summary>
        /// Construct SequenceCompleteStruct with specific parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="sequenceCount"></param>
        public SequenceCompleteStruct(object sender, StatusEventArgs e, int sequenceCount)
        {
            SequenceCompleteArg = e;
            Sender = sender;
            Index = -1;
            SequenceCount = sequenceCount;
        }
        /// <summary>
        /// The Args of Sequence Complete events
        /// </summary>
        public StatusEventArgs SequenceCompleteArg;
        /// <summary>
        /// The Sender of Sequence Complete events
        /// </summary>
        public object Sender;
        /// <summary>
        /// The Index of Sequence Complete events
        /// </summary>
        public int Index;
        /// <summary>
        /// The Sequence Count of Sequence Complete events
        /// </summary>
        public int SequenceCount;
    } // end SequenceCompleteStruct class
} // end namespace
