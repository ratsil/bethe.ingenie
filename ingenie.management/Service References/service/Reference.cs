﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This code was auto-generated by Microsoft.Silverlight.ServiceReference, version 5.0.61118.0
// 
namespace ingenie.management.service {
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="EffectInfo", Namespace="http://replica/ig/services/Cues.asmx")]
    public partial class EffectInfo : object, System.ComponentModel.INotifyPropertyChanged {
        
        private int nHashCodeField;
        
        private string sNameField;
        
        private string sInfoField;
        
        private string sTypeField;
        
        private string sStatusField;
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public int nHashCode {
            get {
                return this.nHashCodeField;
            }
            set {
                if ((this.nHashCodeField.Equals(value) != true)) {
                    this.nHashCodeField = value;
                    this.RaisePropertyChanged("nHashCode");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false)]
        public string sName {
            get {
                return this.sNameField;
            }
            set {
                if ((object.ReferenceEquals(this.sNameField, value) != true)) {
                    this.sNameField = value;
                    this.RaisePropertyChanged("sName");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=2)]
        public string sInfo {
            get {
                return this.sInfoField;
            }
            set {
                if ((object.ReferenceEquals(this.sInfoField, value) != true)) {
                    this.sInfoField = value;
                    this.RaisePropertyChanged("sInfo");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=3)]
        public string sType {
            get {
                return this.sTypeField;
            }
            set {
                if ((object.ReferenceEquals(this.sTypeField, value) != true)) {
                    this.sTypeField = value;
                    this.RaisePropertyChanged("sType");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=4)]
        public string sStatus {
            get {
                return this.sStatusField;
            }
            set {
                if ((object.ReferenceEquals(this.sStatusField, value) != true)) {
                    this.sStatusField = value;
                    this.RaisePropertyChanged("sStatus");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.CollectionDataContractAttribute(Name="ArrayOfInt", Namespace="http://replica/ig/services/Cues.asmx", ItemName="int")]
    public class ArrayOfInt : System.Collections.Generic.List<int> {
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://replica/ig/services/Cues.asmx", ConfigurationName="service.ManagementSoap")]
    public interface ManagementSoap {
        
        [System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://replica/ig/services/Cues.asmx/BaetylusEffectsInfoGet", ReplyAction="*")]
        System.IAsyncResult BeginBaetylusEffectsInfoGet(ingenie.management.service.BaetylusEffectsInfoGetRequest request, System.AsyncCallback callback, object asyncState);
        
        ingenie.management.service.BaetylusEffectsInfoGetResponse EndBaetylusEffectsInfoGet(System.IAsyncResult result);
        
        [System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://replica/ig/services/Cues.asmx/BaetylusEffectStop", ReplyAction="*")]
        System.IAsyncResult BeginBaetylusEffectStop(ingenie.management.service.BaetylusEffectStopRequest request, System.AsyncCallback callback, object asyncState);
        
        ingenie.management.service.BaetylusEffectStopResponse EndBaetylusEffectStop(System.IAsyncResult result);
        
        [System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://replica/ig/services/Cues.asmx/RestartServices", ReplyAction="*")]
        System.IAsyncResult BeginRestartServices(System.AsyncCallback callback, object asyncState);
        
        void EndRestartServices(System.IAsyncResult result);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class BaetylusEffectsInfoGetRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="BaetylusEffectsInfoGet", Namespace="http://replica/ig/services/Cues.asmx", Order=0)]
        public ingenie.management.service.BaetylusEffectsInfoGetRequestBody Body;
        
        public BaetylusEffectsInfoGetRequest() {
        }
        
        public BaetylusEffectsInfoGetRequest(ingenie.management.service.BaetylusEffectsInfoGetRequestBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute()]
    public partial class BaetylusEffectsInfoGetRequestBody {
        
        public BaetylusEffectsInfoGetRequestBody() {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class BaetylusEffectsInfoGetResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="BaetylusEffectsInfoGetResponse", Namespace="http://replica/ig/services/Cues.asmx", Order=0)]
        public ingenie.management.service.BaetylusEffectsInfoGetResponseBody Body;
        
        public BaetylusEffectsInfoGetResponse() {
        }
        
        public BaetylusEffectsInfoGetResponse(ingenie.management.service.BaetylusEffectsInfoGetResponseBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute(Namespace="http://replica/ig/services/Cues.asmx")]
    public partial class BaetylusEffectsInfoGetResponseBody {
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=0)]
        public ingenie.management.service.EffectInfo[] BaetylusEffectsInfoGetResult;
        
        public BaetylusEffectsInfoGetResponseBody() {
        }
        
        public BaetylusEffectsInfoGetResponseBody(ingenie.management.service.EffectInfo[] BaetylusEffectsInfoGetResult) {
            this.BaetylusEffectsInfoGetResult = BaetylusEffectsInfoGetResult;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class BaetylusEffectStopRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="BaetylusEffectStop", Namespace="http://replica/ig/services/Cues.asmx", Order=0)]
        public ingenie.management.service.BaetylusEffectStopRequestBody Body;
        
        public BaetylusEffectStopRequest() {
        }
        
        public BaetylusEffectStopRequest(ingenie.management.service.BaetylusEffectStopRequestBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute(Namespace="http://replica/ig/services/Cues.asmx")]
    public partial class BaetylusEffectStopRequestBody {
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=0)]
        public ingenie.management.service.EffectInfo[] aEffects;
        
        public BaetylusEffectStopRequestBody() {
        }
        
        public BaetylusEffectStopRequestBody(ingenie.management.service.EffectInfo[] aEffects) {
            this.aEffects = aEffects;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class BaetylusEffectStopResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="BaetylusEffectStopResponse", Namespace="http://replica/ig/services/Cues.asmx", Order=0)]
        public ingenie.management.service.BaetylusEffectStopResponseBody Body;
        
        public BaetylusEffectStopResponse() {
        }
        
        public BaetylusEffectStopResponse(ingenie.management.service.BaetylusEffectStopResponseBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute(Namespace="http://replica/ig/services/Cues.asmx")]
    public partial class BaetylusEffectStopResponseBody {
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=0)]
        public ingenie.management.service.ArrayOfInt BaetylusEffectStopResult;
        
        public BaetylusEffectStopResponseBody() {
        }
        
        public BaetylusEffectStopResponseBody(ingenie.management.service.ArrayOfInt BaetylusEffectStopResult) {
            this.BaetylusEffectStopResult = BaetylusEffectStopResult;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ManagementSoapChannel : ingenie.management.service.ManagementSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class BaetylusEffectsInfoGetCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        public BaetylusEffectsInfoGetCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        public ingenie.management.service.EffectInfo[] Result {
            get {
                base.RaiseExceptionIfNecessary();
                return ((ingenie.management.service.EffectInfo[])(this.results[0]));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class BaetylusEffectStopCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        public BaetylusEffectStopCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        public ingenie.management.service.ArrayOfInt Result {
            get {
                base.RaiseExceptionIfNecessary();
                return ((ingenie.management.service.ArrayOfInt)(this.results[0]));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ManagementSoapClient : System.ServiceModel.ClientBase<ingenie.management.service.ManagementSoap>, ingenie.management.service.ManagementSoap {
        
        private BeginOperationDelegate onBeginBaetylusEffectsInfoGetDelegate;
        
        private EndOperationDelegate onEndBaetylusEffectsInfoGetDelegate;
        
        private System.Threading.SendOrPostCallback onBaetylusEffectsInfoGetCompletedDelegate;
        
        private BeginOperationDelegate onBeginBaetylusEffectStopDelegate;
        
        private EndOperationDelegate onEndBaetylusEffectStopDelegate;
        
        private System.Threading.SendOrPostCallback onBaetylusEffectStopCompletedDelegate;
        
        private BeginOperationDelegate onBeginRestartServicesDelegate;
        
        private EndOperationDelegate onEndRestartServicesDelegate;
        
        private System.Threading.SendOrPostCallback onRestartServicesCompletedDelegate;
        
        private BeginOperationDelegate onBeginOpenDelegate;
        
        private EndOperationDelegate onEndOpenDelegate;
        
        private System.Threading.SendOrPostCallback onOpenCompletedDelegate;
        
        private BeginOperationDelegate onBeginCloseDelegate;
        
        private EndOperationDelegate onEndCloseDelegate;
        
        private System.Threading.SendOrPostCallback onCloseCompletedDelegate;
        
        public ManagementSoapClient() {
        }
        
        public ManagementSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ManagementSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ManagementSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ManagementSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public System.Net.CookieContainer CookieContainer {
            get {
                System.ServiceModel.Channels.IHttpCookieContainerManager httpCookieContainerManager = this.InnerChannel.GetProperty<System.ServiceModel.Channels.IHttpCookieContainerManager>();
                if ((httpCookieContainerManager != null)) {
                    return httpCookieContainerManager.CookieContainer;
                }
                else {
                    return null;
                }
            }
            set {
                System.ServiceModel.Channels.IHttpCookieContainerManager httpCookieContainerManager = this.InnerChannel.GetProperty<System.ServiceModel.Channels.IHttpCookieContainerManager>();
                if ((httpCookieContainerManager != null)) {
                    httpCookieContainerManager.CookieContainer = value;
                }
                else {
                    throw new System.InvalidOperationException("Unable to set the CookieContainer. Please make sure the binding contains an HttpC" +
                            "ookieContainerBindingElement.");
                }
            }
        }
        
        public event System.EventHandler<BaetylusEffectsInfoGetCompletedEventArgs> BaetylusEffectsInfoGetCompleted;
        
        public event System.EventHandler<BaetylusEffectStopCompletedEventArgs> BaetylusEffectStopCompleted;
        
        public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> RestartServicesCompleted;
        
        public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> OpenCompleted;
        
        public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> CloseCompleted;
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.IAsyncResult ingenie.management.service.ManagementSoap.BeginBaetylusEffectsInfoGet(ingenie.management.service.BaetylusEffectsInfoGetRequest request, System.AsyncCallback callback, object asyncState) {
            return base.Channel.BeginBaetylusEffectsInfoGet(request, callback, asyncState);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        private System.IAsyncResult BeginBaetylusEffectsInfoGet(System.AsyncCallback callback, object asyncState) {
            ingenie.management.service.BaetylusEffectsInfoGetRequest inValue = new ingenie.management.service.BaetylusEffectsInfoGetRequest();
            inValue.Body = new ingenie.management.service.BaetylusEffectsInfoGetRequestBody();
            return ((ingenie.management.service.ManagementSoap)(this)).BeginBaetylusEffectsInfoGet(inValue, callback, asyncState);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        ingenie.management.service.BaetylusEffectsInfoGetResponse ingenie.management.service.ManagementSoap.EndBaetylusEffectsInfoGet(System.IAsyncResult result) {
            return base.Channel.EndBaetylusEffectsInfoGet(result);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        private ingenie.management.service.EffectInfo[] EndBaetylusEffectsInfoGet(System.IAsyncResult result) {
            ingenie.management.service.BaetylusEffectsInfoGetResponse retVal = ((ingenie.management.service.ManagementSoap)(this)).EndBaetylusEffectsInfoGet(result);
            return retVal.Body.BaetylusEffectsInfoGetResult;
        }
        
        private System.IAsyncResult OnBeginBaetylusEffectsInfoGet(object[] inValues, System.AsyncCallback callback, object asyncState) {
            return this.BeginBaetylusEffectsInfoGet(callback, asyncState);
        }
        
        private object[] OnEndBaetylusEffectsInfoGet(System.IAsyncResult result) {
            ingenie.management.service.EffectInfo[] retVal = this.EndBaetylusEffectsInfoGet(result);
            return new object[] {
                    retVal};
        }
        
        private void OnBaetylusEffectsInfoGetCompleted(object state) {
            if ((this.BaetylusEffectsInfoGetCompleted != null)) {
                InvokeAsyncCompletedEventArgs e = ((InvokeAsyncCompletedEventArgs)(state));
                this.BaetylusEffectsInfoGetCompleted(this, new BaetylusEffectsInfoGetCompletedEventArgs(e.Results, e.Error, e.Cancelled, e.UserState));
            }
        }
        
        public void BaetylusEffectsInfoGetAsync() {
            this.BaetylusEffectsInfoGetAsync(null);
        }
        
        public void BaetylusEffectsInfoGetAsync(object userState) {
            if ((this.onBeginBaetylusEffectsInfoGetDelegate == null)) {
                this.onBeginBaetylusEffectsInfoGetDelegate = new BeginOperationDelegate(this.OnBeginBaetylusEffectsInfoGet);
            }
            if ((this.onEndBaetylusEffectsInfoGetDelegate == null)) {
                this.onEndBaetylusEffectsInfoGetDelegate = new EndOperationDelegate(this.OnEndBaetylusEffectsInfoGet);
            }
            if ((this.onBaetylusEffectsInfoGetCompletedDelegate == null)) {
                this.onBaetylusEffectsInfoGetCompletedDelegate = new System.Threading.SendOrPostCallback(this.OnBaetylusEffectsInfoGetCompleted);
            }
            base.InvokeAsync(this.onBeginBaetylusEffectsInfoGetDelegate, null, this.onEndBaetylusEffectsInfoGetDelegate, this.onBaetylusEffectsInfoGetCompletedDelegate, userState);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.IAsyncResult ingenie.management.service.ManagementSoap.BeginBaetylusEffectStop(ingenie.management.service.BaetylusEffectStopRequest request, System.AsyncCallback callback, object asyncState) {
            return base.Channel.BeginBaetylusEffectStop(request, callback, asyncState);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        private System.IAsyncResult BeginBaetylusEffectStop(ingenie.management.service.EffectInfo[] aEffects, System.AsyncCallback callback, object asyncState) {
            ingenie.management.service.BaetylusEffectStopRequest inValue = new ingenie.management.service.BaetylusEffectStopRequest();
            inValue.Body = new ingenie.management.service.BaetylusEffectStopRequestBody();
            inValue.Body.aEffects = aEffects;
            return ((ingenie.management.service.ManagementSoap)(this)).BeginBaetylusEffectStop(inValue, callback, asyncState);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        ingenie.management.service.BaetylusEffectStopResponse ingenie.management.service.ManagementSoap.EndBaetylusEffectStop(System.IAsyncResult result) {
            return base.Channel.EndBaetylusEffectStop(result);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        private ingenie.management.service.ArrayOfInt EndBaetylusEffectStop(System.IAsyncResult result) {
            ingenie.management.service.BaetylusEffectStopResponse retVal = ((ingenie.management.service.ManagementSoap)(this)).EndBaetylusEffectStop(result);
            return retVal.Body.BaetylusEffectStopResult;
        }
        
        private System.IAsyncResult OnBeginBaetylusEffectStop(object[] inValues, System.AsyncCallback callback, object asyncState) {
            ingenie.management.service.EffectInfo[] aEffects = ((ingenie.management.service.EffectInfo[])(inValues[0]));
            return this.BeginBaetylusEffectStop(aEffects, callback, asyncState);
        }
        
        private object[] OnEndBaetylusEffectStop(System.IAsyncResult result) {
            ingenie.management.service.ArrayOfInt retVal = this.EndBaetylusEffectStop(result);
            return new object[] {
                    retVal};
        }
        
        private void OnBaetylusEffectStopCompleted(object state) {
            if ((this.BaetylusEffectStopCompleted != null)) {
                InvokeAsyncCompletedEventArgs e = ((InvokeAsyncCompletedEventArgs)(state));
                this.BaetylusEffectStopCompleted(this, new BaetylusEffectStopCompletedEventArgs(e.Results, e.Error, e.Cancelled, e.UserState));
            }
        }
        
        public void BaetylusEffectStopAsync(ingenie.management.service.EffectInfo[] aEffects) {
            this.BaetylusEffectStopAsync(aEffects, null);
        }
        
        public void BaetylusEffectStopAsync(ingenie.management.service.EffectInfo[] aEffects, object userState) {
            if ((this.onBeginBaetylusEffectStopDelegate == null)) {
                this.onBeginBaetylusEffectStopDelegate = new BeginOperationDelegate(this.OnBeginBaetylusEffectStop);
            }
            if ((this.onEndBaetylusEffectStopDelegate == null)) {
                this.onEndBaetylusEffectStopDelegate = new EndOperationDelegate(this.OnEndBaetylusEffectStop);
            }
            if ((this.onBaetylusEffectStopCompletedDelegate == null)) {
                this.onBaetylusEffectStopCompletedDelegate = new System.Threading.SendOrPostCallback(this.OnBaetylusEffectStopCompleted);
            }
            base.InvokeAsync(this.onBeginBaetylusEffectStopDelegate, new object[] {
                        aEffects}, this.onEndBaetylusEffectStopDelegate, this.onBaetylusEffectStopCompletedDelegate, userState);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.IAsyncResult ingenie.management.service.ManagementSoap.BeginRestartServices(System.AsyncCallback callback, object asyncState) {
            return base.Channel.BeginRestartServices(callback, asyncState);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        void ingenie.management.service.ManagementSoap.EndRestartServices(System.IAsyncResult result) {
            base.Channel.EndRestartServices(result);
        }
        
        private System.IAsyncResult OnBeginRestartServices(object[] inValues, System.AsyncCallback callback, object asyncState) {
            return ((ingenie.management.service.ManagementSoap)(this)).BeginRestartServices(callback, asyncState);
        }
        
        private object[] OnEndRestartServices(System.IAsyncResult result) {
            ((ingenie.management.service.ManagementSoap)(this)).EndRestartServices(result);
            return null;
        }
        
        private void OnRestartServicesCompleted(object state) {
            if ((this.RestartServicesCompleted != null)) {
                InvokeAsyncCompletedEventArgs e = ((InvokeAsyncCompletedEventArgs)(state));
                this.RestartServicesCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(e.Error, e.Cancelled, e.UserState));
            }
        }
        
        public void RestartServicesAsync() {
            this.RestartServicesAsync(null);
        }
        
        public void RestartServicesAsync(object userState) {
            if ((this.onBeginRestartServicesDelegate == null)) {
                this.onBeginRestartServicesDelegate = new BeginOperationDelegate(this.OnBeginRestartServices);
            }
            if ((this.onEndRestartServicesDelegate == null)) {
                this.onEndRestartServicesDelegate = new EndOperationDelegate(this.OnEndRestartServices);
            }
            if ((this.onRestartServicesCompletedDelegate == null)) {
                this.onRestartServicesCompletedDelegate = new System.Threading.SendOrPostCallback(this.OnRestartServicesCompleted);
            }
            base.InvokeAsync(this.onBeginRestartServicesDelegate, null, this.onEndRestartServicesDelegate, this.onRestartServicesCompletedDelegate, userState);
        }
        
        private System.IAsyncResult OnBeginOpen(object[] inValues, System.AsyncCallback callback, object asyncState) {
            return ((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(callback, asyncState);
        }
        
        private object[] OnEndOpen(System.IAsyncResult result) {
            ((System.ServiceModel.ICommunicationObject)(this)).EndOpen(result);
            return null;
        }
        
        private void OnOpenCompleted(object state) {
            if ((this.OpenCompleted != null)) {
                InvokeAsyncCompletedEventArgs e = ((InvokeAsyncCompletedEventArgs)(state));
                this.OpenCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(e.Error, e.Cancelled, e.UserState));
            }
        }
        
        public void OpenAsync() {
            this.OpenAsync(null);
        }
        
        public void OpenAsync(object userState) {
            if ((this.onBeginOpenDelegate == null)) {
                this.onBeginOpenDelegate = new BeginOperationDelegate(this.OnBeginOpen);
            }
            if ((this.onEndOpenDelegate == null)) {
                this.onEndOpenDelegate = new EndOperationDelegate(this.OnEndOpen);
            }
            if ((this.onOpenCompletedDelegate == null)) {
                this.onOpenCompletedDelegate = new System.Threading.SendOrPostCallback(this.OnOpenCompleted);
            }
            base.InvokeAsync(this.onBeginOpenDelegate, null, this.onEndOpenDelegate, this.onOpenCompletedDelegate, userState);
        }
        
        private System.IAsyncResult OnBeginClose(object[] inValues, System.AsyncCallback callback, object asyncState) {
            return ((System.ServiceModel.ICommunicationObject)(this)).BeginClose(callback, asyncState);
        }
        
        private object[] OnEndClose(System.IAsyncResult result) {
            ((System.ServiceModel.ICommunicationObject)(this)).EndClose(result);
            return null;
        }
        
        private void OnCloseCompleted(object state) {
            if ((this.CloseCompleted != null)) {
                InvokeAsyncCompletedEventArgs e = ((InvokeAsyncCompletedEventArgs)(state));
                this.CloseCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(e.Error, e.Cancelled, e.UserState));
            }
        }
        
        public void CloseAsync() {
            this.CloseAsync(null);
        }
        
        public void CloseAsync(object userState) {
            if ((this.onBeginCloseDelegate == null)) {
                this.onBeginCloseDelegate = new BeginOperationDelegate(this.OnBeginClose);
            }
            if ((this.onEndCloseDelegate == null)) {
                this.onEndCloseDelegate = new EndOperationDelegate(this.OnEndClose);
            }
            if ((this.onCloseCompletedDelegate == null)) {
                this.onCloseCompletedDelegate = new System.Threading.SendOrPostCallback(this.OnCloseCompleted);
            }
            base.InvokeAsync(this.onBeginCloseDelegate, null, this.onEndCloseDelegate, this.onCloseCompletedDelegate, userState);
        }
        
        protected override ingenie.management.service.ManagementSoap CreateChannel() {
            return new ManagementSoapClientChannel(this);
        }
        
        private class ManagementSoapClientChannel : ChannelBase<ingenie.management.service.ManagementSoap>, ingenie.management.service.ManagementSoap {
            
            public ManagementSoapClientChannel(System.ServiceModel.ClientBase<ingenie.management.service.ManagementSoap> client) : 
                    base(client) {
            }
            
            public System.IAsyncResult BeginBaetylusEffectsInfoGet(ingenie.management.service.BaetylusEffectsInfoGetRequest request, System.AsyncCallback callback, object asyncState) {
                object[] _args = new object[1];
                _args[0] = request;
                System.IAsyncResult _result = base.BeginInvoke("BaetylusEffectsInfoGet", _args, callback, asyncState);
                return _result;
            }
            
            public ingenie.management.service.BaetylusEffectsInfoGetResponse EndBaetylusEffectsInfoGet(System.IAsyncResult result) {
                object[] _args = new object[0];
                ingenie.management.service.BaetylusEffectsInfoGetResponse _result = ((ingenie.management.service.BaetylusEffectsInfoGetResponse)(base.EndInvoke("BaetylusEffectsInfoGet", _args, result)));
                return _result;
            }
            
            public System.IAsyncResult BeginBaetylusEffectStop(ingenie.management.service.BaetylusEffectStopRequest request, System.AsyncCallback callback, object asyncState) {
                object[] _args = new object[1];
                _args[0] = request;
                System.IAsyncResult _result = base.BeginInvoke("BaetylusEffectStop", _args, callback, asyncState);
                return _result;
            }
            
            public ingenie.management.service.BaetylusEffectStopResponse EndBaetylusEffectStop(System.IAsyncResult result) {
                object[] _args = new object[0];
                ingenie.management.service.BaetylusEffectStopResponse _result = ((ingenie.management.service.BaetylusEffectStopResponse)(base.EndInvoke("BaetylusEffectStop", _args, result)));
                return _result;
            }
            
            public System.IAsyncResult BeginRestartServices(System.AsyncCallback callback, object asyncState) {
                object[] _args = new object[0];
                System.IAsyncResult _result = base.BeginInvoke("RestartServices", _args, callback, asyncState);
                return _result;
            }
            
            public void EndRestartServices(System.IAsyncResult result) {
                object[] _args = new object[0];
                base.EndInvoke("RestartServices", _args, result);
            }
        }
    }
}
