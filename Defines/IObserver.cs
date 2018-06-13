// ****************************************************************
// IObserver.cs This interface is used to communicate between the server and
// the remote object.
//
// Hitachi Parametrics tester
// Copyright 2007
// Robert L. Kimball
// ****************************************************************
using System;

namespace Hitachi.Tester.Module
{

    /// <summary>
	/// This sends messages from the Remote object to the server hosting the remote 
    ///  object.  This came from the CodeProject.
	/// </summary>
    public interface IObserver
    {
        void Notify(string text);
        void NotifyContent(string text);
        void OnExitDelegate(Delegate thisOne);
        string GetCurrentCultureString(string whichStr);
        Hitachi.Tester.Enums.ReturnValues LogInValueProp { set; }
        void BladeEvent(object[] BladeEventArgsObject);
        event ObjObjDelegate SendToTesterObjectEvent;

	}

    public delegate object ObjObjDelegate(object[] argsArray);

}
