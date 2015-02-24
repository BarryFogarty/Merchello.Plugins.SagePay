using System;
using SagePay.IntegrationKit;

namespace Merchello.Plugin.Payments.SagePay.Models
{
        public class SagePayProcessorSettings
        {
            // From provider settings dialog data
            public bool LiveMode { get; set; }
            public string VendorName { get; set; }
            public string EncryptionPassword { get; set; }
            public string ReturnUrl { get; set; }
            
            // Hard coded stuff 
            public string ApiVersion = "3.00";
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

            
        }
}
