using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hitachi.Tester.Module
{
    public class StopGoPauseState
    {
        #region Fields
        public volatile bool NowTestsArePaused;
        public volatile bool PauseTests;
        private bool _PauseEvents;
        public volatile bool CmdBusy;
        public volatile bool PleaseStop;
        private bool _SeqGoing;
        #endregion Fields

        #region Constructors
        public StopGoPauseState()
        {
            NowTestsArePaused = false;
            PauseTests = false;
            _PauseEvents = false;
            CmdBusy = false;
            PleaseStop = false;
            _SeqGoing = false;
        }
        #endregion Constructors

        #region Prop

        public bool PauseEvents
        {
            get { return _PauseEvents; }
            set { _PauseEvents = value; }
        }

        public bool SeqGoing
        {
            get { return _SeqGoing; }
            set { _SeqGoing = value; }
        }

        //public volatile bool NowTestsArePaused
        //{
        //    get { return _NowTestsArePaused; }
        //    set { _NowTestsArePaused = value; }
        //}

        //public volatile bool PauseTests
        //{
        //    get { return _PauseTests; }
        //    set { _PauseTests = value; }
        //}

        //public volatile bool CmdBusy
        //{
        //    get { return _CmdBusy; }
        //    set { _CmdBusy = value; }
        //}

        //public volatile bool PleaseStop
        //{
        //    get { return _PleaseStop; }
        //    set { _PleaseStop = value; }
        //}
        #endregion Properties

        #region Methods
        public void Assign(StopGoPauseState that)
        {
            this.CmdBusy = that.CmdBusy;
            this.PleaseStop = that.PleaseStop;
            this._SeqGoing = that._SeqGoing;
            this._PauseEvents = that._PauseEvents;
            this.PauseTests = that.PauseTests;
            this.NowTestsArePaused = that.NowTestsArePaused;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.CmdBusy ? "" : "!");
            sb.Append("Busy ");
            sb.Append(this.PleaseStop ? "" : "!");
            sb.Append("PleaseStop ");
            sb.Append(this._SeqGoing ? "" : "!");
            sb.Append("SeqGoing ");
            sb.Append(this._PauseEvents ? "" : "!");
            sb.Append("PauseEvents ");
            sb.Append(this.PauseTests ? "" : "!");
            sb.Append("PauseTests ");
            sb.Append(this.NowTestsArePaused ? "" : "!");
            sb.Append("NowTestsArePaused ");

            return sb.ToString();
        }
        #endregion Methods 
    }
}
