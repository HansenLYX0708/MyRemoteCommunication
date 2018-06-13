using System;
using System.Runtime.Remoting.Messaging;

namespace Hitachi.Tester.Module
{
    /// <summary>
    /// Summary description for AbstractEventClass.
    /// </summary>
    public abstract class AbstractEventClass : MarshalByRefObject
    {
        #region Fields

        #endregion Fields

        #region Constructors
        public AbstractEventClass()
        {
        }
        #endregion Constructors

        #region Properties

        #endregion Properties

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
