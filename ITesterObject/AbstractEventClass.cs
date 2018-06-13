// ==========================================================================================
// Copyright ©                                                       
//                                                                                          
// Classification           :                  
// Date                     :                                               
// Author                   : Hansen Liu                                             
// Purpose                  : 
// ==========================================================================================
using System;

namespace Hitachi.Tester.Module
{
    /// <summary>
    /// Summary description for AbstractEventClass.
    /// </summary>
    public abstract class AbstractEventClass : MarshalByRefObject
    {
        #region Methods
        //[OneWay]
        public void StatusCallback(object sender, StatusEventArgs e)
        {
            InternalStatusCallback(sender, e);
        }

        //[OneWay]
        public void BladeCallback(object sender, BladeEventArgs e)
        {
            InternalBladeCallback(sender, e);
        }

        public abstract void InternalStatusCallback(object sender, StatusEventArgs e);
        public abstract void InternalBladeCallback(object sender, BladeEventArgs e);
        #endregion Methods
    }
}
