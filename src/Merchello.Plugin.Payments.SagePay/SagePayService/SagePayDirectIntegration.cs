using System.Collections.Specialized;
using Merchello.Plugin.Payments.SagePay.Models;
using SagePay.IntegrationKit;
using SagePay.IntegrationKit.Messages;

namespace Merchello.Plugin.Payments.SagePay.SagePayService
{
    /// <summary>
    /// Summary description for FormIntegration
    /// </summary>
    public class SagePayDirectIntegration : SagePayAPIIntegration
    {
        private readonly SagePayProcessorSettings _settings;

        public SagePayDirectIntegration(SagePayProcessorSettings settings) : base(settings)
        {
            _settings = settings;
        }

        public IDirectPayment DirectPaymentRequest()
        {
            IDirectPayment request = new DataObject();
            return request;
        }

        public NameValueCollection Validation(IDirectPayment directPayment)
        {
            return Validation(ProtocolMessage.FORM_PAYMENT, typeof(IDirectPayment), directPayment, _settings.ProtocolVersion);
        }

        public IDirectPaymentResult ProcessDirect3D(IThreeDAuthRequest request)
        {
            //request.TransactionType = TransactionType.three;
            RequestQueryString = BuildQueryString(request, ProtocolMessage.THREE_D_AUTH_REQUEST, _settings.ProtocolVersion);
            ResponseQueryString = ProcessWebRequestToSagePay(string.Format("https://{0}.sagepay.com/gateway/service/direct3dcallback.vsp", _settings.Environment), RequestQueryString);
            IDirectPaymentResult result = GetDirectPaymentResult(ResponseQueryString);
            return result;
        }


        public IThreeDAuthRequest ThreeDAuthRequest()
        {
            IThreeDAuthRequest request = new DataObject();
            return request;
        }

        
    }
}