using System.Linq;
using Merchello.Core;

namespace Merchello.Plugin.Payments.SagePay.Provider
{
    using Merchello.Core.Gateways;
    using Merchello.Core.Gateways.Payment;
    using Merchello.Core.Models;
    using Merchello.Core.Services;

    using Umbraco.Core;
    public abstract class SagePayPaymentGatewayMethodBase : PaymentGatewayMethodBase, ISagePayFormPaymentGatewayMethod
    {

        protected SagePayPaymentProcessorBase _processor;

           public SagePayPaymentGatewayMethodBase(IGatewayProviderService gatewayProviderService, IPaymentMethod paymentMethod, ExtendedDataCollection extendedData)
            : base(gatewayProviderService, paymentMethod)
        {
            

        }



           /// <summary>
           /// Does the actual work capturing a payment
           /// </summary>
           /// <param name="invoice">The <see cref="IInvoice"/></param>
           /// <param name="payment">The previously Authorize payment to be captured</param>
           /// <param name="amount">The amount to capture</param>
           /// <param name="args">Any arguments required to process the payment.</param>
           /// <returns>The <see cref="IPaymentResult"/></returns>
           protected override IPaymentResult PerformCapturePayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
           {
               var payedTotalList = invoice.AppliedPayments().Select(item => item.Amount).ToList();
               var payedTotal = (payedTotalList.Count == 0 ? 0 : payedTotalList.Aggregate((a, b) => a + b));
               var isPartialPayment = amount + payedTotal < invoice.Total;

               var result = _processor.CapturePayment(invoice, payment, amount, isPartialPayment);
               //GatewayProviderService.Save(payment);

               if (!result.Payment.Success)
               {
                   //payment.VoidPayment(invoice, payment.PaymentMethodKey.Value);
                   GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Denied, "SagePay: request capture error: " + result.Payment.Exception.Message, 0);
               }
               else
               {
                   GatewayProviderService.Save(payment);
                   GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "SagePay: captured", amount);
                   //GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.CaptureTransactionResult), amount);
               }


               return result;
           }



           /// <summary>
           /// Does the actual work of authorizing and capturing a payment
           /// </summary>
           /// <param name="invoice">The <see cref="IInvoice"/></param>
           /// <param name="amount">The amount to capture</param>
           /// <param name="args">Any arguments required to process the payment.</param>
           /// <returns>The <see cref="IPaymentResult"/></returns>
           protected override IPaymentResult PerformAuthorizeCapturePayment(IInvoice invoice, decimal amount, ProcessorArgumentCollection args)
           {
               // SERVER Side implementation ... probably not need for the IFRAME method
               throw new System.NotImplementedException();
           }

           /// <summary>
           /// Does the actual work of refunding a payment
           /// </summary>
           /// <param name="invoice">The <see cref="IInvoice"/></param>
           /// <param name="payment">The previously Authorize payment to be captured</param>
           /// <param name="amount">The amount to be refunded</param>
           /// <param name="args">Any arguments required to process the payment.</param>
           /// <returns>The <see cref="IPaymentResult"/></returns>
           protected override IPaymentResult PerformRefundPayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
           {
               throw new System.NotImplementedException();
           }

           /// <summary>
           /// Does the actual work of voiding a payment
           /// </summary>
           /// <param name="invoice">The invoice to which the payment is associated</param>
           /// <param name="payment">The payment to be voided</param>
           /// <param name="args">Additional arguments required by the payment processor</param>
           /// <returns>A <see cref="IPaymentResult"/></returns>
           protected override IPaymentResult PerformVoidPayment(IInvoice invoice, IPayment payment, ProcessorArgumentCollection args)
           {
               throw new System.NotImplementedException();
           }


    }
}
