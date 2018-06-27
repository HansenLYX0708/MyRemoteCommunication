using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Management;

using HGST.USB;


namespace HGST.Blades
{
    /// <summary>
    /// An instance of a Bunny board
    /// </summary>
    public partial class BunnyBoard //: Device<BunnyBoard>   
    {

        #region Properties

        /// <summary>
        /// GUID
        /// </summary>
        public override Guid ID
        {
            get { return _id; }
        }

        /// <summary>
        /// Vendor
        /// </summary>
        public override uint Vendor
        {
            get { return _vendor; }
        }

        /// <summary>
        /// Product
        /// </summary>
        public override uint Product
        {
            get { return _product; }
        }

        /// <summary>
        /// Drive letter of the flash ie F:
        /// </summary>
        public string Flash
        {
            get
            {
                if (_flash == String.Empty)
                {
                    _GetFlash();
                }

                return _flash;
            }
        }

        /// <summary>
        /// Kernel's Symbolic name for the drive.
        /// Looks like ////.//PhysicalDrive1 not the GUID one.
        /// </summary>
        public string PhysicalDrive
        {
            get
            {
                if (_physicalDrive == String.Empty)
                {
                    _GetFlash();
                }

                return _physicalDrive;
            }
        }

        /// <summary>
        /// Global?? symbolic name from SetupDIxxx
        /// This is the one with the GUID.
        /// </summary>
        public string BunnyPath
        {
            get
            {
                if (_bunnyPath == String.Empty)
                {
                    _GetFlash();
                }
                return _bunnyPath;
            }
            set
            {
                _bunnyPath = value;
            }
        }



        #endregion

        #region Methods

        /// <summary>
        /// Matches the Bunny Flash drive with the Bunny IO.
        /// </summary>
        private void _GetFlash()
        {
            IntPtr devices = IntPtr.Zero;
            Int32 bufferSize = 0;
            IntPtr detailDataBuffer = IntPtr.Zero;

            // Retrieve a list of of plugged-in devices matching our class GUID (DIGCF_PRESENT indicates only devices which are plugged in)
            devices = GetClassDevices(ref _id, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            try
            {
                // Step through the list of devices and generate new Device objects as necessary
                int index = 0;
                DeviceInterfaceData deviceInterfaceData = new DeviceInterfaceData();
                deviceInterfaceData.Size = Marshal.SizeOf(deviceInterfaceData);

                // Loop until we find this instance.
                while (EnumerateDeviceInterfaces(devices, IntPtr.Zero, ref _id, index, ref deviceInterfaceData))
                {
                    int node = 0;

                    SP_DEVINFO_DATA spData = new SP_DEVINFO_DATA();
                    spData.cbSize = Marshal.SizeOf(spData);
                    SetupDiEnumDeviceInfo(devices, index, ref spData);

                    // Get the bunny board device node for this index
                    node = spData.DevInst;

                    index++;
                    // Is this node equal to the instance node?
                    if (node != Node) continue; // Nope - try again.

                    // Reset our location in the device hierarchy to the first child of the parent
                    _Node = GetParentIndex(Node);
                    _Node = GetChildIndex(_Node);

                    // Find the flash device node
                    int previous = _Node;
                    bool passed = false;
                    do
                    {
                        try
                        {
                            GetChildIndex(previous);
                            _Node = GetChildIndex(previous);
                            passed = true;
                        }
                        catch
                        {
                            previous = GetSiblingIndex(previous);
                        }
                    }
                    while (!passed);

                    _pnpDeviceID = GetDeviceID(_Node);
                    _queryID = _pnpDeviceID.Replace("\\", "\\\\");

                    // Find the WMI disk drive device ID of the flash (there will only be one at maximum)
                    ManagementObject drive = null;
                    string query = "SELECT * FROM Win32_DiskDrive WHERE PNPDeviceID='" + _queryID + "'";
                    ManagementObjectCollection drives = null;
                    try
                    {
                        drives = new ManagementObjectSearcher(query).Get();
                    }
                    catch
                    {
                        // did not work this time so we will try it again later.   
                    }
                    if(drives == null) return;
                   
                    if (drives.Count < 1)
                    {
                        throw new Exception("No matching WMI device ID could be found for the Bunny board's PNPDeviceID.");
                    }
                    foreach (ManagementObject candidate in drives) { drive = candidate; }

                    _physicalDrive = drive["DeviceID"].ToString();

                    // Find the drive partitions associated with the WMI disk drive device ID
                    string label = String.Empty;
                    query = "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + drive["DeviceID"] +
                            "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
                    ManagementObjectCollection partitions = new ManagementObjectSearcher(query).Get();
                    if (partitions.Count < 1)
                    {
                        throw new Exception("The bunny board flash has not been partitioned.");
                    }

                    // Find a logical disk from the partitions associated with the drive
                    ManagementObject flash = null;
                    foreach (ManagementObject partition in partitions)
                    {
                        // Find the logial disk associate with the drive partition
                        query = "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partition["DeviceID"] +
                                "'} WHERE AssocClass = Win32_LogicalDiskToPartition";
                        ManagementObjectCollection disks = new ManagementObjectSearcher(query).Get();
                        foreach (ManagementObject candidate in disks) { flash = candidate; }
                    }
                    if (flash == null)
                    {
                        throw new Exception("No logical disks are associated with any of the Bunny Board's partitions.");
                    }

                    _flash = flash["DeviceID"].ToString();

                    // Determine the size of the buffer to allocate to retreive all the device info
                    GetDeviceInterface(devices, ref deviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, IntPtr.Zero);

                    // Allocate memory for the buffer
                    detailDataBuffer = Marshal.AllocHGlobal(bufferSize);
                    Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                    // Copy the device interface data into the newly created buffer
                    if (!GetDeviceInterface(devices, ref deviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, IntPtr.Zero))
                    {
                        throw new Exception("Failed to copy the device interface details.");
                    }

                    // Get the device path
                    BunnyPath = Marshal.PtrToStringAuto(new IntPtr(detailDataBuffer.ToInt32() + 4)).ToLower();

                    // Free the device detail buffer
                    Marshal.FreeHGlobal(detailDataBuffer);
                    detailDataBuffer = IntPtr.Zero;
                } // end while each instance
            } // end try
            finally
            {
                // Clean up the unmanaged memory allocations
                if (detailDataBuffer != IntPtr.Zero)
                {
                    // Free the memory allocated previously by AllocHGlobal.
                    Marshal.FreeHGlobal(detailDataBuffer);
                }

                if (devices != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(devices);
                }
            } // end finally
        }  // end _GetFlash method

  

        #endregion
    }
}
