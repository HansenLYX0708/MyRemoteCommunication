﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.269
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Hitachi.Tester.Client.ServiceReference2 {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="Hitachi.Tester.Module", ConfigurationName="ServiceReference2.TesterObjectStreamingContract")]
    public interface TesterObjectStreamingContract {
        
        [System.ServiceModel.OperationContractAttribute(Action="Hitachi.Tester.Module/TesterObjectStreamingContract/BladeFileRead", ReplyAction="Hitachi.Tester.Module/TesterObjectStreamingContract/BladeFileReadResponse")]
        System.IO.Stream BladeFileRead(string fileRequest);
        
        // CODEGEN: Generating message contract since the wrapper name (FileStreamRequest) of message FileStreamRequest does not match the default value (BladeFileWrite)
        [System.ServiceModel.OperationContractAttribute(Action="Hitachi.Tester.Module/TesterObjectStreamingContract/BladeFileWrite", ReplyAction="Hitachi.Tester.Module/TesterObjectStreamingContract/BladeFileWriteResponse")]
        Hitachi.Tester.Client.ServiceReference2.FileStreamResponse BladeFileWrite(Hitachi.Tester.Client.ServiceReference2.FileStreamRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="FileStreamRequest", WrapperNamespace="Hitachi.Tester.Module", IsWrapped=true)]
    public partial class FileStreamRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="Hitachi.Tester.Module")]
        public string FileName;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="Hitachi.Tester.Module", Order=0)]
        public System.IO.Stream FileByteStream;
        
        public FileStreamRequest() {
        }
        
        public FileStreamRequest(string FileName, System.IO.Stream FileByteStream) {
            this.FileName = FileName;
            this.FileByteStream = FileByteStream;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="FileStreamResponse", WrapperNamespace="Hitachi.Tester.Module", IsWrapped=true)]
    public partial class FileStreamResponse {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="Hitachi.Tester.Module")]
        public string FileName;
        
        public FileStreamResponse() {
        }
        
        public FileStreamResponse(string FileName) {
            this.FileName = FileName;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface TesterObjectStreamingContractChannel : Hitachi.Tester.Client.ServiceReference2.TesterObjectStreamingContract, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class TesterObjectStreamingContractClient : System.ServiceModel.ClientBase<Hitachi.Tester.Client.ServiceReference2.TesterObjectStreamingContract>, Hitachi.Tester.Client.ServiceReference2.TesterObjectStreamingContract {
        
        public TesterObjectStreamingContractClient() {
        }
        
        public TesterObjectStreamingContractClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public TesterObjectStreamingContractClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public TesterObjectStreamingContractClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public TesterObjectStreamingContractClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public System.IO.Stream BladeFileRead(string fileRequest) {
            return base.Channel.BladeFileRead(fileRequest);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Hitachi.Tester.Client.ServiceReference2.FileStreamResponse Hitachi.Tester.Client.ServiceReference2.TesterObjectStreamingContract.BladeFileWrite(Hitachi.Tester.Client.ServiceReference2.FileStreamRequest request) {
            return base.Channel.BladeFileWrite(request);
        }
        
        public void BladeFileWrite(ref string FileName, System.IO.Stream FileByteStream) {
            Hitachi.Tester.Client.ServiceReference2.FileStreamRequest inValue = new Hitachi.Tester.Client.ServiceReference2.FileStreamRequest();
            inValue.FileName = FileName;
            inValue.FileByteStream = FileByteStream;
            Hitachi.Tester.Client.ServiceReference2.FileStreamResponse retVal = ((Hitachi.Tester.Client.ServiceReference2.TesterObjectStreamingContract)(this)).BladeFileWrite(inValue);
            FileName = retVal.FileName;
        }
    }
}
