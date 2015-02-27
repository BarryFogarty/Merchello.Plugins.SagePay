using System.Linq;
using Merchello.Core;

namespace Merchello.Plugin.Payments.SagePay.Provider
{
    using Merchello.Core.Gateways;
    using Merchello.Core.Gateways.Payment;
    using Merchello.Core.Models;
    using Merchello.Core.Services;

    /// <summary>
    /// Represents a SagePayGatewayMethod for Merchello.
    /// </summary>
    [GatewayMethodUi("SagePayIFrame")]
    [PaymentGatewayMethod("SagePay IFrame Method Editors",
        "~/App_Plugins/Merchello.SagePay/",
        "~/App_Plugins/Merchello.SagePay/",
        "~/App_Plugins/Merchello.SagePay/")]
    public class SagePayFormPaymentGatewayMethod : SagePayPaymentGatewayMethodBase, ISagePayFormPaymentGatewayMethod       
    {
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SagePayFormPaymentGatewayMethod"/> class.
        /// </summary>
        /// <param name="gatewayProviderService">
        /// The <see cref="GatewayProviderService"/>.
        /// </param>
        /// <param name="paymentMethod">
        /// The <see cref="IPaymentMethod"/>.
        /// </param>
        /// <param name="extendedData">
        /// The SagePay providers <see cref="ExtendedDataCollection"/>
        /// </param>
        public SagePayFormPaymentGatewayMethod(IGatewayProviderService gatewayProviderService, IPaymentMethod paymentMethod, ExtendedDataCollection extendedData)
            : base(gatewayProviderService, paymentMethod, extendedData)
        {
            // New instance of the SagePay payment processor
            _processor = new SagePayFormPaymentProcessor(extendedData.GetProcessorSettings());
        }

        /// <summary>
        /// Does the actual work of creating and processing the payment
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/></param>
        /// <param name="args">Any arguments required to process the payment.</param>
        /// <returns>The <see cref="IPaymentResult"/></returns>
        protected override IPaymentResult PerformAuthorizePayment(IInvoice invoice, ProcessorArgumentCollection args)
        {
            return InitializePayment(invoice, args);
        }


        private IPaymentResult InitializePayment(IInvoice invoice, ProcessorArgumentCollection args)
        {
            var payment = GatewayProviderService.CreatePayment(PaymentMethodType.CreditCard, invoice.Total, PaymentMethod.Key);
            payment.CustomerKey = invoice.CustomerKey;
            payment.Authorized = false;
            payment.Collected = false;
            payment.PaymentMethodName = "SagePay";
            GatewayProviderService.Save(payment);

            var result = ((SagePayFormPaymentProcessor)_processor).InitializePayment(invoice, payment, args);

            if (!result.Payment.Success)
            {
                GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Denied, "SagePay: request initialization error: " + result.Payment.Exception.Message, 0);
            }
            else
            {
                GatewayProviderService.Save(payment);
                GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "SagePay: initialized", 0);
            }

            return result;
        }

      


    }
}