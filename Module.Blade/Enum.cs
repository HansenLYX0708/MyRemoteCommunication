using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Blade
{
    /// <summary>
    /// Control state quantity in Slot module.
    /// </summary>
    public enum OnOffState
    {
        Unknown = 0,
        On,
        Off,
        TuringOn,
        TurningOff,
        Error,
    }

    /// <summary>
    /// Indicates that the blade state.
    /// </summary>
    public enum BladeState
    {
        Unknown = 0,
        OnConnecting,
        Disconnected,
        Idle,
        OnMeasurement,
        Error,
    }

    /// <summary>
    /// Indicates the category of text.
    /// </summary>
    public enum TextCategory
    {
        Normal = 0,
        Event,
        Error,
    }
}
