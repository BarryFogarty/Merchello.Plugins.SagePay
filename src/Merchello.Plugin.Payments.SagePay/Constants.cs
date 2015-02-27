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

            // Stores the Sagepay redirect URL once the transaction post has been registered
            public static string SagePayPaymentUrl = "SagePayPaymentUrl";

            // Stores the receipt page URL to show confirmation of payment 
            //public static string ReturnUrl = "ReturnUrl";

            // Stores the URL to return to if the customer aborts payment on SagePay
            //public static string CancelUrl = "CancelUrl";

            // Flag keys
            public static string PaymentAuthorized = "PaymentAuthorized";
            public static string PaymentCaptured = "PaymentCaptured";
            public static string PaymentCancelled = "PaymentCancelled";
            public static string PaymentCancelInfo = "PaymentCancelInfo";

            // Our generated unique ID for the transaction , e.g. babypotz-1424717223480-230356
            public static string VendorTransactionCode = "VendorTransactionCode";

            // Sagepay unique ID for the transaction 
            public static string SagePayTransactionCode = "SagePayTransactionCode";

            // Sagepay security key for the transaction, used as a key for confirming the MD5 hash signature in the notification POST
            public static string SagePaySecurityKey = "SagePaySecurityKey";

            
        }


    }
}
