using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.ServiceModel;

using Hitachi.Tester.Module;

namespace Hitachi.Tester.Client
{
    /// <summary>
    /// Service definition for Client callback.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class TesterObjectCallback : ITesterObjectCallback
    {
        #region Fields
        private RemoteConnectLib _RemoteConnectLib;

        #endregion Fields

        #region Constructors
        public TesterObjectCallback(RemoteConnectLib remoteConnectLib)
        {
            _RemoteConnectLib = remoteConnectLib;
        }
        #endregion Constructors

        #region Properties

        #endregion Properties

        #region Methods

        #endregion Methods



        #region ITesterObjectCallback Members

        /// <summary>
        /// Calls the callback decision func in RemoteConnectLib.
        /// RemoteConnectLib splits out the events from TesterObject and sends them to the correct
        ///  callback function in TesterClient.
        /// </summary>
        /// <param name="e"></param>
        public void BladeEventCallback(BladeEventArgs e)
        {
            // remoteConnectLib.BladeEvent.InternalBladeCallback(this, e);
        }

        #endregion
    }
}
