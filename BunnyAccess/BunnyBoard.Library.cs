
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace HGST.Blades
{

    partial class BunnyBoard //: Device<BunnyBoard>   
    {
        /// <summary>
        /// Filename of bunny serial number file
        /// </summary>
        public static readonly string BladeSNTXT = "BladeSN.TXT";
        /// <summary>
        /// Filename of bunny info file
        /// </summary>
        public static readonly string BladeInfoTXT = "BladeInfo.TXT";

        /// <summary>
        /// This constant must match the definition in BunnyDrv_ioct.h
        /// </summary>
        public const uint FILE_DEVICE_BunnyDriver = 0xCF53;

        /// <summary>
        /// The following constant is defined by Microsoft.
        /// </summary>
        public const uint METHOD_BUFFERED = 0x00000000;
        //public const uint FILE_READ_ACCESS = 0x80000000;
        //public const uint FILE_WRITE_ACCESS = 0x40000000;
        //public const uint GENERIC_READ = 0x80000000;
        //public const uint GENERIC_WRITE = 0x40000000;

        /// <summary>
        /// The following constant is defined by Microsoft.
        /// </summary>
        public const uint FILE_SHARE_READ = 0x00000001;
        /// <summary>
        /// The following constant is defined by Microsoft.
        /// </summary>
        public const uint FILE_SHARE_WRITE = 0x00000002;
        //public const uint FILE_SHARE_DELETE = 0x00000004;

        //public const uint CREATE_NEW = 1;
        //public const uint CREATE_ALWAYS = 2;
        //public const uint OPEN_EXISTING = 3;
        //public const uint OPEN_ALWAYS = 4;
        //public const uint TRUNCATE_EXISTING = 5;

        /// <summary>
        /// This is how many chars are on the LCD.
        /// Two rows of 16.
        /// </summary>
        public const int DISPLAY_SIZE = 32;

        /// <summary>
        /// Size of USB transfer block.
        /// </summary>
        public const int BUFFER_SIZE = 64;

        #region defineIoctls
        // Helper function matches same function in BunnyDrv_ioctl.h
        static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
        {
            return ((deviceType) << 16) | ((access) << 14) | ((function) << 2) | (method);
        }

        // All of  the following static ints must match the definition in BunnyDrv_ioctl.h
        // See the Bunny kernel driver project for details.
        // We use these to call the bunny functions in the kernel driver.

        
        /// <summary>
        /// This command uploads the status of all port IO bits. 
        /// </summary>
        public static uint IOCTL_HtGSUploadStatus = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2049,
                                                 METHOD_BUFFERED,
                                                 FILE_SHARE_READ);
        
        /// <summary>
        /// This command sets the operating status of the solenoid 
        /// </summary>
        public static uint IOCTL_HtGSSetSolenoid = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2050,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);

        
        /// <summary>
        ///This command sets the ramp period of the solenoid 
        /// </summary>
        public static uint IOCTL_HtGSSetSolenoidRamp = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2051,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);

        
        /// <summary>
        ///This command sets the operating status of the 12VDC 
        /// </summary>
        public static uint IOCTL_HtGSSetAux12VDC = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2053,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);

        
        /// <summary>
        ///This command sets the operating status of the 5VDC 
        /// </summary>
        public static uint IOCTL_HtGSSetAux5VDC = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2054,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        ///This command sets the operating status of the Aux0 Output 
        /// </summary>
        public static uint IOCTL_HtGSSetAuxOut0 = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2055,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        ///This command sets the operating status of the Aux1 Output 
        /// </summary>
        public static uint IOCTL_HtGSSetAuxOut1 = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2056,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        ///This command sets the operating status of the LCD back light.
        /// </summary>
        public static uint IOCTL_HtGSSetBackLight = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2057,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
         
        /// <summary>
        ///This command send the display text to the LCD. 
        /// </summary>
        public static uint IOCTL_HtGSLCDtext = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2058,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        ///This command uploads the firmware version 
        /// </summary>
        public static uint IOCTL_HtGSFirmVersion = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2059,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_READ);
        
        /// <summary>
        /// This command sets the operating status of solenoid 2 
        /// </summary>
        public static uint IOCTL_HtGSSetSolenoid2 = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2060,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        ///This command sets the ramp period of solenoid 2 
        /// </summary>
        public static uint IOCTL_HtGSSetSolenoid2Ramp = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2061,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        /// This command moves the indicated servo channel according to provided parameters (returns 1 byte Boolean) 
        /// </summary>
        public static uint IOCTL_HtGSMoveServo = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2062,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);

        
        /// <summary>
        ///This gets the current position of the indicated servo channel in PWM ticks (returns 2 byte 16 bit value) 
        /// </summary>
        public static uint IOCTL_HtGSGetServo = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2063,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_READ);

        
        
        /// <summary>
        ///This gets the current value of the indicated servo channel switch (returns 1 byte Boolean true=closed)
        /// Actually do not know what this does.  Likely returns an Auxio bit.  Not used.
        /// </summary>
        public static uint IOCTL_HtGSGetServoSwitch = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2064,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_READ);

         
        /// <summary>
        ///This command sets or saves the indicated servo channel parameters. Always returns the current 
        /// </summary>
        public static uint IOCTL_HtGSSetSaveServo = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2065,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        ///This command sets the neutral position for the servo 
        /// </summary>
        public static uint IOCTL_HtGSSetNeutral = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2066,               
                                                 METHOD_BUFFERED,    
                                                 FILE_SHARE_WRITE);
        
        /// <summary>
        ///This command gets the neutral position for the servo 
        /// </summary>
        public static uint IOCTL_HtGSGetNeutral = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2067,
                                                 METHOD_BUFFERED,
                                                 FILE_SHARE_READ);
        
        /// <summary>
        ///This command uploads this driver's version 
        /// </summary>
        public static uint IOCTL_HtGSDriverVersion = CTL_CODE(FILE_DEVICE_BunnyDriver,
                                                 2068,               
                                                 METHOD_BUFFERED,   
                                                 FILE_SHARE_READ);
        #endregion defineIoctls

        #region Methods

        /// <summary>
        /// This function takes the parameters int dev, and
        /// int i_neutral. The neutral position of the servo channel 'dev'
        /// on the device is updated with the value in 'i_neutral'. The 
        /// available servo devices described by 'dev' are firmware and hardware specific.
        /// The return value of the function indicates the status of the overall function.
        /// </summary>
        /// <param name="dev">Bunny channel</param>
        /// <param name="i_neutral">Position</param>
        /// <returns>Pass/Fail</returns>
        public bool SetNeutral(int dev, int i_neutral)
        {
            bool status = false;
            try
            {
                if (Connected)
                {
                    status = setSingleShortDev(IOCTL_HtGSSetNeutral, dev, i_neutral);
                }
            }
            catch
            {
                status = false;
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameters int dev, and
        /// ref int i_position. The current servo position of the servo channel 'dev'
        /// is placed in 'i_position'. The available servo
        /// devices described by 'dev' are firmware and hardware specific.
        /// The return value of the function indicates the status of the overall function.
        /// </summary>
        /// <param name="dev">Bunny channel</param>
        /// <param name="i_position">ref position</param>
        /// <returns>Pass/Fail</returns>
        public bool GetServoPosition(int dev, ref int i_position)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    status = getSingleShortDev(IOCTL_HtGSGetServo, dev, out i_position);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        //public int hgst_get_servo_switch(int index, ref bool state)
        //{
        //    if (!loggedOn) return -1;
        //    int answer;
        //    int status = getSingleShortDev(IOCTL_HtGSGetServoSwitch, dev, out answer);
        //    state = (answer != 0) ? true : false;
        //    return status;
        //}

        private object _LcdScrollTextLockObj = new object();

        /// <summary>
        /// Adds up to 16 chars to bottom line and scrolls current chars to top line.
        /// </summary>
        public void LcdScrollText(string value)
        {
            lock (_LcdScrollTextLockObj)
            {
                string firstLine;
                if (_lastKnownLcdString.Length >= DISPLAY_SIZE / 2)
                {
                    firstLine = _lastKnownLcdString.Substring(DISPLAY_SIZE / 2, DISPLAY_SIZE / 2);
                }
                else
                {
                    firstLine = _lastKnownLcdString;
                }
                LcdText = firstLine + value;
            }
        }

        /// <summary>
        /// Property: Gets last known LCD text; Sets new text.
        /// Trunks text to 32 (DISPLAY_SIZE) chars.
        /// Adds spaces if length less than 32.
        /// </summary>
        public string LcdText
        {   
            get
            {
                return _lastKnownLcdString;
            }
            set
            {
                // Trunk if too long
                if (value.Length > DISPLAY_SIZE)
                {
                    value = value.Substring(0, DISPLAY_SIZE);
                }

                // If too small append spaces;
                string spaces = "                                ";
                if (value.Length < DISPLAY_SIZE)
                {
                    value = value + spaces.Substring(0, DISPLAY_SIZE - value.Length);
                }
                
                _SetLcdText(value);
                _lastKnownLcdString = value;
            }
        }


        /// <summary>
        /// This function takes the parameter string str. 
        /// The lcd text of the device is set to
        /// string str. The return value of the function indicates the status 
        /// of the overall function.
        /// </summary>
        /// <param name="str">string to display</param>
        /// <returns>Pass/Fail</returns>
        private bool _SetLcdText(string str)
        {
            bool status = false;
            if (Connected)
            {
                // trunk if too long
                if (str.Length > DISPLAY_SIZE)
                {
                    str = str.Substring(0, DISPLAY_SIZE);
                }

                // declare in and out arrays.
                byte[] inArray = new byte[BUFFER_SIZE];
                byte[] outArray = new byte[BUFFER_SIZE];
                // init to zero
                for (int i = str.Length; i < BUFFER_SIZE; i++)
                {
                    inArray[i] = 0;
                }
                byte[] strArray = Encoding.ASCII.GetBytes(str);
                int strSize = strArray.Length < BUFFER_SIZE ? strArray.Length : BUFFER_SIZE;
                for (int i = 0; i < strSize; i++)
                {
                    inArray[i] = strArray[i];
                }

                // spot for qty of bytes returned
                uint outSize;
                // call it
                try
                {
                    status = ExecuteIoctlCommand(IOCTL_HtGSLCDtext, inArray, ref outArray, out outSize);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameters int dev, int type, int end_pos,
        /// int max_vel and int accel. The type of "move" depends of the 'type' parameter.
        /// See Commands class for details.
        /// int dev - The servo channel the command is addressing (firmware and hardware specific).
        /// int type - The type of move command. See Commands class for details.
        /// int end_pos - The end position of the move in PMW ticks. See Commands class for details.
        /// int max_vel - The maximum velocity of the move in PMW ticks/20ms cycle. See Commands class for details.
        /// int accel - The acceleration of the move in PMW ticks/20ms cycle^2. See Commands class for details.
        /// The return value of the function indicates the status of the overall function.
        /// </summary>
        /// <param name="dev">Bunny channel</param>
        /// <param name="type">Action to perform</param>
        /// <param name="end_pos">Destination</param>
        /// <param name="max_vel">Velocity</param>
        /// <param name="accel">Acceleration</param>
        /// <returns>Pass/Fail</returns>
        public bool MoveServo(int dev, int type, int end_pos, int max_vel, int accel)
        {
            bool status = false;
            if (Connected)
            {
                // declare in and out arrays.
                byte[] inArray = new byte[BUFFER_SIZE];
                byte[] outArray = new byte[BUFFER_SIZE];
                // init to zero
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    inArray[i] = 0;
                    outArray[i] = 0;
                }

                inArray[0] = (byte)dev;
                inArray[1] = (byte)type;
                inArray[2] = (byte)(end_pos & 0xFF);
                inArray[3] = (byte)((end_pos & 0xFF00) >> 8);
                inArray[4] = (byte)(max_vel & 0xFF);
                inArray[5] = (byte)((max_vel & 0xFF00) >> 8);
                inArray[6] = (byte)(accel & 0xFF);
                inArray[7] = (byte)((accel & 0xFF00) >> 8);
                // spot for qty of bytes returned
                uint outSize;
                // call it
                try
                {
                    status = ExecuteIoctlCommand(IOCTL_HtGSMoveServo, inArray, ref outArray, out outSize);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameter bool b_state. 
        /// The 12V channel of the indexed device is set to
        /// bool b_state.The return value of the function indicates the status 
        /// of the overall function.
        /// </summary>
        /// <param name="b_state">True = ON</param>
        /// <returns>Pass/Fail</returns>
        public bool Set12vdc(bool b_state)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    status = setSingleBool(IOCTL_HtGSSetAux12VDC, b_state);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameter bool b_state. 
        /// The 5V channel of the indexed device is set to
        /// bool b_state. The return value of the function indicates the status 
        /// of the overall function.
        /// </summary>
        /// <param name="b_state">True = ON</param>
        /// <returns>Pass/Fail</returns>
        public bool Set5vdc(bool b_state)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    status = setSingleBool(IOCTL_HtGSSetAux5VDC, b_state);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Property get and set Card power.
        /// </summary>
        public bool CardPower
        {
            get
            {
                int i_status = 0;
                GetDeviceStatus(ref i_status, false);
                return ((i_status & ((int)EnumBunnyStatusBits.AUX_12VDC | (int)EnumBunnyStatusBits.AUX_5VDC)) == ((int)EnumBunnyStatusBits.AUX_12VDC | (int)EnumBunnyStatusBits.AUX_5VDC));
            }
            set
            {
                Set12vdc(value);
                Set5vdc(value);
            }
        }

        /// <summary>
        /// This function takes the parameter bool b_state. 
        /// The aux out 0 channel of the indexed device is set to
        /// bool b_state. The return value of the function indicates the status 
        /// of the overall function.
        /// </summary>
        /// <param name="b_state">True = High</param>
        /// <returns>Pass/Fail</returns>
        public bool SetAuxOut0(bool b_state)        
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    status = setSingleBool(IOCTL_HtGSSetAuxOut0, b_state);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameter bool b_state. 
        /// The aux out 1 channel of the indexed device is set to
        /// bool b_state. The return value of the function indicates the status 
        /// of the overall function.
        /// </summary>
        /// <param name="b_state">True = High</param>
        /// <returns>Pass/Fail</returns>
        public bool SetAuxOut1(bool b_state)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    status = setSingleBool(IOCTL_HtGSSetAuxOut1, b_state);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameter bool b_state. 
        /// The lcd back light channel of the indexed device is set to
        /// bool b_state. The return value of the function indicates the status 
        /// of the overall function.
        /// </summary>
        /// <param name="b_state">True = ON</param>
        /// <returns>Pass/Fail</returns>
        public bool SetBackLight(bool b_state)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    status = setSingleBool(IOCTL_HtGSSetBackLight, b_state);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameters int dev, and
        /// ref int i_neutral. The current servo neutral position of the servo channel 'dev'
        /// is placed in 'i_neutral'. The available servo
        /// devices described by 'dev' are firmware and hardware specific.
        /// The return value of the function indicates the status of the overall function.
        /// </summary>
        /// <param name="dev">Bunny channel</param>
        /// <param name="i_neutral">ref Position</param>
        /// <returns>Pass/Fail</returns>
        public bool GetNeutral(int dev, ref int i_neutral)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    status = getSingleShortDev(IOCTL_HtGSGetNeutral, dev, out i_neutral);
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>                          		                            				                
        /// This function is used to set and save RAM and ROM parameters for servo channels. For details, see the
        /// Commands class from SEG13 bunny firmware.
        /// The return value of the function indicates the status of the overall function.
        /// </summary>
        /// <param name="dev">Bunny channel</param>
        /// <param name="type">Action to perform</param>
        /// <param name="open_end_pos"></param>
        /// <param name="open_max_vel"></param>
        /// <param name="open_accel"></param>
        /// <param name="close_end_pos"></param>
        /// <param name="close_max_vel"></param>
        /// <param name="close_accel"></param>
        /// <param name="current_end_pos"></param>
        /// <param name="current_max_vel"></param>
        /// <param name="current_accel"></param>
        /// <returns>Pass/Fail</returns>
        public bool SetSaveServo(int dev, int type,
				                 ref int open_end_pos, ref int open_max_vel, ref int open_accel, 
						         ref int close_end_pos, ref int close_max_vel, ref int close_accel, 
						         ref int current_end_pos, ref int current_max_vel, ref int current_accel )
        {
            bool status = false;
            if (!Connected) return status;
            
            // declare in and out arrays.
            byte[] inArray = new byte[BUFFER_SIZE];
            byte[] outArray = new byte[BUFFER_SIZE];
            // init to zero
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                inArray[i] = 0;
                outArray[i] = 0;
            }

            // Initialize input array.
            inArray[0] = (byte)(dev & 0xFF);
            inArray[1] = (byte)(type & 0xFF);
            inArray[2] = (byte)(open_end_pos & 0xFF);
            inArray[3] = (byte)(open_end_pos >> 8);
            inArray[4] = (byte)(open_max_vel & 0xFF);
            inArray[5] = (byte)(open_max_vel >> 8);
            inArray[6] = (byte)(open_accel & 0xFF);
            inArray[7] = (byte)(open_accel >> 8);
            inArray[8] = (byte)(close_end_pos & 0xFF);
            inArray[9] = (byte)(close_end_pos >> 8);
            inArray[10] = (byte)(close_max_vel & 0xFF);
            inArray[11] = (byte)(close_max_vel >> 8);
            inArray[12] = (byte)(close_accel & 0xFF);
            inArray[13] = (byte)(close_accel >> 8);
            inArray[14] = (byte)(current_end_pos & 0xFF);
            inArray[15] = (byte)(current_end_pos >> 8);
            inArray[16] = (byte)(current_max_vel & 0xFF);
            inArray[17] = (byte)(current_max_vel >> 8);
            inArray[18] = (byte)(current_accel & 0xFF);
            inArray[19] = (byte)(current_accel >> 8);

            // spot for qty of bytes returned
            uint outSize;
            // call it
            try
            {
                status = ExecuteIoctlCommand(IOCTL_HtGSSetSaveServo, inArray, ref outArray, out outSize);
            }
            catch
            {
                status = false;
            }

            // Parse output array for final values.
            open_end_pos = outArray[0] | (outArray[1] << 8);
            open_max_vel = outArray[2] | (outArray[3] << 8);
            open_accel = outArray[4] | (outArray[5] << 8);
            close_end_pos = outArray[6] | (outArray[7] << 8);
            close_max_vel = outArray[8] | (outArray[9] << 8);
            close_accel = outArray[10] | (outArray[11] << 8);
            current_end_pos = outArray[12] | (outArray[13] << 8);
            current_max_vel = outArray[14] | (outArray[15] << 8);
            current_accel = outArray[16] | (outArray[17] << 8);
        
            return status;
        }

        /// <summary>
        /// This function takes the parameters int sol_dev 
        /// and bool b_state. The solenoid channel 'sol_dev' of the device at 'index' 
        /// is set to the state 'b_state'. Limitation of this function are firmware
        /// and hardware specific.The return value of the function indicates the status 
        /// of the overall function.
        /// </summary>
        /// <param name="dev"></param>
        /// <param name="b_state"></param>
        /// <returns>Pass/Fail</returns>
        public bool SetSolenoid(int dev, bool b_state)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    if (dev == 0)
                    {
                        status = setSingleBool(IOCTL_HtGSSetSolenoid, b_state);
                    }
                    else
                    {
                        status = setSingleBool(IOCTL_HtGSSetSolenoid2, b_state);
                    }
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// This function takes the parameters int sol_dev 
        /// and int i_value. The ramp value of solenoid channel 'sol_dev' of the 
        /// device at 'index'is set to the state 'b_state'. Limitation of this function 
        /// are firmware and hardware specific.The return value of the function
        /// indicates the status of the overall function.
        /// </summary>
        /// <param name="dev"></param>
        /// <param name="i_value"></param>
        /// <returns>Pass/Fail</returns>
        public bool SetSolenoidRampRate(int dev, int i_value)
        {
            bool status = false;
            if (Connected)
            {
                try
                {
                    if (dev == 0)
                    {
                        status = setSingleShort(IOCTL_HtGSSetSolenoidRamp, i_value);
                    }
                    else
                    {
                        status = setSingleShort(IOCTL_HtGSSetSolenoid2Ramp, i_value);
                    }
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Public property returns firmware version.
        /// </summary>
        public string GetFirmwareVersion
        {
            get
            {
                lock (_GetVerLockObj)
                {
                    string str = String.Empty;
                    bool status = _GetFirmwareVersion(ref str);
                    if (status)
                    {
                        return str;
                    }
                    else
                    {
                        return "Off-Line";
                    }
                }
            }
        }

        private object _GetVerLockObj = new object();
        private int m_GetFirmwareVersionRetryCount = 0;

        /// <summary>
        /// Gets the string firmware version and places it in ref string str.
        /// </summary>
        /// <param name="str">ref firmware version string</param>
        /// <returns>Pass/Fail</returns>
        private bool _GetFirmwareVersion(ref string str)
        {
            bool status = false;
            try
            {
                // Avoid too much recursion.
                if (m_GetFirmwareVersionRetryCount > 4) return status;
                m_GetFirmwareVersionRetryCount++;

                try
                {
                    status = getString(IOCTL_HtGSFirmVersion, out str);
                }
                catch
                {
                    status = false;
                }

                if (status)
                {
                    // End of line (string) varies for various versions
                    // So check everything that we know of.
                    int eolStrLen = str.IndexOf("\r");
                    if (eolStrLen < 0) eolStrLen = 10;
                    int zeroLen = str.IndexOf("\0");
                    if (zeroLen < 0) zeroLen = 10;
                    int questionStrLen = str.IndexOf("?");
                    if (questionStrLen < 0) questionStrLen = 10;
                    if (zeroLen < eolStrLen) eolStrLen = zeroLen;
                    if (questionStrLen < eolStrLen) eolStrLen = questionStrLen;
                    if (eolStrLen > 6) eolStrLen = 6;
                    str = str.Substring(0, eolStrLen);
                }

                // Some versions of firmware return only a few chars on some calls.
                // Some versions of firmware fail on the first call.
                // Mask random firmware bugs by trying again.
                if (!status || str.Length < 5)
                {
                    Thread.Sleep(25);
                    status = _GetFirmwareVersion(ref str);
                }
            }
            finally
            {
                m_GetFirmwareVersionRetryCount = 0;
            }
            return status;
        }

        /// <summary>
        /// Public property gets kernel drive rversion.
        /// </summary>
        public string GetDriverVersion
        {
            get
            {
                lock (_GetVerLockObj)
                {
                    string str = String.Empty;

                    bool status = _GetDriverVersion(ref str);
                    if (status)
                    {
                        return str;
                    }
                    else
                    {
                        return "Off-Line";
                    }
                }
            }
        }

        /// <summary>
        /// This function places the string driver version in 
        /// the passed string reference. 
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Pass/Fail</returns>
        private bool _GetDriverVersion(ref string str)
        {
            lock (_GetVerLockObj)
            {
                bool status = false;
                try
                {
                    status = getString(IOCTL_HtGSDriverVersion, out str);
                }
                catch
                {
                    status = false;
                }
                int count = 0;
                int maxTries = 10;
                int delayTime = 1000;
                // Wait until the board wakes up.
                while (!status && count < maxTries)
                {
                    count++;
                    Thread.Sleep(delayTime);
                    try
                    {
                        status = getString(IOCTL_HtGSDriverVersion, out str);
                    }
                    catch
                    {
                        status = false;
                    }
                }
                if (status)
                {
                    int strlen = str.IndexOf("\0");
                    str = str.Substring(0, strlen);
                }
                return status;
            }
        }

        /// <summary>
        /// This function takes a reference to an int. The status of the device is 
        /// retrieved and written to the ref int. The return value of the 
        /// function indicates the status of the overall function.
        /// These are the status bits (like what is on and what is off).
        /// </summary>
        /// <param name="i_status"></param>
        /// <returns>Pass/Fail</returns>
        public bool GetDeviceStatus(ref int i_status, bool force)
        {
            bool status = true;
            // If value is fairly new, return last known value.
            if (!force && _lastStatusTime + TimeSpan.FromMilliseconds(50.0) > DateTime.Now)
            {
                i_status = _status;
            }
            else // if stale then we read.
            {
                try
                {
                    if (Connected)
                    {
                        status = getSingleShort(IOCTL_HtGSUploadStatus, out i_status);
                        if (!status || i_status == 0)  //if (broke) or (possibly invalid) then try again
                        {
                            status = getSingleShort(IOCTL_HtGSUploadStatus, out i_status);
                        }
                        _status = i_status;
                        _lastStatusTime = DateTime.Now;
                    }
                    else
                    {
                        status = false;
                    }
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Read some string value from kernel driver.
        /// </summary>
        /// <param name="theIoCtl"></param>
        /// <param name="theString"></param>
        /// <returns></returns>
        private bool getString(uint theIoCtl, out string theString)
        {
            // Declare in and out arrays.
            byte[] inArray = new byte[BUFFER_SIZE];
            byte[] outArray = new byte[BUFFER_SIZE];
            // Init to zero.
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                inArray[i] = 0;
            }
            // For qty of bytes returned.
            uint outSize;
            // ask the bunny
            bool status = ExecuteIoctlCommand(theIoCtl, inArray, ref outArray, out outSize);
            if (status)
            {
                theString = Encoding.ASCII.GetString(outArray);
            }
            else
            {
                theString = "";
            }
            return status;
        }

        /// <summary>
        /// Read a short value (2 bytes; SEG13 made everything 2 bytes).
        /// </summary>
        /// <param name="theIoCtl"></param>
        /// <param name="dev"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool getSingleShortDev(uint theIoCtl, int dev, out int value)
        {
            // declare in and out arrays.
            byte[] inArray = new byte[BUFFER_SIZE];
            byte[] outArray = new byte[BUFFER_SIZE];
            // init to zero
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                inArray[i] = 0;
                outArray[i] = 0;
            }
            inArray[0] = (byte)(dev & 0xFF);
            // spot for qty of bytes returned
            uint outSize;
            // call it
            bool status = ExecuteIoctlCommand(theIoCtl, inArray, ref outArray, out outSize);
            // copy output array byte to pData
            value = (int)(outArray[0] | (outArray[1] << 8));
            return status;
        }

        /// <summary>
        /// Read a short value (2 bytes; SEG13 made everything 2 bytes).
        /// </summary>
        /// <param name="theIoCtl"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool getSingleShort(uint theIoCtl, out int value)
        {
            // declare in and out arrays.
            byte[] inArray = new byte[BUFFER_SIZE];
            byte[] outArray = new byte[BUFFER_SIZE];
            // init to zero
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                inArray[i] = 0;
                outArray[i] = 0;
            }
            // spot for qty of bytes returned
            uint outSize;
            // call it
            bool status = ExecuteIoctlCommand(theIoCtl, inArray, ref outArray, out outSize);
            // copy output array byte to pData
            value = (int)(outArray[0] | (outArray[1] << 8));
            return status;
        }

        /// <summary>
        /// Write a short value (2 bytes; SEG13 made everything 2 bytes).
        /// </summary>
        /// <param name="theIoCtl"></param>
        /// <param name="dev"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool setSingleShortDev(uint theIoCtl, int dev, int value)
        {
            // Declare in and out arrays.
            byte[] inArray = new byte[BUFFER_SIZE];
            byte[] outArray = new byte[BUFFER_SIZE];
            // Init to zero.
            for (int i = 2; i < BUFFER_SIZE; i++)
            {
                inArray[i] = 0;
            }
            // For qty of bytes returned.
            uint outSize;
            // set state
            inArray[0] = (byte)(dev & 0xFF);
            inArray[1] = (byte)(value & 0xFF);
            inArray[2] = (byte)(value >> 8);
            bool status = ExecuteIoctlCommand(theIoCtl, inArray, ref outArray, out outSize);
            return status;
        }

        /// <summary>
        /// Write a short value (2 bytes; SEG13 made everything 2 bytes).
        /// </summary>
        /// <param name="theIoCtl"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool setSingleShort(uint theIoCtl, int value)
        {
            // Declare in and out arrays.
            byte[] inArray = new byte[BUFFER_SIZE];
            byte[] outArray = new byte[BUFFER_SIZE];
            // Init to zero.
            for (int i = 2; i < BUFFER_SIZE; i++)
            {
                inArray[i] = 0;
            }
            // For qty of bytes returned.
            uint outSize;
            // set state
            inArray[0] = (byte)(value & 0xFF);
            inArray[1] = (byte)(value >> 8);
            bool status = ExecuteIoctlCommand(theIoCtl, inArray, ref outArray, out outSize);
            return status;
        }

        /// <summary>
        /// Set some bool value in kernel driver.
        /// </summary>
        /// <param name="theIoCtl"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool setSingleBool(uint theIoCtl, bool state)
        {
            // Declare in and out arrays.
            byte[] inArray = new byte[BUFFER_SIZE];
            byte[] outArray = new byte[BUFFER_SIZE];
            // Init to zero.
            for (int i = 1; i < BUFFER_SIZE; i++)
            {
                inArray[i] = 0;
            }
            // For qty of bytes returned.
            uint outSize;
            // set state
            inArray[0] = (byte)(state ? 0x01 : 0x00);
            bool status = ExecuteIoctlCommand(theIoCtl, inArray, ref outArray, out outSize);
            return status;
        }

        /// <summary>
        /// This function converts firmware version to a scaled integer value.
        /// Copied from SEG13.
        /// </summary>
        /// <param name="ver"></param>
        /// <returns>Pass/Fail</returns>
        private bool convert_ver_to_string(ref string ver)
        {
            string l_ver;
            int i = 0;

            try
            {
                if (ver != null)
                {
                    l_ver = ver.Substring(1, 5);
                    float l_flt = (float)System.Convert.ToSingle(l_ver);
                    i = (int)(l_flt * 100);
                    ver = ver.Substring(0, 1) + i.ToString();
                }
                else
                {
                    return false;
                }
            }
            catch 
            {
                return false;
            }

            return true;
        }
 
        /// <summary>
        /// Read data files
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string ReadDataFiles(string fileName)
        {
            FileStream fs = null;
            StreamReader sr = null;

            string retStr = "File not found.";
            const int interval = 100;
            int count = 0;
            int countMax = 20;  // time-out.  4 x (25 + exception-timeout)
            // while wait for Blade's flash to show up
            while (count < countMax)
            {
                try
                {
                    // open the file.
                    fs = new FileStream(System.IO.Path.Combine(Flash, fileName), FileMode.Open, FileAccess.Read, FileShare.Read);
                    sr = new StreamReader(fs);
                    retStr = sr.ReadLine();
                    break;
                }
                catch (DirectoryNotFoundException e)
                {
                    System.Threading.Thread.Sleep(interval);
                    count++;
                    if (count >= countMax)
                    {
                        throw new Exception(System.IO.Path.Combine(Flash, fileName), e);
                    }
                }
                catch (FileNotFoundException e)
                {
                    throw new Exception(System.IO.Path.Combine(Flash, fileName), e);
                }
                catch (Exception e)
                {
                    throw new Exception(System.IO.Path.Combine(Flash, fileName), e);
                }
                finally
                {
                    if (sr != null)
                    {
                        try { sr.Close(); }
                        catch { }
                        try { sr.Dispose(); }
                        catch { }
                    }
                    if (fs != null)
                    {
                        try { fs.Close(); }
                        catch { }
                        try { fs.Dispose(); }
                        catch { }
                    }
                }
            }
            return retStr;
        } // end readDataFiles

        /// <summary>
        /// Write Blade data files..
        /// </summary>
        /// <param name="key"></param>
        /// <param name="serialNumber"></param>
        public void WriteDataFiles(string fileName, string data)
        {
            FileStream fs = null;
            StreamWriter sw = null; ;
            try
            {
                // open the file.
                fs = new FileStream(System.IO.Path.Combine(Flash, fileName), FileMode.Create, FileAccess.Write, FileShare.Read);
                sw = new StreamWriter(fs);
                sw.WriteLine(data);
            }
            catch
            { }  // please leave this catch here.
            finally
            {
                if (sw != null)
                {
                    try { sw.Flush(); }
                    catch { }
                    try { sw.Close(); }
                    catch { }
                    try { sw.Dispose(); }
                    catch { }
                }
                if (fs != null)
                {
                    try { fs.Flush(); }
                    catch { }
                    try { fs.Close(); }
                    catch { }
                    try { fs.Dispose(); }
                    catch { }
                }
            }
            return;
        } // end writeDataFiles


       #endregion Methods
        
    }
}
