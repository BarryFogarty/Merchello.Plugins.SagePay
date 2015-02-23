using System;
using SagePay.IntegrationKit;

namespace Merchello.Plugin.Payments.SagePay.Models
{
        public class SagePayProcessorSettings
        {

            public bool LiveMode { get; set; }
            public string VendorName { get; set; }
            public string EncryptionPassword { get; set; }
            public string SuccessCallbackUrl { get; set; }
            public string FailureCallbackUrl { get; set; }
            public string ApiVersion { get; set; }

            public ProtocolVersion ProtocolVersion
            {
                get
                {
                    return (ProtocolVersion)Enum.Parse(typeof(ProtocolVersion), "V_" + this.ApiVersion.Replace(".", ""));
                }
            }

            public TransactionType TransactionType
            {
                get
                {
                    return (TransactionType)Enum.Parse(typeof(TransactionType), Constants.TransactionType);
                }
            }

            //public string VendorUrl { get; set; }
            //public string Currency { get; set; }

            
        }
}
