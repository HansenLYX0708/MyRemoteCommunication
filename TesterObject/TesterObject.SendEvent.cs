using System;
using System.Collections.Generic;
using System.Threading;

using Hitachi.Tester.Enums;
using System.Windows.Forms;
using HGST.Defines;

namespace Hitachi.Tester.Module
{
    public partial class TesterObject : ITesterObject
    {
        #region Fields
        private Queue<BladeEventStruct> _BladeEventQueue = new Queue<BladeEventStruct>();
        /// <summary>
        /// Lock the queue of blade event
        /// </summary>
        private ReaderWriterLock _BladeEventQueueLock = new ReaderWriterLock();
        
        private object _BladeEventLockObj = new object();

        private delegate void AsyncSendGenericEventDelegate(object sender, StatusEventArgs e, ref StatusEventHandler handler, Queue<BladeEventArgs> queue, string eventName);
        private MemsStateValues _LastBunnyStatusSent = MemsStateValues.Unknown;

        private object _SendStatusEventLockObject = new object();
        private int factErrorCount = 0;
        LastSeveralStringsChecker _HistoryOfLastSeveralMessages = new LastSeveralStringsChecker(5);

        private int _ConsecutiveCount = 0;
        #endregion Fields

        #region Methods
        private void SendBunnyEvent(object sender, StatusEventArgs e)
        {
            object[] objArray = { sender, (object)e };
            Thread BunnyEventThread = new Thread(new ParameterizedThreadStart(SendBunnyEventThreadFunc))
            {
                IsBackground = true
            };
            BunnyEventThread.Start((object)objArray);
        }

