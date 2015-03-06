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
    public class SagePayFormPaymentProcessor : SagePayPaymentProcessorBase
    {       
        public SagePayFormPaymentProcessor(SagePayProcessorSettings settings)
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
                // Gather SagePay settings info and formulate it into a Dictionary for posting to the gateway
                var sagePayFormIntegration = new SagePayFormIntegration(Settings);
                var request = sagePayFormIntegration.FormPaymentRequest();
                SetSagePayApiData(request, invoice, payment);

                var errors = sagePayFormIntegration.Validation(request);

                if (errors.Count > 0)
                {
                    return new PaymentResult(Attempt<IPayment>.Fail(payment, base.CreateErrorResult(errors)), invoice, true);
                }

                sagePayFormIntegration.ProcessRequest(request);

                // Use the SagePay methods to encrypt the form so it can be posted

                var content =
                        new FormUrlEncodedContent(
                            new Dictionary<string, string>
						{
							{"VPSProtocol", Settings.ApiVersion},
                            {"TxType", Constants.TransactionType},
                            {"Vendor", Settings.VendorName},
                            //{"Profile", "LOW"},  // Provider setting?
                            {"Crypt", request.Crypt}
                            //{"Crypt", "@97BBE806FEC54747CDC26543072F2BDD7568B80931B3A7BFE835B241DF286D2A26E87331AEE2FF193985E2A74167E2FBADF7775711D9E161F91AECD6D148FBC2D4DA9650AE340957939D31249926EA702CBF7214B379DFFF3465789D0C10DA14FED61E6B204F5D1C7C024BFE78B09D9FA759D93F4A538303BB2DBEDF705F69E8437621A1A244126748BE438F480D8E1923B9ECB1D8234B5548611AF5B3B6C8A94D9E8A613081AC1F918503BA24F44013CB9117F0CBDD326634071AA99CD0B2004356D246E35A14737D6AD95FB93559A57F37C501DF57300895FBD9E6D370E94BE990EACE51C23002DC6BEC2BE0D2D7DF91866A37BACF977F2E338C29424C5A2D44C4218EC6A2DB7961DA8C8C54D89CE5EEC7DE40928EBDFAD1428DBE8C7F3D81E8996FBA14C1C0684B17F61E65FFD7F1F7E94F4DAAEC4380B94E9940524832CFC317E89AE611AFF58F7B4E889AA973CA6ED83364261C5918536A5862E8F379B991C95F3A953F2BB65FB9C308AF0F08AB7B7F773621322EB9C9DBCB98846805A280DB3C043DFA44BCE23452BC4C19B0F27D2E81BC183C77F98B78D8C50D9E3FB377AE1FE638EC898330FB5D08D3AC4CB379C0BB0333F9A83722387FC7C52B2FC50EB4718E3AA13F5CD87822FA42D128A9E2830F550F1450A410D5859ED7C2327C6F9BCF12AC3EF28497BF16F209FE3DD5F9B53613EE4F34BBF413C75F2D615291508AF92B38B60D485FC7C71AB6A84691B05F14454CA3316935427E0B0ECD0047DD7FDC9999D4C56107D54EF3B1D913FDA49E4B490ABAE29C08E2E88941BE2F6D8DD494F0E2008915441C45CD7521DECAA7BC6B9B81B896ED50377DA5036AA87A112068E24CDC486D4E42C92A546940E4AC1EF1D95171DEAA6BCA0C7330F9EB67003F7D5C06B2372FBC708BBE3E60F09FD364A172ED5B039CCD01782001B113A8C1249CF4B19C6D18711B13426E05008FD246447290AD2D059E854FF2777D6783E3EF8A2DEBB61D1FE98624DA28EC759AFC0EE4EC55D77602A0A19D05A05EAC704183A2B2F90A53B881230353EF6411033BD2C1F85FE3089B49A1BADD0CC7793131E0F3E327FDBB49D54B4E267172E628C89836D6582D36212859F5BB8CFE7F6D8BEE01ACBD72B53D1A6BA2A43C0C063EF73957E433A94B7BC11B632B03BD779CD248084F0D27D7D16EC72B321F3CD6766667673B1DAF4E0E00C6F179F91B5498F2EC97F82ECF1AFFADECF6D6BDB610F21B1373EC6FE7A6FBB388E4F0F90FE11F5E098362E01EEBDD996B6CAB278124F22C6FAE995D64FFB56FF27F20E9A4806ACDA129823958C2FF6FBAA05AB275854C9AA4EE044BCB108E341A86189E7D9D7BCED123802A0DB5F570D0CDC696824220CB6C1FE8D4703CE9FC753788488E6DC9AB8523909033515FAEEC3ADF5F19557D51608D9CE8967C6BD8168375C4CAF20188292038A4426057570D18099A0A17383BC6DC89B6D05BF8B8191710A13B9F46DA55377A72712BBFE9613D72DEEC6B958A35F53D7BA4548314E5322DBA2387A2AE53EE66163B493EE49E85381DB862520D73A5F2E4A4679BB715E0F3B617FF1E284B5E2BF6AF5EE065934357A11A87655A1BE7DEC751A770C7097FE68652488C499B94AE40DA480F8483E5A04BEF3678637AF6D7F7D126D77DE95FB67E6972CE"}                  
						});


                // Post the form to SagePay VSP
                //var formPaymentUrl = string.Format("https://{0}.sagepay.com/showpost/showpost.asp", GetModeString(Settings.LiveMode));
                var formPaymentUrl = string.Format("https://{0}.sagepay.com/gateway/service/vspform-register.vsp", GetModeString(Settings.LiveMode));
                var result = new HttpClient().PostAsync(formPaymentUrl, content).Result;

                // Store transaction details in ExtendedData
                payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.VendorTransactionCode, request.VendorTxCode);
                
                // Flag an error in the backoffice if the result is not successful
                if (!result.IsSuccessStatusCode)
                {
                    return new PaymentResult(Attempt<IPayment>.Fail(payment, CreateErrorResult(result.RequestMessage.Content)), invoice, true);
                }

                // Process the response from SagePay - this contains the redirect URL for the customer to complete their payment.
                var redirectUrl = result.RequestMessage.RequestUri.ToString();
                payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayPaymentUrl, redirectUrl);

                // Store our site return URL and cancel URL in extendedData so it can be used in the callback
                var returnUrl = GetWebsiteUrl() + Settings.ReturnUrl;
                payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.ReturnUrl, returnUrl);

                var cancelUrl = GetWebsiteUrl() + Settings.CancelUrl;
                payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.CancelUrl, cancelUrl);

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
            request.VendorTxCode = SagePayFormIntegration.GetNewVendorTxCode();
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
