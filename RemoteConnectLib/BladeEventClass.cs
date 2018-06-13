
using System;
using System.Collections.Generic;
using System.Text;
using Hitachi.Tester.Module;

namespace Hitachi.Tester.Client
{
    public class BladeEventClass : AbstractEventClass
    {
        #region Fields
        BladeEventsInOrder unshuffleEvents;
        private RemoteConnectLib remoteConnectLib;

        private delegate void voidObjectBladeEventArgs(object sender, BladeEventArgs e);
        #endregion Fields

        #region Constructors
        private BladeEventClass()
        {
        }

        public BladeEventClass(RemoteConnectLib _remoteConnectLib)
        {
            remoteConnectLib = _remoteConnectLib;
            unshuffleEvents = new BladeEventsInOrder();
            unshuffleEvents.OrderedBladeEvent += new BladeEventHandler(unshuffleEvents_OrderedBladeEvent);
        }
        #endregion Constructors

        #region Methods
        /// <summary>
        /// This is the callback for events from the server object
        /// This is no longer used.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void InternalStatusCallback(object sender, StatusEventArgs e)
        {
            return;
        }

        /// <summary>
        /// This function calls the event dispatch function on a worker thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void InternalBladeCallback(object sender, BladeEventArgs e)
        {
            voidObjectBladeEventArgs bladeEventDelegate = new voidObjectBladeEventArgs(unshuffleEvents.AddOneToQueue);
            bladeEventDelegate.BeginInvoke(sender, e, new AsyncCallback(delegate (IAsyncResult ar) { bladeEventDelegate.EndInvoke(ar); }), bladeEventDelegate);
        }

        /// <summary>
        /// Override lifetime manager for an infinite lease on life.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Object</returns>
        public override Object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        /// Receives BladeRunner BladeEvent callback from host and dispatches to correct callback function 
        /// in TesterObject (client).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void unshuffleEvents_OrderedBladeEvent(object sender, BladeEventArgs e)
        {
            switch (e.EventType)
            {
                case BladeEventType.Bunny:
                    //remoteConnectLib.OnBunnyEvent(sender, e.ToStatusEventBladeArgs().ToStatusEventArgs());
                    break;

                case BladeEventType.ProgramClosing:
                    //remoteConnectLib.OnProgramClosingEvent(sender, e.ToStatusEventBladeArgs().ToStatusEventArgs());
                    break;

                case BladeEventType.SequenceAborting:
                    //remoteConnectLib.OnSequenceAbortingEvent(sender, e.ToStatusEventBladeArgs().ToStatusEventArgs());
                    break;

                case BladeEventType.SequenceCompleted:
                    //remoteConnectLib.OnSequenceCompleteEvent(sender, e.ToStatusEventBladeArgs().ToStatusEventArgs());
                    break;

                case BladeEventType.SequenceStarted:
                    //remoteConnectLib.OnSequenceStartedEvent(sender, e.ToSequenceStartedBladeEventArgs().ToStartedEventArgs());
                    break;

                case BladeEventType.SequenceUpdate:
                    //remoteConnectLib.OnSequenceUpdateEvent(sender, e.ToStatusEventBladeArgs().ToStatusEventArgs());
                    break;

                case BladeEventType.TestCompleted:
                    //remoteConnectLib.OnTestCompleteEvent(sender, e.ToCompletedEventBladeArgs().ToCompletedEventArgs());
                    break;

                case BladeEventType.TestStarted:
                    //remoteConnectLib.OnTestStartedEvent(sender, e.ToStatusEventBladeArgs().ToStatusEventArgs());
                    break;

                case BladeEventType.Status:
                    //remoteConnectLib.OnStatusEvent(sender, e.ToStatusEventBladeArgs().ToStatusEventArgs());
                    break;
            }
        }
        #endregion Methods
    }
}
