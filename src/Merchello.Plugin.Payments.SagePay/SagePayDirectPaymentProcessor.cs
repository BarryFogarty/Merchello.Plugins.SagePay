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
using SagePay.IntegrationKit;
using Merchello.Core.Services;

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
                var sagePayDirectIntegration = new SagePayAPIIntegration(Settings);
                var request = sagePayDirectIntegration.DirectPaymentRequest();

                var creditCard = args.AsCreditCard();
                 
                SetSagePayApiData(request, invoice, payment, creditCard);                

                // Incredibly frustratingly, the sagepay integration kit provided by sagepay does not support paypal integration with sagepay direct so if this is a paypal transaction, we have to build the post manually.

                if (request.CardType == CardType.PAYPAL)
                {
                    using (var client = new HttpClient())
                    {
                        var values = new Dictionary<string, string> { };

                        // Build the post and fix the values that the integration kit breaks...
                        foreach (var property in request.GetType().GetAllProperties())
                        {
                            if (property.CanRead && property.GetValue(request) != null)
                            {
                                if (property.Name == "VpsProtocol")
                                {
                                    values.Add(property.Name, "3.00");
                                }
                                else if (property.Name == "TransactionType")
                                {
                                    values.Add("TxType", "PAYMENT");
                                }
                                else if (property.Name == "Amount")
                                {
                                    // If amount has no decimal place, the property.getvalue method adds lots of zeros
                                    var amount = property.GetValue(request).ToString();
                                    var amountDec = decimal.Parse(amount);

                                    values.Add(property.Name, amountDec.ToString("n2"));

                                }
                                else
                                {
                                    values.Add(property.Name, property.GetValue(request).ToString());
                                }
                            }
                        }

                        var content = new FormUrlEncodedContent(values);

                        var response = client.PostAsync(string.Format("https://{0}.sagepay.com/gateway/service/vspdirect-register.vsp", Settings.Environment), content).Result;

                        var responseString = response.Content.ReadAsStringAsync().Result.Replace("\r\n", "&");

                        NameValueCollection sagePayResponseValues = HttpUtility.ParseQueryString(responseString);
                        if (sagePayResponseValues["Status"] == "PPREDIRECT")
                        {
                            var redirectUrl = sagePayResponseValues["PayPalRedirectURL"] + "&token=" + sagePayResponseValues["token"];
                            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayPaymentUrl, redirectUrl);

                            var returnUrl = GetWebsiteUrl() + Settings.ReturnUrl;
                            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.ReturnUrl, returnUrl);

                            var cancelUrl = GetWebsiteUrl() + Settings.CancelUrl;
                            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.CancelUrl, cancelUrl);

                            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayTransactionCode, sagePayResponseValues["VPSTxId"]);

                            return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);

                        }
                        else
                        {
                            return new PaymentResult(Attempt<IPayment>.Fail(payment, new Exception(sagePayResponseValues["StatusDetail"])), invoice, true);
                        }


                    }

                }

                IDirectPaymentResult result = sagePayDirectIntegration.ProcessDirectPaymentRequest(request, string.Format("https://{0}.sagepay.com/gateway/service/vspdirect-register.vsp", Settings.Environment));
                

                
                if (result.Status == ResponseStatus.OK)
                {
                    payment.Collected = true;
                    payment.Authorized = true;
                    GatewayProviderService service = new GatewayProviderService();
                    service.ApplyPaymentToInvoice(payment.Key, invoice.Key, Core.AppliedPaymentType.Debit, "SagePay: capture authorized", invoice.Total);
                    return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
                }
                else if (result.Status == ResponseStatus.THREEDAUTH)
                {
                    
                    // For 3D Secure we have to show a client side form which posts to the ACS url. 
                      Func<string, string> adjustUrl = (url) =>
                {
                    if (!url.StartsWith("http")) url = GetWebsiteUrl() + (url[0] == '/' ? "" : "/") + url;
                    url = url.Replace("{invoiceKey}", invoice.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                    url = url.Replace("{paymentKey}", payment.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                    return url;
                };

                    var redirectUrl = adjustUrl("/App_Plugins/Merchello.SagePay/3dsecureRedirect.aspx?");
                    redirectUrl += "acsurl=" + Base64Encode(result.AcsUrl);
                    redirectUrl += "&PaReq=" + Base64Encode(result.PaReq);
                    redirectUrl += "&MD=" + Base64Encode(result.Md);
                    redirectUrl += "&TermUrl=" + Base64Encode(adjustUrl("/umbraco/MerchelloSagePay/SagePayApi/ThreeDSecureCallback?InvoiceKey={invoiceKey}&PaymentKey={paymentKey}"));
                    payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.ThreeDSecureUrl, redirectUrl);

                    var returnUrl = GetWebsiteUrl() + Settings.ReturnUrl;
                    payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.ReturnUrl, returnUrl);

                    var cancelUrl = GetWebsiteUrl() + Settings.CancelUrl;
                    payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.CancelUrl, cancelUrl);

                    return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);

                }
                //else if (result.Status == ResponseStatus.PPREDIRECT)
                //{
                //    var redirectUrl = result.PayPalRedirectUrl + "&token=" + result.Token;
                //    payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.SagePayPaymentUrl, redirectUrl);

                //    var returnUrl = GetWebsiteUrl() + Settings.ReturnUrl;
                //    payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.ReturnUrl, returnUrl);

                //    var cancelUrl = GetWebsiteUrl() + Settings.CancelUrl;
                //    payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.CancelUrl, cancelUrl);

                //    return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
                //}
                else
                {
                    return new PaymentResult(Attempt<IPayment>.Fail(payment, new Exception(result.StatusDetail)), invoice, true);
                }

            }
            catch (Exception ex)
            {
                return new PaymentResult(Attempt<IPayment>.Fail(payment, ex), invoice, true);
            }

        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        //TODO: refactor away to a Service that wraps the SagePay kit horribleness
        private void SetSagePayApiData(IDirectPayment request, IInvoice invoice, IPayment payment, CreditCard creditCard)
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

            request.CardType = (CardType)Enum.Parse(typeof(CardType), creditCard.CreditCardType);
            request.CardHolder = creditCard.CardholderName;
            request.CardNumber = creditCard.CardNumber;
            request.ExpiryDate = creditCard.ExpireMonth + creditCard.ExpireYear;
            request.Cv2 = creditCard.CardCode;

            request.Apply3dSecure = 0;

            if (request.CardType == CardType.PAYPAL)
            {
                Func<string, string> adjustUrl = (url) =>
                {
                    if (!url.StartsWith("http")) url = GetWebsiteUrl() + (url[0] == '/' ? "" : "/") + url;
                    url = url.Replace("{invoiceKey}", invoice.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                    url = url.Replace("{paymentKey}", payment.Key.ToString(), StringComparison.InvariantCultureIgnoreCase);
                    return url;
                };

                request.PayPalCallbackUrl = adjustUrl("/umbraco/MerchelloSagePay/SagePayApi/PaypalCallback?InvoiceKey={invoiceKey}&PaymentKey={paymentKey}");
            }

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
