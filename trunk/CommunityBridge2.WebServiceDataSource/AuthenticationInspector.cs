using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace CommunityBridge2.WebServiceDataSource
{

    public class AuthenticationInspector : IClientMessageInspector, IEndpointBehavior
    {
        public string PassportTicket
        { get; set; }

        public AuthenticationInspector(string ticket)
        { PassportTicket = ticket; }

        #region IClientMessageInspector Members
        public void AfterReceiveReply(ref Message reply, object correlationState) { }
        public object BeforeSendRequest(ref Message request, System.ServiceModel.IClientChannel channel)
        {
            request.Headers.Add(MessageHeader.CreateHeader("Passport", "ms", PassportTicket));
            return null;
        }
        #endregion
        #region IEndpointBehavior Members
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new AuthenticationInspector(PassportTicket));
        }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void Validate(ServiceEndpoint endpoint) { }
        #endregion
    }

}
