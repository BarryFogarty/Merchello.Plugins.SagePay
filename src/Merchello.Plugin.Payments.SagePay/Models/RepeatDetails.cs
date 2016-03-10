using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Plugin.Payments.SagePay.Models
{
    public class RepeatDetails
    {
        /// <summary>
        /// The security key from the transaction to repeat  
        /// </summary>
        public string RelatedSecurityKey { get; set; }

        /// <summary>
        /// The id from the original transaction
        /// </summary>
        public string RelatedVpsTxId { get; set; }

        /// <summary>
        /// The original vendor transaction code
        /// </summary>
        public string RelatedVendorTxCode { get; set; }

        /// <summary>
        /// The original auth code
        /// </summary>
        public string RelatedTxAuthNo { get; set; }

        /// <summary>
        /// The customer's CV2 code (optional)
        /// </summary>
        public string CV2 { get; set; }
   

    }
}
