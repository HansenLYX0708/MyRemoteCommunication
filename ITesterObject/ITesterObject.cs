﻿// ==========================================================================================
// Copyright ©                                                       
//                                                                                          
// Classification           :                  
// Date                     :                                               
// Author                   : Hansen Liu                                             
// Purpose                  : 
// ==========================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

namespace Hitachi.Tester.Module
{
    [ServiceContract(Name = "TesterObjectContract",
        Namespace = "Hitachi.Tester.Module",
        CallbackContract = typeof(ITesterObjectCallback))]
    public interface ITesterObject
    {
        #region part one
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
        #endregion part one

        #region part two
        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        string Ping(string message);

        [OperationContract]
        [FaultContract(typeof(ReceiverFaultDetail))]
        [FaultContract(typeof(SenderFaultDetail))]
        void SetConfig(string NewConfig);
        #endregion part two

    } // end interface

    // These event handlers are used inside of Jade and BladeRunner.
    public delegate void StatusEventHandler(object sender, StatusEventArgs e);
    // TODO :
    //public delegate void CompleteEventHandler(object sender, CompletedEventArgs e);
    //public delegate void StartedEventHandler(object sender, StartedEventArgs e);

    // The following event handler is used to send all events across the wire.
    // Actually we no longer do .NET remoting events.  Instead we use a WCF callback.
    // Blade runner wraps this in a callback.
    public delegate void BladeEventHandler(object sender, BladeEventArgs e);
}
