using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Hitachi.Tester.Enums;

namespace Hitachi.Tester.Module
{
    public partial class TesterObject
    {
        #region Fields
        private Queue<BladeEventStruct> _BladeEventQueue = new Queue<BladeEventStruct>();
        private ReaderWriterLock _BladeEventQueueLock = new ReaderWriterLock();
        private Thread _BladeEventsThread;
        private object _BladeEventLockObj = new object();

        private delegate void AsyncSendGenericEventDelegate(object sender, StatusEventArgs e, ref StatusEventHandler handler, Queue<BladeEventArgs> queue, string eventName);
        private MemsStateValues _LastBunnyStatusSent = MemsStateValues.Unknown;
        #endregion Fields

        #region Properties

        #endregion Properties

        #region Methods
        /// <summary>
        /// Puts items in the queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendBladeEventCallback(object sender, BladeEventArgs e)
        {

        }

        /// <summary>
        /// This function pulls items out and sends to remote clients.
        /// Actually it calls the callback function in each client via proxy.
        /// </summary>
        private void doBladeEvents()
        {
            // TODO : 处理SendBladeEventCallback传来的事件
            while (!_Exit)
            {
                //doBladeEventsGoing = true;
                List<BladeEventStruct> bladeEventArgList = new List<BladeEventStruct>();
                try
                {
                    // 2010/08/24 Akishi Murata: Use ReaderWriterLock instead of lock statement
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
                    // TODO : 下面的测试状态
                    if (_BladeEventQueue.Count == 0 /*||
                        testerState.bPauseEvents*/)
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
                                    StaticServerTalker.MessageStringContent = "Client callback proxy failed. " + Environment.NewLine + makeUpExceptionString(e).ToString();
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
