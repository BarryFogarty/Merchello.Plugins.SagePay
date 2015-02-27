using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Web;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Merchello.Plugin.Payments.SagePay.Models;
using Merchello.Plugin.Payments.SagePay.SagePayService;
using SagePay.IntegrationKit.Messages;
using Umbraco.Core;
using IPayment = Merchello.Core.Models.IPayment;
using IPaymentResult = Merchello.Core.Gateways.Payment.IPaymentResult;

namespace Merchello.Plugin.Payments.SagePay
{
    public class SagePayDirectPaymentProcessor : SagePayPaymentProcessorBase
    {
      
        public SagePayDirectPaymentProcessor(SagePayProcessorSettings settings)
            : base(settings)
        {
            Settings = settings;
        }

		
		/// <summary>
		/// Create a dictionary with credentials for SagePay service.
		/// </summary>
		/// <param name="settings"></param>
		/// <returns></returns>
        //private static Dictionary<string, string> CreateSagePayApiConfig(SagePayProcessorSettings settings)
        //{
        //    return new Dictionary<string, string>
        //            {
        //                {"mode", GetModeString(settings.LiveMode)},
        //                {"account.vendorName", settings.VendorName},
        //                {"account.encyptionPassword", settings.EncyptionPassword},
        //                {"account.apiVersion", settings.ApiVersion}
        //            };
        //}


        /// <summary>
        /// Processes the Authorize and AuthorizeAndCapture transactions
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/> to be paid</param>
        /// <param name="payment">The <see cref="Core.Models.IPayment"/> record</param>
        /// <param name="args"></param>
        /// <returns>The <see cref="Core.Gateways.Payment.IPaymentResult"/></returns>
        public IPaymentResult InitializePayment(IInvoice invoice, IPayment payment, ProcessorArgumentCollection args)
        {
            try
            {            
               ///TODO: Attempt payment here

                return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);

            }
            catch (Exception ex)
            {
                return new PaymentResult(Attempt<IPayment>.Fail(payment, ex), invoice, true);
            }

        }


        //TODO: refactor away to a Service that wraps the SagePay kit horribleness
        private void SetSagePayApiData(IFormPayment request, IInvoice invoice, IPayment payment)
        {
            // Get Merchello data
            //TODO - what if there is no shipping info?  e.g. Classes only - Get from billing?
            var shipmentLineItem = invoice.ShippingLineItems().FirstOrDefault();
            var shipment = shipmentLineItem.ExtendedData.GetShipment<InvoiceLineItem>();
            var shippingAddress = shipment.GetDestinationAddress();
            var billingAddress = invoice.GetBillingAddress(); 

            // Merchello info for callback
            //request.InvoiceKey = invoice.Key;
            //request.PayerId = invoice.Pa
            //request.PaymentKey = payment.Key
            
            // SagePay details
            request.VpsProtocol = Settings.ProtocolVersion;
            request.TransactionType = Settings.TransactionType;
            request.Vendor = Settings.VendorName;
            request.VendorTxCode = SagePayAPIIntegration.GetNewVendorTxCode();
            request.Amount = payment.Amount;
            request.Currency = invoice.CurrencyCode();
            request.Description = "Goods from " + Settings.VendorName;
            
            // TODO:  Is there a basket summary I can access?  Or convert the Basket to a sagepay format

            // Set ReturnUrl and CancelUrl of SagePay request to SagePayApiController.
            Func<string, string> adjustUrl = (url) =>
            {
                if (!url.StartsWith("http")) url = GetWebsiteUrl() + (url[0] == '/' ? "" : "/") + url;
                url = url.Replace("{invoiceKey}", invoice.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                url = url.Replace("{paymentKey}", payment.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                return url;
            };

            request.SuccessUrl = adjustUrl("/umbraco/MerchelloSagePay/SagePayApi/SuccessPayment?InvoiceKey={invoiceKey}&PaymentKey={paymentKey}");
            request.FailureUrl = adjustUrl("/umbraco/MerchelloSagePay/SagePayApi/AbortPayment?InvoiceKey={invoiceKey}&PaymentKey={paymentKey}");

            // Billing details
            request.BillingSurname = billingAddress.TrySplitLastName();
            request.BillingFirstnames = billingAddress.TrySplitFirstName();
            request.BillingAddress1 = billingAddress.Address1;
            request.BillingAddress2 = billingAddress.Address2;
            request.BillingPostCode = billingAddress.PostalCode;
            request.BillingCity = billingAddress.Locality;
            request.BillingCountry = invoice.BillToCountryCode;

            // Shipping details
            request.DeliverySurname = shippingAddress.TrySplitLastName();
            request.DeliveryFirstnames = shippingAddress.TrySplitFirstName();
            request.DeliveryAddress1 = shippingAddress.Address1;
            request.DeliveryCity = shippingAddress.Locality;
            request.DeliveryCountry = shippingAddress.CountryCode;
            request.DeliveryPostCode = shippingAddress.PostalCode;

            //Optional
            //request.CustomerName = cart.Billing.FirstNames + " " + cart.Billing.Surname;
            //request.CustomerEmail = customer.Email;
            //request.VendorEmail = Settings.VendorEmail;
            //request.SendEmail = Settings.SendEmail;

            //request.EmailMessage = Settings.EmailMessage;
            //request.BillingAddress2 = billingAddress.Address2;
            //request.BillingPostCode = billingAddress.PostalCode;
            //request.BillingState = billingAddress.Region;
            //request.BillingPhone = billingAddress.Phone;
            //request.DeliveryAddress2 = shippingAddress.Address2;
            //request.DeliveryPostCode = shippingAddress.PostalCode;
            //request.DeliveryState = shippingAddress.Region;
            //request.DeliveryPhone = shippingAddress.Phone;

        }

    }
}
