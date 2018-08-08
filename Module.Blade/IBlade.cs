using System;
using System.Collections.Generic;
using System.ComponentModel;
using WD.Tester.Enums;

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
        OnOffValues AuxOut0 { get; set; }

        /// <summary>
        /// Gets and sets the state of AuxOut1.
        /// </summary>
        OnOffValues AuxOut1 { get; set; }

        /// <summary>
        /// Gets the state of AuxIn0.
        /// </summary>
        OnOffValues AuxIn0 { get; }

        /// <summary>
        /// Gets the state of AuxIn1.
        /// </summary>
        OnOffValues AuxIn1 { get; }

        /// <summary>
        /// Gets and sets the state of lock status.
        /// </summary>
        // TODO : If necessary OnOffState LockControl { get; set; }

        /// <summary>
        /// Record the amount to be recorded on the Blade.
        /// </summary>
        IEnumerable<BladeCount> Counts { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Interface to connect to the server
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="UserID">When authentication is required, the user ID is set aside.</param>
        /// <param name="Password">Same as user ID.</param>
        void Connect(string Address, string UserID, string Password);

        /// <summary>
        /// Interface to disconnect to the server
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
