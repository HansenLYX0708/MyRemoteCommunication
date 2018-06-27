using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Hitachi.Tester.Enums;

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
        private Thread _BladeEventsThread;
        private object _BladeEventLockObj = new object();

        private delegate void AsyncSendGenericEventDelegate(object sender, StatusEventArgs e, ref StatusEventHandler handler, Queue<BladeEventArgs> queue, string eventName);
        private MemsStateValues _LastBunnyStatusSent = MemsStateValues.Unknown;

        private object _SendStatusEventLockObject = new object();

        private int _ConsecutiveCount = 0;
        #endregion Fields

        #region Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendBunnyEvent(object sender, StatusEventArgs e)
        {
            object[] objArray = { sender, (object)e };
            Thread BunnyEventThread = new Thread(new ParameterizedThreadStart(SendBunnyEventThreadFunc));
            BunnyEventThread.IsBackground = true;
            BunnyEventThread.Start((object)objArray);
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

            BunnyEvents bEvent = (BunnyEvents)e.EventType;
            // Add log information.voltageCheckBlade Aux I/O is too talkative.
            // TODO : Write line content
            
            //  
            if (bEvent == BunnyEvents.Broke)
            {

            }
            else if (bEvent == BunnyEvents.BunnyFixed)
            {

            }
            else if (bEvent == BunnyEvents.MemsOpenClose)
            {

            }

            // Single channel remoting
            SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.Bunny, e).ToBladeEventArgs());
        }

        private void SendStatusEvent(object sender, StatusEventArgs e)
        { 
            lock (_SendStatusEventLockObject)
            {

            }
            // Single channel remoting
            SendBladeEventCallback(sender, new StatusBladeEventArgs(BladeEventType.Status, e).ToBladeEventArgs());
        }

        // TODO : The event of sequence. 
        private void SendSequenceStartedEvent(object sender, StartedEventArgs e)
        { }

        private void SendSequenceAbortingEvent(object sender, StatusEventArgs e)
        { }

        private void SendSequenceCompletedEvent(object sender, StatusEventArgs e)
        { }

        private void SendSequenceUpdateEvent(object sender, StatusEventArgs e)
        { }

        private void SendTestStartedEvent(object sender, StatusEventArgs e)
        { }

        private void SendTestCompletedEvent(object sender, CompletedEventArgs e)
        { }

        private void SendProgramClosingEvent(object sender, StatusEventArgs e)
        { }


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
                    catch (System.Exception ex)
                    {
                    	// TODO : Do it anyway?
                    }
                    _BladeEventQueue.Enqueue(new BladeEventStruct( sender, e ));
                    // Send to host (BladeRunner)
                    try
                    {
                        StaticServerTalker.BladeEvent(new object[] { sender, e });
                    }
                    catch (System.Exception ex)
                    {
                        // TODO : Do nothing?
                    }


                }
                catch (System.Exception ex)
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
        /// This function pulls items out and sends to remote clients.
        /// Actually it calls the callback function in each client via proxy.
        /// </summary>
        private void DoBladeEvents()
        {
            // TODO : Deal the event send from SendBladeEventCallback
            while (!_Exit)
            {
                //doBladeEventsGoing = true;
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
