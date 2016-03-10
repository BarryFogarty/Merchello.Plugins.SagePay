using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Merchello.Plugin.Payments.SagePay.SagePayService;
using Merchello.Web;
using SagePay.IntegrationKit;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using Merchello.Core;
using Merchello.Core.Services;
using Merchello.Plugin.Payments.SagePay.Provider;
using System.Text;
using SagePay.IntegrationKit.Messages;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

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
        private readonly SagePayFormPaymentProcessor _formProcessor;
        private readonly SagePayDirectPaymentProcessor _directProcessor;

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
            _formProcessor = new SagePayFormPaymentProcessor(provider.ExtendedData.GetProcessorSettings());
            _directProcessor = new SagePayDirectPaymentProcessor(provider.ExtendedData.GetProcessorSettings());
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

            var sagePayFormIntegration = new SagePayFormIntegration(_formProcessor.Settings);
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

            // Get a ref to the customer so the invoice Key can be stored in their extended data.
            // This can be retrieved on the receipt page
            //var customer = _merchelloContext.Services.CustomerService.GetByKey(invoice.CustomerKey.Value);
            //customer.ExtendedData.SetValue(Constants.ExtendedDataKeys.InvoiceKey, invoice.Key.ToString());

            // Store some SagePay data in payment
            payment.ReferenceNumber = paymentResult.VpsTxId;
            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayTransactionCode, paymentResult.VpsTxId);

            // Authorize and save payment
            var authorizeResult = _formProcessor.AuthorizePayment(invoice, payment);
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

            // Redirect to ReturnUrl (with token replacement for an alternative means of order retrieval)
            var returnUrl = payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.ReturnUrl);
            var response = Request.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Location = new Uri(returnUrl.Replace("%INVOICE%", invoice.Key.ToString().EncryptWithMachineKey()));
            return response;
        }


        [HttpGet]
        public HttpResponseMessage AbortPayment(Guid invoiceKey, Guid paymentKey, string crypt = "")
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
            var cancelUrl = payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.CancelUrl);
            var response = Request.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Location = new Uri(cancelUrl);
            return response;

        }



        [HttpPost]
        public HttpResponseMessage PaypalCallback(Guid invoiceKey, Guid paymentKey)
        {
            IPayPalNotificationRequest payPalNotificationRequest = new SagePayDirectIntegration(_directProcessor.Settings).GetPayPalNotificationRequest();

            // Query merchello for associated invoice and payment objects
            var payment = _merchelloContext.Services.PaymentService.GetByKey(paymentKey);
            var invoice = _merchelloContext.Services.InvoiceService.GetByKey(invoiceKey);

            var cancelUrl = payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.CancelUrl);

            if (payPalNotificationRequest.Status != ResponseStatus.OK)
            {
                //var ex = new Exception(string.Format("Invalid payment status.  Detail: {0}", paymentResult.StatusDetail));
                LogHelper.Error<SagePayApiController>("Sagepay error processing payment.", new System.Exception(payPalNotificationRequest.StatusDetail));
                var cancelResponse = Request.CreateResponse(HttpStatusCode.Moved);
                cancelResponse.Headers.Location = new Uri(cancelUrl.Replace("%INVOICE%", invoice.Key.ToString().EncryptWithMachineKey()));
                return cancelResponse;
            }


            if (invoice == null || payment == null)
            {
                var ex = new NullReferenceException(string.Format("Invalid argument exception. Arguments: invoiceKey={0}, paymentKey={1}", invoiceKey, paymentKey));
                LogHelper.Error<SagePayApiController>("Payment not authorized.", ex);
                throw ex;
            }

            // Complete payment with Sagepay
            // Once again, the sagepay integration kit provided by sagepay does not support paypal integration with sagepay direct so we have to build the post manually.
            NameValueCollection sagePayResponseValues = new NameValueCollection();
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string> { };
                values.Add("Accept", "YES");
                values.Add("VPSProtocol", "3.00");
                values.Add("TxType", "COMPLETE");
                values.Add("VPSTxId", payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.SagePayTransactionCode));
                values.Add("Amount", invoice.Total.ToString("n2"));
   
                var content = new FormUrlEncodedContent(values);

                var sagePayResponse = client.PostAsync(string.Format("https://{0}.sagepay.com/gateway/service/complete.vsp", _directProcessor.Settings.Environment), content).Result;

                var responseString = sagePayResponse.Content.ReadAsStringAsync().Result.Replace("\r\n", "&");

                sagePayResponseValues = HttpUtility.ParseQueryString(responseString);
                if (sagePayResponseValues["Status"] != "OK")
                {
                    // This is almost certainly caused by the user cancelling the paypal payment. Abort the payment
                    LogHelper.Error<SagePayApiController>("Payment not authorized.", new NullReferenceException(string.Format("Payment Invalid. Arguments: invoiceKey={0}, paymentKey={1}, exception={2}", invoiceKey, paymentKey, sagePayResponseValues["StatusDetail"])));
                    return AbortPayment(invoiceKey, paymentKey);
                }
                

            }


            // Get a ref to the customer so the invoice Key can be stored in their extended data.
            // This can be retrieved on the receipt page
            //var customer = _merchelloContext.Services.CustomerService.GetByKey(invoice.CustomerKey.Value);
            //customer.ExtendedData.SetValue(Constants.ExtendedDataKeys.InvoiceKey, invoice.Key.ToString());

            // Store some SagePay data in payment
            payment.ReferenceNumber = sagePayResponseValues["VPSTxId"];
            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayTransactionCode, sagePayResponseValues["VPSTxId"]);

            // Authorize and save payment
            var authorizeResult = _directProcessor.AuthorizePayment(invoice, payment);
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

            Notification.Trigger("OrderConfirmation", new Merchello.Core.Gateways.Payment.PaymentResult(Attempt<Merchello.Core.Models.IPayment>.Succeed(payment), invoice, true), new[] { invoice.BillToEmail });

            // Redirect to ReturnUrl (with token replacement for an alternative means of order retrieval)
            var returnUrl = payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.ReturnUrl);
            var response = Request.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Location = new Uri(returnUrl.Replace("%INVOICE%", invoice.Key.ToString().EncryptWithMachineKey()));
            return response;
        }

        // MVC doesn't seem to like getting querystring and form post mixed so create a class for the form post
        public class threeDSecurePostback {
            public string MD { get; set; }
            public string PaRes { get; set; }
            public string MDX { get; set; }
        }

        [HttpPost]
        public HttpResponseMessage ThreeDSecureCallback(Guid invoiceKey, Guid paymentKey, [FromBody]threeDSecurePostback values)
        {
        
              SagePayDirectIntegration sagePayDirectIntegration = new SagePayDirectIntegration(_directProcessor.Settings);

            // Query merchello for associated invoice and payment objects
            var invoice = _merchelloContext.Services.InvoiceService.GetByKey(invoiceKey);
            var payment = _merchelloContext.Services.PaymentService.GetByKey(paymentKey);

            if (invoice == null)
            {
                var ex = new NullReferenceException(string.Format("Invalid argument exception. Arguments: invoiceKey={0}, paymentKey={1}", invoiceKey, paymentKey));
                LogHelper.Error<SagePayApiController>("Payment not authorized.", ex);
                throw ex;
            }

            // Complete payment with Sagepay

            IThreeDAuthRequest request = sagePayDirectIntegration.ThreeDAuthRequest();
            request.Md = values.MD;
            request.PaRes = values.PaRes;
            IDirectPaymentResult result = sagePayDirectIntegration.ProcessDirect3D(request);

            if (result.Status != ResponseStatus.OK)
            {
                return ShowError(result.StatusDetail);

            }
            // Store some SagePay data in payment
            payment.ReferenceNumber = result.VpsTxId;
            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayTransactionCode, result.VpsTxId);

            // Authorize and save payment
            var authorizeResult = _directProcessor.AuthorizePayment(invoice, payment);
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

            Notification.Trigger("OrderConfirmation", new Merchello.Core.Gateways.Payment.PaymentResult(Attempt<Merchello.Core.Models.IPayment>.Succeed(payment), invoice, true), new[] { invoice.BillToEmail });
            
            
            // Redirect to ReturnUrl (with token replacement for an alternative means of order retrieval)
            var returnUrl = payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.ReturnUrl);
            var response = Request.CreateResponse(HttpStatusCode.Moved);

            Func<string, string> adjustUrl = (url) =>
            {
                if (!url.StartsWith("http")) url = GetWebsiteUrl() + (url[0] == '/' ? "" : "/") + url;
                url = url.Replace("{invoiceKey}", invoice.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                url = url.Replace("{paymentKey}", payment.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                return url;
            };

            var redirectUrl = adjustUrl("/App_Plugins/Merchello.SagePay/3dsecureFinished.aspx?");
            redirectUrl += "&redirect=" + Base64Encode(returnUrl.Replace("%INVOICE%", invoice.Key.ToString().EncryptWithMachineKey()));
            response.Headers.Location = new Uri(redirectUrl);
            return response;
        }

        protected static string GetWebsiteUrl()
        {
            var url = HttpContext.Current.Request.Url;
            var baseUrl = String.Format("{0}://{1}{2}", url.Scheme, url.Host, url.IsDefaultPort ? "" : ":" + url.Port);
            return baseUrl;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
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
