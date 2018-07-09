using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Module.Blade
{
    /// <summary>
    /// Define interface of blade which inherited from INotifyPropertyChanged.
    /// </summary>
    public interface IBlade : INotifyPropertyChanged
    {
        #region Properties
        /// <summary>
        /// We connect the Blade through WCF, this is the IP address.
        /// </summary>
        string IPAddress { get; set; }

        /// <summary>
        /// Indicates that the blade is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Indicates that the blade has been operated.
        /// </summary>
        bool Operational { get; }

        /// <summary>
        /// Indicates the status of blade when running.
        /// </summary>
        BladeState BladeControl { get; }

        /// <summary>
        /// Gets and sets the state of Mems.
        /// </summary>
        OnOffState MemsControl { get; set; }

        /// <summary>
        /// Gets and sets the state of load state.
        /// </summary>
        OnOffState LoadStatus { get; }

        /// <summary>
        /// Gets and sets the state of card power in blade.
        /// </summary>
        OnOffState CardPowerControl { get; set; }

        /// <summary>
        /// Gets and sets the state of LCD.
        /// </summary>
        OnOffState LCDControl { get; set; }

        /// <summary>
        /// Gets and sets the state of AuxOut0.
        /// </summary>
        OnOffState AuxOut0Control { get; set; }

        /// <summary>
        /// Gets and sets the state of AuxOut1.
        /// </summary>
        OnOffState AuxOut1Control { get; set; }

        /// <summary>
        /// Gets the state of AuxIn0.
        /// </summary>
        OnOffState AuxIn0Control { get; }

        /// <summary>
        /// Gets the state of AuxIn1.
        /// </summary>
        OnOffState AuxIn1Control { get; }

        /// <summary>
        /// Gets and sets the state of lock status.
        /// </summary>
        // TODO : If necessary         OnOffState LockControl { get; set; }

        /// <summary>
        /// Record the amount to be recorded on the Blade.
        /// </summary>
        IEnumerable<BladeCount> Counts { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// The automation system connects from the blade here.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="UserID">When authentication is required, the user ID is set aside.</param>
        /// <param name="Password">Same as user ID.</param>
        void Connect(string Address, string UserID, string Password);

        /// <summary>
        /// The automation system disconnects from the blade here.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Reserved for extending the TCL module.
        /// </summary>
        /// <param name="Command">Command of Tcl.</param>
        void TclCommand(string Command);
        #endregion
    }
}
