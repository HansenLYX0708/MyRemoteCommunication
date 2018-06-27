using System;

// ReSharper disable once CheckNamespace
namespace HGST.Blades
{
    /// <summary>
    /// Bunny board class
    /// </summary>
    public partial class BunnyBoard : USB.Device<BunnyBoard>   
    {

        private string _flash;
        private string _physicalDrive;
        private string _bunnyPath;
        private Guid _id;
        private UInt16 _vendor;
        private UInt16 _product;

        private string _pnpDeviceID;
        private string _queryID;

        private int _status;
        private DateTime _lastStatusTime;

        private string _lastKnownLcdString;

        #region Constructors


        /// <summary>
        /// Constructor takes string path (PNP path) and node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="path"></param>
        public BunnyBoard(int node, string path)
            : base(node, path)
        {
           // _RefreshThreadGoing = false;
            _flash = String.Empty;
            _bunnyPath = String.Empty;
            _physicalDrive = string.Empty;
            _id = new Guid(0x3dd26298, 0x8e9e, 0x45dc, 0x94, 0x3d, 0x4b, 0x33, 0xfe, 0x0e, 0x85, 0x67);
            _vendor = 0xEFEF;
            _product = 0x1;
            _pnpDeviceID = path;
            _queryID = string.Empty;
            _status = 0;
            _lastStatusTime = DateTime.MinValue;
            _lastKnownLcdString = "                                ";

            _GetFlash();
        }


        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return "Bunny controller"; }
        }

        #endregion Properties

    }
}
