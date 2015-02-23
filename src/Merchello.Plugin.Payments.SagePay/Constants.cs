using System;

namespace Merchello.Plugin.Payments.SagePay
{
    public class Constants
    {
        /// <summary>
        /// Gets the gateway provider settings key.
        /// </summary>
        public const string GatewayProviderSettingsKey = "31FDA06C-A9D3-4DFB-9773-63724E0977B4";

        public const string TransactionType = "PAYMENT";

        public static class ExtendedDataKeys
        {
            public static string ProcessorSettings = "sagePayProviderSettings";
            public static string CaptureAmount = "CaptureAmount";

            public static string SuccessCallbackUrl = "successCallbackUrl";
            public static string FailureCallbackUrl = "failureCallbackUrl";

        }

        //public static class ProcessorPaymentUrls
        //{
        //    public static class FormPayment
        //    {

        //    }

            

        //}


    }
}
