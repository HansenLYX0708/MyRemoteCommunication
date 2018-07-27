// ==========================================================================================
// Copyright ©                                                       
//                                                                                          
// Classification           :                  
// Date                     :                                               
// Author                   : Hansen Liu                                             
// Purpose                  : 
// ==========================================================================================
using System.ServiceModel;

namespace WD.Tester.Module
{
    [ServiceContract(
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(ITesterObjectCallback))]
    public interface ITesterObjectCallback
    {
        [OperationContract(IsOneWay = true)]
        void BladeEventCallback(BladeEventArgs e);
    }
}
