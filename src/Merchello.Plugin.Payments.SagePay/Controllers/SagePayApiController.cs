using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Merchello.Plugin.Payments.SagePay.SagePayService;
using SagePay.IntegrationKit;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using Merchello.Core;
using Merchello.Core.Services;
using Merchello.Plugin.Payments.SagePay.Provider;
using System.Text;

namespace Merchello.Plugin.Payments.SagePay.Controllers
{
    /// <summary>
    /// The SagePay API controller.
    /// </summary>
    [PluginController("MerchelloSagePay")]
    public class SagePayApiController : UmbracoApiController
    {
        /// <summary>
        /// Merchello context
        /// </summary>
        private readonly IMerchelloContext _merchelloContext;

        /// <summary>
        /// The SagePay payment processor.
        /// </summary>
        private readonly SagePayFormPaymentProcessor _processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SagePayApiController"/> class.
        /// </summary>
        public SagePayApiController()
            : this(MerchelloContext.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SagePayApiController"/> class.
        /// </summary>
        /// <param name="merchelloContext">
        /// The <see cref="IMerchelloContext"/>.
        /// </param>
        public SagePayApiController(IMerchelloContext merchelloContext)
        {
            if (merchelloContext == null) throw new ArgumentNullException("merchelloContext");

            var providerKey = new Guid(Constants.GatewayProviderSettingsKey);
            var provider =
                (SagePayPaymentGatewayProvider) merchelloContext.Gateways.Payment.GetProviderByKey(providerKey);

            if (provider == null)
            {
                var ex =
                    new NullReferenceException(
                        "The SagePayPaymentGatewayProvider could not be resolved.  The provider must be activiated");
                LogHelper.Error<SagePayApiController>("SagePayPaymentGatewayProvider not activated.", ex);
                throw ex;
            }

            _merchelloContext = merchelloContext;
            _processor = new SagePayFormPaymentProcessor(provider.ExtendedData.GetProcessorSettings());
        }


        // <summary>
        /// Authorize payment
        /// </summary>
        /// <param name="crypt"></param>
        /// <param name="invoiceKey"></param>
        /// <param name="paymentKey"></param>
        /// <returns></returns>
        /// <example>/umbraco/MerchelloSagePay/SagePayApi/SuccessPayment?crypt=396280fd282bc0fef215708695f23da02e5ba6f8d4980ed758627d7b872bf2f1c25599f1a9ac9cf5617e449dc3336e8d0786d367bdc6315a4686b44698f17f99350b5d4fe21eb04179388b0d72c241e4d45893bafd375a2865cfbed596dcdd9f85d2aff2cd46301507d8e38688438027d3e3a4d35ece75ef7369a02c4225b38f003b21180c088f3e6da6342bd7922fbb2a84238f9f2b7c189ae6870c8fe23af37114c85f658ec2a620c25106839cd96cb10529f9f44f7c5154ec78a87d95803e2b8150955d7c0da167c7199d7a13027a812f5d340ee580e691b7d4357920dd01fc48e501d8574e67f708c907030272c35ef01eb236fcc6d97d25fda55869c99370210a2790091e1cd20c9a93791b00e77533c391d1e313ec721be99f60bd4861ccb993547e054ca83ab8c40523e925c07fb2cdf9a3fd98434ee531eeab6c7013525d1c8506ab208e04455b8be84f08e9de1ab52033e9f6193fc8ec4314fb85e5674971af3fee334030e010b3d60ceea18589de3c7ec397d3be152154cd49ffe6861dfdcda1d31eb244f924a61d4610ff</example>
        [HttpGet]
        public HttpResponseMessage SuccessPayment(Guid invoiceKey, Guid paymentKey, string crypt)
        {
            // Decrypt sagepay querystring data message
            if (string.IsNullOrEmpty(crypt))
            {
                var ex = new NullReferenceException(string.Format("Invalid argument exception. Arguments: crypt={0}", crypt));
                LogHelper.Error<SagePayApiController>("Payment not authorized.", ex);
                throw ex;
            }

            var sagePayFormIntegration = new SagePayFormIntegration(_processor.Settings);
            var paymentResult =  sagePayFormIntegration.ProcessResult(crypt);

            if (paymentResult.Status != ResponseStatus.OK && 
                paymentResult.Status != ResponseStatus.AUTHENTICATED &&
                paymentResult.Status != ResponseStatus.REGISTERED)
            {
                //var ex = new Exception(string.Format("Invalid payment status.  Detail: {0}", paymentResult.StatusDetail));
                //LogHelper.Error<SagePayApiController>("Sagepay error processing payment.", ex);
                return ShowError(paymentResult.StatusDetail);
            }

            // Query merchello for associated invoice and payment objects
            var invoice = _merchelloContext.Services.InvoiceService.GetByKey(invoiceKey);
            var payment = _merchelloContext.Services.PaymentService.GetByKey(paymentKey);
            if (invoice == null || payment == null)
            {
                var ex = new NullReferenceException( string.Format( "Invalid argument exception. Arguments: invoiceKey={0}, paymentKey={1}", invoiceKey, paymentKey));
                LogHelper.Error<SagePayApiController>("Payment not authorized.", ex);
                throw ex;
            }

            // Store some SagePay data in payment
            payment.ReferenceNumber = paymentResult.VpsTxId;
            //payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayTransactionCode, paymentResult.VpsTxId);

            // Authorize and save payment
            var authorizeResult = _processor.AuthorizePayment(invoice, payment);
            _merchelloContext.Services.GatewayProviderService.Save(payment);
            if (!authorizeResult.Payment.Success)
            {
                LogHelper.Error<SagePayApiController>("Payment is not authorized.", authorizeResult.Payment.Exception);
                _merchelloContext.Services.GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Denied, "SagePay: request capture authorization error: " + authorizeResult.Payment.Exception.Message, 0);
                return ShowError(authorizeResult.Payment.Exception.Message);
            }

            _merchelloContext.Services.GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "SagePay: capture authorized", 0);

