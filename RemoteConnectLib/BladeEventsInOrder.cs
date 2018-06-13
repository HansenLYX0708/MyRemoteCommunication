using System;
using System.Collections.Generic;
using System.Threading;
using Hitachi.Tester.Module;
using NLog;

namespace Hitachi.Tester.Client
{
    public class BladeEventsInOrder
    {
        #region Fields
        private readonly Logger EventOrderLog = LogManager.GetLogger("EventOrderLog");

        private int _NowServing;
        private System.Timers.Timer _OutputTimer;
        private volatile List<BladeEventWithTime> _EventQueue;
        private volatile ReaderWriterLock _ReaderWriterLock;

        // Our event Sends out ordered items.
        public event BladeEventHandler OrderedBladeEvent;
        #endregion Fields



        #region Constructors
        public BladeEventsInOrder()
        {
            _EventQueue = new List<BladeEventWithTime>();
            _ReaderWriterLock = new ReaderWriterLock();
            _NowServing = -1;
            _OutputTimer = new System.Timers.Timer(25);
            _OutputTimer.Elapsed += new System.Timers.ElapsedEventHandler(_OutputTimer_Elapsed);
            _OutputTimer.Start();
        }

        #endregion Constructors

        #region Properties

        #endregion Properties


        #region Methods

        /// <summary>
        /// Callback for timer event.
        /// Flushes queue of sequential entries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void  _OutputTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // Stop timer until we finish
                _OutputTimer.Stop();

                try
                {
                    _ReaderWriterLock.AcquireWriterLock(5000);
                }
                catch
                {
                    // Do it anyway
                } 

                if (_EventQueue.Count > 0)
                {
                    for (int i = 0; i < _EventQueue.Count; i++)
                    {
                        // Fetch one item
                        BladeEventWithTime anItem = _EventQueue[i];

                        // Send stale ones
                        if (anItem.ArrivalTime + TimeSpan.FromMilliseconds(500) < DateTime.Now)
                        {
                            EventOrderLog.Info("Stale event e.con:{0} e.text:{1} e.type:{2} queue.count:{3}, nowServing:{4} ", anItem.EventArgs.Consecutive, anItem.EventArgs.Text, anItem.EventArgs.Type, _EventQueue.Count, _NowServing);

                            _SendOrderedOutputEvent(anItem.Sender, anItem.EventArgs);

                            // Resync
                            if (_NowServing <= anItem.EventArgs.Consecutive)
                            {
                                Interlocked.Exchange(ref _NowServing, anItem.EventArgs.Consecutive + 1);
                            }
                            _EventQueue.Remove(anItem);
                            i--;
                            continue;
                        }

                        // Is this one now ready to send?
                        if (_NowServing >= anItem.EventArgs.Consecutive)
                        {
                            EventOrderLog.Info("Regular event e.con:{0} e.text:{1} e.type:{2} queue.count:{3}, nowServing:{4} ", anItem.EventArgs.Consecutive, anItem.EventArgs.Text, anItem.EventArgs.Type, _EventQueue.Count, _NowServing);
                            _SendOrderedOutputEvent(anItem.Sender, anItem.EventArgs);
                            Interlocked.Increment(ref _NowServing);
                            _EventQueue.Remove(anItem);
                            i--;
                        }
                        else
                        {
                            break; // Nothing to do (yet).
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    // Release lock
                    _ReaderWriterLock.ReleaseLock();
                }
                catch
                {
                    // disregard.
                }
                // restart timer
                _OutputTimer.Start();
            }
        
        }

        /// <summary>
        ///  Send ordered event to whom ever has subscribed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SendOrderedOutputEvent(object sender, BladeEventArgs e)
        {
            BladeEventHandler handler = OrderedBladeEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        /// <summary>
        /// Push one item into our queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddOneToQueue(object sender, BladeEventArgs e)
        {
            if (e == null) return;

            // Try to get a lock
            try
            {
                try
                {
                    _ReaderWriterLock.AcquireWriterLock(5000);
                }
                catch
                {
                    // Do it anyway
                } 

                // If items there then insert in order.
                if (_EventQueue.Count > 0)
                {
                    EventOrderLog.Info("Add another event e.con:{0} e.text:{1} e.type:{2} queue.count:{3}, nowServing:{4}", e.Consecutive, e.Text, e.Type, _EventQueue.Count, _NowServing);
                    // Start at end and iterate back until we find a place to insert.
                    for (int index = _EventQueue.Count - 1; index >= 0; index--)
                    {
                        try
                        {
                            // Is this one null
                            if (_EventQueue[index] == null)
                            {
                                _EventQueue[index] = new BladeEventWithTime(DateTime.Now, sender, e);
                            }
                            // Is this a good spot?
                            else if (e.Consecutive > _EventQueue[index].EventArgs.Consecutive)
                            {
                                // Insert here.
                                _EventQueue.Insert(index + 1, new BladeEventWithTime(DateTime.Now, sender, e));
                                break;
                            }
                            // did we run out of items
                            else if (index == 0)
                            {
                                // Insert at top.
                                _EventQueue.Insert(0, new BladeEventWithTime(DateTime.Now, sender, e));
                                break;
                            }
                        }
                        catch
                        {
                            // skip
                        }
                    }
                }
                else 
                {
                    EventOrderLog.Info("Add new event e.con:{0} e.text:{1} e.type:{2} queue.count:{3}, nowServing:{4}", e.Consecutive, e.Text, e.Type, _EventQueue.Count, _NowServing);
                    // else nothing there just add.
                    _EventQueue.Add(new BladeEventWithTime(DateTime.Now, sender, e));
                    // If needed Init the count.
                    if (_NowServing < 0)
                    {
                        Interlocked.Exchange(ref _NowServing,  e.Consecutive);
                    }
                }
            }
            finally
            {
                try
                {
                    // Release lock
                    _ReaderWriterLock.ReleaseLock();
                }
                catch
                {
                    // ignored
                }
            }
        }

        #endregion Methods

    } // end class
} // end Namespace