        private void SendStatusEvent(object sender, StatusEventArgs e)
        { 
            lock (_SendStatusEventLockObject)
            {
                // TODO : write line
                if (!((eventInts)e.EventType == eventInts.Notify) &&
                   !((eventInts)e.EventType == eventInts.NotifyWithContent))
                {
                    // Remeber the last one. TODO : no use, I remove it

                    _HistoryOfLastSeveralMessages.InsertMessage(e.Text);

                    if (_HistoryOfLastSeveralMessages.IsThisStringThereSomewhere(Constants.FACTCmdErrorString))
                    {
                        // if too many return.
                        if (factErrorCount > Constants.TesterObjectFactErrorLimit)
                        {
                            return;
                        }
                        // inc count
                        factErrorCount++;
                    }
                    else if (e.Text.Trim().Length > 0)
                    {
                        // if not sendCommand driver error then zero count.
                        // not consecutive driver error so zero counter
                        factErrorCount = 0;
                    }

                    if (e.Text.ToLower().Contains("event::"))
                    {
                        int separator = e.Text.Substring(7).IndexOf("::");
                        WriteLine(e.Text.Substring(0, separator + 7));
                        WriteLineContent("TesterObject::SendStatusEvent" + e.Text);
                    }
                }
            }
            // Single channel remoting
            SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.Status, e).ToBladeEventArgs());
        }

        private void SendTestStartedEvent(object sender, StatusEventArgs e)
        {
            MethodInvoker del = delegate
            {
                string instanceString = "";
                try
                {
                    // Single channel remoting
                    SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.TestStarted, e).ToBladeEventArgs());

                    try
                    {
                        instanceString = MyLocation;
                    }
                    catch
                    {
                        instanceString = "NONE";
                    }
                    WriteLine("TesterObject::SendTestStartedEvent fired ");
                    WriteLineContent("TesterObject::SendTestStartedEvent " + Environment.NewLine +
                        "Sequence: " + _TesterState.SequenceName + Environment.NewLine +
                        "Grade: " + _TesterState.GradeName + Environment.NewLine +
                        "Instance: " + instanceString + Environment.NewLine +
                        "Text: " + e.Text + Environment.NewLine +
                        "Event type: " + e.EventType + Environment.NewLine +
                        "Consecutive count: " + e.ConsecutiveCount);
                }
                catch (Exception ee)
                {
                    WriteLine("TesterObject::SendTestStartedEvent exception");
                    WriteLineContent("TesterObject::SendTestStartedEvent exception " + MakeUpExceptionString(ee).ToString() + Environment.NewLine +
                        "Sequence: " + _TesterState.SequenceName + Environment.NewLine +
                        "Grade: " + _TesterState.GradeName + Environment.NewLine +
                        "Instance: " + instanceString + Environment.NewLine +
                        "Text: " + e.Text + Environment.NewLine +
                        "Event type: " + e.EventType + Environment.NewLine +
                        "Consecutive count: " + e.ConsecutiveCount);
                }
            };
            del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { del.EndInvoke(ar); }), del);
        }

        private void SendTestCompletedEvent(object sender, CompletedEventArgs e)
        {
            MethodInvoker del = delegate
            {
                string instanceString = "";
                try
                {
                    // Single remoting channel
                    SendBladeEventCallback(sender, new CompletedBladeEventArgs(BladeEventType.TestCompleted, e).ToBladeEventArgs());
                    try
                    {
                        instanceString = MyLocation;
                    }
                    catch
                    {
                        instanceString = "NONE";
                    }

                    WriteLine("TesterObject::SendTestCompletedEvent ");
                    WriteLineContent("TesterObject::SendTestCompletedEvent  " + Environment.NewLine +
                        "Text: " + e.Text + Environment.NewLine +
                        "Test number: " + e.testNum + Environment.NewLine +
                        "Test count: " + e.testCount + Environment.NewLine +
                        "Fail: " + e.fail + Environment.NewLine +
                        "Instance: " + instanceString);
                }
                catch (Exception ee)
                {
                    WriteLine("TesterObject::SendTestCompletedEvent with exception");
                    WriteLineContent("TesterObject::SendTestCompletedEvent exception " + MakeUpExceptionString(ee).ToString() + Environment.NewLine +
                        "Text " + e.Text + Environment.NewLine +
                        "Test number: " + e.testNum + Environment.NewLine +
                        "Test count: " + e.testCount + Environment.NewLine +
                        "Fail: " + e.fail + Environment.NewLine +
                        "Instance " + instanceString);
                }
            };
            // Block on last one.
            if (e.testNum == e.testCount - 1)
            {
                del.Invoke();
            }
            else
            {
                del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { del.EndInvoke(ar); }), del);
            }
        }

        private void SendProgramClosingEvent(object sender, StatusEventArgs e)
        {
            MethodInvoker del = delegate
            {
                // Single remoting channel
                SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.ProgramClosing, e).ToBladeEventArgs());
            };
            del.Invoke();
        }

        private void SendSequenceStartedEvent(object sender, StartedEventArgs e)
        {
            string instanceString = "";
            try
            {
                // if modifying code,please do not remove this part
                // This is for ping.  It does not inc count if someone does ping.
                if (!e.seqName.Contains(Constants.CommTestString))
                {
                    // Inc Test count;
                    _CountStateFromDisk.IncValue(BladeDataName.TestCount);
                    WriteOutCountsData();
                }

                // Single channel remoting
                SendBladeEventCallback(sender, new SequenceStartedBladeEventArgs(BladeEventType.SequenceStarted, e).ToBladeEventArgs());
                try
                {
                    instanceString = MyLocation;
                }
                catch
                {
                    instanceString = "NONE";
                }
                WriteLine("TesterObject::SendSequenceStartedEvent fired ");
                WriteLineContent("TesterObject::SendSequenceStartedEvent " + Environment.NewLine +
                    "Sequence: " + _TesterState.SequenceName + Environment.NewLine +
                    "Grade: " + _TesterState.GradeName + Environment.NewLine +
                    "Instance: " + instanceString);

                // Send Event:: for JAS
                //Event::Completed::Blade5Completed::False
                SendStatusEvent(sender, new StatusEventArgs("Event::Completed::" + MyLocation + "Completed::False" + Environment.NewLine, (int)eventInts.toTv));
            }
            catch (Exception ee)
            {
                WriteLine("TesterObject::SendSequenceStartedEvent exception");
                WriteLineContent("TesterObject::SendSequenceStartedEvent exception " + MakeUpExceptionString(ee).ToString() + Environment.NewLine +
                    "Sequence: " + _TesterState.SequenceName + Environment.NewLine +
                    "Grade: " + _TesterState.GradeName + Environment.NewLine +
                    "Instance: " + instanceString);
            }
        }

        private void SendSequenceAbortingEvent(object sender, StatusEventArgs e)
        {
            MethodInvoker del = delegate
            {
                string instanceString = "";
                try
                {
                    // Single remoting channel
                    SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.SequenceAborting, e).ToBladeEventArgs());

                    try
                    {
                        instanceString = MyLocation;
                    }
                    catch
                    {
                        instanceString = "NONE";
                    }

                    WriteLine("TesterObject::SendSequenceAbortingEvent ");
                    WriteLineContent("TesterObject::SendSequenceAbortingEvent " + Environment.NewLine +
                        "Text: " + e.Text + Environment.NewLine +
                        "Event type: " + e.EventType + Environment.NewLine +
                        "Consecutive count: " + e.ConsecutiveCount + Environment.NewLine +
                        "Instance: " + instanceString);
                }
                catch (Exception ee)
                {
                    WriteLine("TesterObject::SendSequenceAbortingEvent exception");
                    WriteLineContent("TesterObject::SendSequenceAbortingEvent exception " + MakeUpExceptionString(ee).ToString() + Environment.NewLine +
                        "Text: " + e.Text + Environment.NewLine +
                        "Event type: " + e.EventType + Environment.NewLine +
                        "Consecutive count: " + e.ConsecutiveCount + Environment.NewLine +
                        "Instance: " + instanceString);
                }
                finally
                {
                    // clear a bunch of flags.
                    _TesterState.CmdBusy = false;
                    _TesterState.NowTestsArePaused = false;
                    _TesterState.PauseEvents = false;
                    _TesterState.PauseTests = false;
                    _TesterState.PleaseStop = false;
                    _TesterState.SeqGoing = false;
                }
            };
            del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { del.EndInvoke(ar); }), del);
        }

        /// <summary>
        /// TODO : only be used in ping function. no use
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendSequenceUpdateEvent(object sender, StatusEventArgs e)
        {
            MethodInvoker del = delegate
            {
                // Single remoting channel
                SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.SequenceUpdate, e).ToBladeEventArgs());
            };
            del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { del.EndInvoke(ar); }), del);
        }

        private void SendSequenceCompletedEvent(object sender, StatusEventArgs e)
        {
            MethodInvoker del = delegate
            {
                string instanceString = "";
                try
                {
                    // Single remoting channel
                    SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.SequenceCompleted, e).ToBladeEventArgs());

                    // Event:: for JAS
                    //Event::Completed::Blade5Completed::True
                    SendStatusEvent(sender, new StatusEventArgs("Event::Completed::" + MyLocation + "Completed::True" + Environment.NewLine, (int)eventInts.toTv));

                    try
                    {
                        instanceString = MyLocation;
                    }
                    catch
                    {
                        instanceString = "NONE";
                    }

                    WriteLine("TesterObject::SendSequenceCompletedEvent ");
                    WriteLineContent("TesterObject::SendSequenceCompletedEvent " + Environment.NewLine +
                        "Text: " + e.Text + Environment.NewLine +
                        "Event type: " + e.EventType + Environment.NewLine +
                        "Consecutive count: " + e.ConsecutiveCount);
                }
                catch (Exception ee)
                {
                    WriteLine("TesterObject::SendSequenceCompletedEvent exception");
                    WriteLineContent("TesterObject::SendSequenceCompletedEvent exception " + MakeUpExceptionString(ee).ToString() + Environment.NewLine +
                        "Text: " + e.Text + Environment.NewLine +
                        "Event type: " + e.EventType + Environment.NewLine +
                        "Consecutive count: " + e.ConsecutiveCount);
                }
                finally
                {
                    _TesterState.CmdBusy = false;
                    _TesterState.NowTestsArePaused = false;
                    _TesterState.PauseEvents = false;
                    _TesterState.PauseTests = false;
                    _TesterState.PleaseStop = false;
                    _TesterState.SeqGoing = false;
                    _TesterState.TestNumber = -1;
                }
            };
            del.BeginInvoke(new AsyncCallback(delegate (IAsyncResult ar) { del.EndInvoke(ar); }), del);
        }

        /// <summary>
        /// Puts items in the queue.
        /// It called by all send event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendBladeEventCallback(object sender, BladeEventArgs e)
        {
            lock (_BladeEventLockObj)
            {
                e.Consecutive = _ConsecutiveCount++;
                if (_ConsecutiveCount < 0) _ConsecutiveCount = 0;
            }

            if ( e.Text.ToLower().IndexOf("Event::varToEvent".ToLower()) == 0)
            {

            }
            else if (e.Text.ToLower().Contains("Event::PowerEvent::PCBPower1".ToLower()))
            {

            }
            else if (e.Text.ToLower().Contains("Event::PowerEvent::PCBPower0".ToLower()))
            {

            }
            else if (e.Text.ToLower().Contains("Event::MEMSOpenEvent::MEMSOpen".ToLower()))
            {

            }
            else if (e.Text.ToLower().Contains("Event::MEMSCloseEvent::MEMSClose".ToLower()))
            {

            }
            else if (e.Text.ToLower().Contains("Event::GetBladeValue::MEMSState".ToLower()))
            {

            }
            else if (e.Text.ToLower().Contains("Event::LoadActuator::".ToLower()))
            {

            }
            if (_BladeEventQueue != null && _BladeEventQueueLock != null)
            {
                try
                {
                    try
                    {
                        _BladeEventQueueLock.AcquireWriterLock(TimeSpan.FromMilliseconds((2000)));
                    }
                    catch 
                    {
                    	// TODO : Do it anyway?
                    }
                    _BladeEventQueue.Enqueue(new BladeEventStruct( sender, e ));
                    // Send to host (BladeRunner)
                    try
                    {
                        StaticServerTalker.BladeEvent(new object[] { sender, e });
                    }
                    catch 
                    {
                        // TODO : Do nothing?
                    }


                }
                catch 
                {
                	// TODO : do nothing?
                }
                finally
                {
                    if (_BladeEventQueueLock.IsWriterLockHeld)
                    {
                        _BladeEventQueueLock.ReleaseWriterLock();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="passingObject"></param>
        private void SendBunnyEventThreadFunc(object passingObject)
        {
            object[] objArray = (object[])passingObject;
            object sender = objArray[0];
            StatusEventArgs e = (StatusEventArgs)objArray[1];

            BunnyEvents bunnyEvent = (BunnyEvents)e.EventType;
            // Add log information.voltageCheckBlade Aux I/O is too talkative.
            // TODO : Write line content
            if (!(_VoltageCheckBlade) &&
                (
                (BunnyEvents)e.EventType == BunnyEvents.Aux0In ||
                (BunnyEvents)e.EventType == BunnyEvents.Aux0Out ||
                (BunnyEvents)e.EventType == BunnyEvents.Aux1In ||
                (BunnyEvents)e.EventType == BunnyEvents.Aux1Out ||
                (BunnyEvents)e.EventType == BunnyEvents.BunnyStatus
                )
                )
            {
                WriteLineContent(string.Format("TesterObject::SendBunnyEventThreadFunc [EventType :{0}] [Text:{1}]", bunnyEvent, e.Text));
            } 
            if (bunnyEvent == BunnyEvents.Broke)
            {
                NotifyWorldBunnyStatus(false, "Bunny broke event");
                ClearAllValues();
                if (_BunnyCard != null) _TesterState.BunnyGood = false;
            }
            else if (bunnyEvent == BunnyEvents.BunnyFixed)
            {
                if (_BunnyCard != null)
                {
                    _TesterState.BunnyGood = true;
                }
                else
                {
                    NotifyWorldBunnyStatus(false, "Bunny broke event");
                    ClearAllValues();
                }
            }
            else if (bunnyEvent == BunnyEvents.MemsOpenClose)
            {
                // TODO : LastBunnyStatusSent no use, should remove it
                _LastBunnyStatusSent = (MemsStateValues)Enum.Parse(typeof(MemsStateValues), e.Text, true);
            }
            // Single channel remoting
            SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.Bunny, e).ToBladeEventArgs());
        }

        /// <summary>
        /// This function pulls items out and sends to remote clients.
        /// Actually it calls the callback function in each client via proxy.
        /// </summary>
        private void DoBladeEvents()
        {
            // TODO : Deal the event send from SendBladeEventCallback
            while (!_Exit)
            {
                List<BladeEventStruct> bladeEventArgList = new List<BladeEventStruct>();
                try
                {
                    // Use ReaderWriterLock
                    while (!_Exit)
                    {
                        try
                        {
                            _BladeEventQueueLock.AcquireWriterLock(20);
                            break;
                        }
                        catch (ApplicationException)
                        {
                            // Timeout
                            if (_Exit) return;
                            continue;
                        }
                    }

                    if (_Exit) continue;
                    if (_BladeEventQueue.Count == 0 || _TesterState.PauseEvents)
                    {
                        // Do not send event
                        Thread.Sleep(5);
                        continue;
                    }

                    while (_BladeEventQueue.Count > 0)
                    {
                        BladeEventStruct anEventStruct = _BladeEventQueue.Dequeue();
                        BladeEventArgs anEventArg = anEventStruct.EE;

                        bladeEventArgList.Add(anEventStruct);
                    }
                }
                finally
                {
                    if (_BladeEventQueueLock.IsWriterLockHeld)
                    {
                        // Release access right
                        _BladeEventQueueLock.ReleaseWriterLock();
                    }
                }
                if (_Exit) return;
                foreach (BladeEventStruct anEventStruct in bladeEventArgList)
                {
                    BladeEventArgs anEventArg = anEventStruct.EE;
                    List<ITesterObjectCallback> sentList = new List<ITesterObjectCallback>();

                    for (int i = 0; _CallbackProxyList != null && i < _CallbackProxyList.Count; i++)
                    {
                        ITesterObjectCallback callback = _CallbackProxyList.Get(i);
                        if (!sentList.Contains(callback))
                        {
                            sentList.Add(callback);
                            try
                            {
                                callback.BladeEventCallback(anEventArg);
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    StaticServerTalker.MessageString = "Client callback proxy failed. ";
                                    StaticServerTalker.MessageStringContent = "Client callback proxy failed. " + Environment.NewLine + MakeUpExceptionString(e).ToString();
                                }
                                catch { }
                                _CallbackProxyList.Remove(i);
                                sentList.Remove(callback);
                                i--;
                            }
                        }
                    }
                    if (_Exit) break;
                }
            }
        }
        #endregion Methods
    }
}
