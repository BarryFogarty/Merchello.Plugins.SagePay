using System.Collections.Specialized;
using Merchello.Plugin.Payments.SagePay.Models;
using SagePay.IntegrationKit;
using SagePay.IntegrationKit.Messages;

namespace Merchello.Plugin.Payments.SagePay.SagePayService
{
    /// <summary>
    /// Summary description for FormIntegration
    /// </summary>
    public class SagePayFormIntegration : SagePayAPIIntegration
    {
        private readonly SagePayProcessorSettings _settings;

        public SagePayFormIntegration(SagePayProcessorSettings settings) : base(settings)
        {
            _settings = settings;
        }

        public IFormPayment FormPaymentRequest()
        {
            IFormPayment request = new DataObject();
            return request;
        }

        public NameValueCollection Validation(IFormPayment formPayment)
        {
            return Validation(ProtocolMessage.FORM_PAYMENT, typeof(IFormPayment), formPayment, _settings.ProtocolVersion);
        }

        public IFormPayment ProcessRequest(IFormPayment formPayment)
        {
            RequestQueryString = BuildQueryString(ConvertSagePayMessageToNameValueCollection(ProtocolMessage.FORM_PAYMENT, typeof(IFormPayment), formPayment, _settings.ProtocolVersion));

            formPayment.Crypt = Cryptography.EncryptAndEncode(RequestQueryString, _settings.EncryptionPassword);

            return formPayment;
        }

        public IFormPaymentResult ProcessResult(string crypt)
        {
            IFormPaymentResult formPaymentResult = new DataObject();

            string cryptDecoded = Cryptography.DecodeAndDecrypt(crypt, _settings.EncryptionPassword);

            formPaymentResult = (IFormPaymentResult)ConvertToSagePayMessage(cryptDecoded);

            return formPaymentResult;
        }

        
    }
}