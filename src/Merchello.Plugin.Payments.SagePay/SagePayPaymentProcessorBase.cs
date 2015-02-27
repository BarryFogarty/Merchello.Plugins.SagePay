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
    public class SagePayPaymentProcessorBase
    {
        public SagePayProcessorSettings Settings { get; set; }

		public SagePayPaymentProcessorBase(SagePayProcessorSettings settings)
        {
            Settings = settings;
        }

		/// <summary>
		/// Get the absolute base URL for this website
		/// </summary>
		/// <returns></returns>
		protected static string GetWebsiteUrl()
		{
			var url = HttpContext.Current.Request.Url;
			var baseUrl = String.Format("{0}://{1}{2}", url.Scheme, url.Host, url.IsDefaultPort ? "" : ":" + url.Port);
			return baseUrl;
		}

		/// <summary>
		/// Get the mode string: "live" or "test".
		/// </summary>
		/// <param name="liveMode"></param>
		/// <returns></returns>
		protected static string GetModeString(bool liveMode)
		{
			return (liveMode ? "live" : "test");
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
     

        public IPaymentResult AuthorizePayment(IInvoice invoice, IPayment payment)
        {         
            try
            {
                payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.PaymentAuthorized, "true");
                payment.Authorized = true;
            }
            catch (Exception ex)
            {
                return new PaymentResult(Attempt<IPayment>.Fail(payment, ex), invoice, false);
            }

            return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
        }

        public IPaymentResult CapturePayment(IInvoice invoice, IPayment payment, decimal amount, bool isPartialPayment)
        {
            try
            {
                payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.PaymentCaptured, "true");
                payment.Collected = true;
            }
            catch (Exception ex)
            {
                return new PaymentResult(Attempt<IPayment>.Fail(payment, ex), invoice, false);
            }

            return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
        }

        protected Exception CreateErrorResult(HttpContent errors)
        {
            //var errorText = errors.Count == 0 ? "Unknown error" : ("- " + string.Join("\n- ", errors.Select(item => item.LongMessage)));
            return new Exception(errors.ToString());
        }

        protected Exception CreateErrorResult(NameValueCollection errors)
        {
            return new Exception(errors.Cast<string>().Select(e => errors[e]).ToString());
        }



    }
}
