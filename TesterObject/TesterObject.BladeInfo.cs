using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hitachi.Tester.Module
{
    public partial class TesterObject : ITesterObject
    {
        #region Fields
        private string _MyLocation = string.Empty;
        private string _JadeSn = string.Empty;
        #endregion Fields

        #region Properties
        public string Name()
        {
            return "FACT BladeRunner";
        }

        public string MyLocation
        {
            get 
            { 
                if (_MyLocation.Length == 0 || _MyLocation.Trim().ToLower().Contains("none"))
                {
                    try
                    {
                        //_MyLocation = ;
                    }
                    catch (System.Exception ex)
                    {
                    	//SendBunnyEvent(this, new StatusEventArgs());
                    }
                }
                return _MyLocation; }
            set 
            { 
                _MyLocation = value;
            }
        }
        #endregion Properties

        #region Methods

        #endregion Methods

    }
}