            // Capture payment
            var providerKeyGuid = new Guid(Constants.GatewayProviderSettingsKey);
            var paymentGatewayMethod = _merchelloContext.Gateways.Payment.GetPaymentGatewayMethods().First(item => item.PaymentMethod.ProviderKey == providerKeyGuid);

            var captureResult = paymentGatewayMethod.CapturePayment(invoice, payment, payment.Amount, null);
            if (!captureResult.Payment.Success)
            {
                LogHelper.Error<SagePayApiController>("Payment not captured.", captureResult.Payment.Exception);
                return ShowError(captureResult.Payment.Exception.Message);
            }

            // Redirect to ReturnUrl (with token replacement)
            var returnUrl = _processor.Settings.ReturnUrl;
            var response = Request.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Location = new Uri(returnUrl.Replace("%INVOICE%", invoice.Key.ToString().EncryptWithMachineKey()));
            return response;
        }


        [HttpGet]
        public HttpResponseMessage AbortPayment(Guid invoiceKey, Guid paymentKey, string crypt)
        {
            var invoiceService = _merchelloContext.Services.InvoiceService;
            var paymentService = _merchelloContext.Services.PaymentService;

            var invoice = invoiceService.GetByKey(invoiceKey);
            var payment = paymentService.GetByKey(paymentKey);
            if (invoice == null || payment == null)
            {
                var ex = new NullReferenceException(string.Format("Invalid argument exception. Arguments: invoiceKey={0}, paymentKey={1}", invoiceKey, paymentKey));
                LogHelper.Error<SagePayApiController>("Payment not aborted correctly.", ex);
                throw ex;
            }

            // Delete invoice
            // invoiceService.Delete(invoice);
            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.PaymentCancelled, "true");
            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.PaymentCancelInfo, "Payment cancelled by customer");

            // Return to CancelUrl
            var cancelUrl = _processor.Settings.CancelUrl;
            var response = Request.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Location = new Uri(cancelUrl);
            return response;

        }


        // TODO: add link to Error page
        private HttpResponseMessage ShowError(string message)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = new StringContent("Error: " + message, Encoding.UTF8, "text/plain");
            return resp;
        }

    }
}
