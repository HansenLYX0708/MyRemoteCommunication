


namespace HGST.Blades
{
    /// <summary>
    /// Enum Types of pin motion devices
    /// </summary>
    public enum BunnyPinMotionType
    {
        /// <summary>
        /// Unknown or no pin motion device
        /// </summary>
        NONE,
        /// <summary>
        /// Servo pin motion device
        /// </summary>
        SERVO,
        /// <summary>
        /// Solenoid pin motion device
        /// </summary>
        SOLENOID
    }

    /// <summary>
    /// Enum for pin motion sensors
    /// </summary>
    public enum BunnyPinMotionSensor
    {
        /// <summary>
        /// No sensor
        /// </summary>
        NONE,
        /// <summary>
        /// True when high.
        /// </summary>
        HIGH,
        /// <summary>
        /// True when low.
        /// </summary>
        LOW,
    }

    /// <summary>
    /// Enum for MEMS state.
    /// </summary>
    public enum MemsStateValues
    {
        /// <summary>
        /// Pin motion now at closed position
        /// </summary>
        Closed,

        /// <summary>
        /// Pin motion now at open position.
        /// </summary>
        Opened,

        /// <summary>
        /// Moving to close position.
        /// </summary>
        Closing,

        /// <summary>
        /// Moving to open position.
        /// </summary>
        Opening,

        /// <summary>
        /// We do not know.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Enum status bits;  Designed by SEG13
    /// </summary>
    public enum EnumBunnyStatusBits
    {
        /// <summary>
        /// Solenoid bit
        /// </summary>
        SOLENOID = 0x01,
        /// <summary>
        /// 12V DC bit
        /// </summary>
        AUX_12VDC = 0x02,
        /// <summary>
        /// 5v DC bit
        /// </summary>
        AUX_5VDC = 0x04,
        /// <summary>
        /// Aux 0 out bit
        /// </summary>
        AUX_OUT0 = 0x08,
        /// <summary>
        /// Aux 1 out bit
        /// </summary>
        AUX_OUT1 = 0x10,
        /// <summary>
        /// Aux 0 in bit
        /// </summary>
        AUX_IN0 = 0x20,
        /// <summary>
        /// Aux 1 in bit
        /// </summary>
        AUX_IN1 = 0x40,
        /// <summary>
        /// Backlight bit
        /// </summary>
        BACKLIGHT = 0x80,
        /// <summary>
        /// Servoenabled bit
        /// </summary>
        SERVOENABLED = 0x100,
    }

    /// <summary>
    /// ENUM for move types; Defined by SEG13
    /// </summary>
    public enum EnumServoTypeActions
    {
        /// <summary>
        /// This moves the indicated servo channel according to the current RAM values for move_open 
        ///   This move terminates when the servo channel reaches the indicated parameters 
        /// </summary>
        OPEN,
        /// <summary>
        /// This moves the indicated servo channel according to the current RAM values for move_close 
        ///   This move terminates when the servo channel reaches the indicated parameters 
        /// </summary>
        CLOSE,
        /// <summary>
        /// This moves the indicated servo channel according to the current RAM values for move_close
        ///   This move terminates when the closed switch is asserted 
        /// </summary>
        CLOSETILSWITCH,
        /// <summary>
        /// This moves the indicated servo channel according to the move_current provided parameters (means something) 
        /// </summary>
        MOVERANDOM,
        /// <summary>
        /// This move repeats the move_current move for the indicated channel(the last random move or something we do not know) 
        /// </summary>
        REPEATRANDOM,
        /// <summary>
        /// This turns the indicated servo channel off 
        /// </summary>
        SERVOOFF,
        /// <summary>
        /// This turns the indicated servo channel on 
        /// </summary>
        SERVOON,
    }

    /// <summary>
    /// ENUM Servo channel set/save types; Derfined by SEG13
    /// </summary>
    public enum EnumServoSaveTypes
    {
        /// <summary>
        /// This sets the move_current (last executed) random command values as the RAM values for move_open for the selected servo device 
        /// </summary>
        SETRAMOPEN,
        /// <summary>
        /// This sets the move_current (last executed) random command values as the RAM values for move_close for the selected servo device 
        /// </summary>
        SETRAMCLOSE,
        /// <summary>
        /// This sets the current RAM move_open/move_close for the selected servo device to EEPROM 
        /// </summary>
        WRITEEEPROM,
        /// <summary>
        /// This reads the EEPROM move_open/move_close for the selected servo device to RAM structures 
        /// </summary>
        READEEPROM,
        /// <summary>
        /// This gets the RAM moves 
        /// </summary>
        READRAM,
    }

    /// <summary>
    /// These are the PinMotion types we know about.
    /// </summary>
    public enum EnumSolenoidServoAddr
    {
        /// <summary>
        /// Select servo channel
        /// </summary>
        SOLENOID,
        /// <summary>
        /// Select Solenoid channel
        /// </summary>
        SERVO,
        /// <summary>
        /// Select nothing
        /// </summary>
        NONE
    }

}
