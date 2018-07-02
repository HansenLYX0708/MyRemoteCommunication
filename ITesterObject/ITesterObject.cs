// ==========================================================================================
// Copyright ©                                                       
//                                                                                          
// Classification           :                  
// Date                     :                                               
// Author                   : Hansen Liu                                             
// Purpose                  : 
// ==========================================================================================
using System;

using System.ServiceModel;

using Hitachi.Tester.Enums;

namespace Hitachi.Tester.Module
{
    [ServiceContract(Name = "TesterObjectContract",
        Namespace = "Hitachi.Tester.Module",
        CallbackContract = typeof(ITesterObjectCallback))]
    public interface ITesterObject
    {
        #region base function
        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        UInt32 Connect(string userID, string password, string computerName);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void Disconnect(string userID, string computerName);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        int PingInt();

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        string Ping(string message);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        FileNameStruct[] BladeFileDir(string path, string Filter);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        bool CopyFileOnBlade(string fromFile, string toFile);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        bool BladeDelFile(string Key, string FileName);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void SafeRemoveBlade();

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        bool BunnyReInit();

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        TesterState GetModuleState();

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void SetModuleState(TesterState testerState);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void GetBunnyStatus();

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        MemsStateValues GetMemsStatus();

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void SetStrings(string key, string[] names, string[] strings);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        string[] GetStrings(string key, string[] names);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void SetIntegers(string key, string[] names, int[] numbers);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        int[] GetIntegers(string key, string[] names);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void PinMotionToggle();

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        string Name();

        #endregion base function

        #region Tester function
        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void SetConfig(string NewConfig);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void GetConfig(string TestName);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        bool DelConfig(string Key, string TestName);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void StartTest(string ParseString, string TestName, string GradeName, string tableStr);
        #endregion Tester function


        // TODO :
        #region part three

        #endregion part three

        #region part three

        #endregion part three

        #region part three

        #endregion part three

    } // end interface

    // These event handlers are used inside of Jade and BladeRunner.
    public delegate void StatusEventHandler(object sender, StatusEventArgs e);
    // TODO :
    //public delegate void CompleteEventHandler(object sender, CompletedEventArgs e);
    //public delegate void StartedEventHandler(object sender, StartedEventArgs e);

    // The following event handler is used to send all events across the wire. Actually we no longer do .NET remoting events.  Instead we use a WCF callback.
    // Blade runner wraps this in a callback.
    // !!be used in server
    public delegate void BladeEventHandler(object sender, BladeEventArgs e);
}
