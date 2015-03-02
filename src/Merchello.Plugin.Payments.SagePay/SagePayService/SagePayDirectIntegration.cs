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

      

        
    }
}