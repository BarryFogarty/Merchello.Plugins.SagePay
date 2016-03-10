using Merchello.Core.Gateways.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Plugin.Payments.SagePay.Models
{
    public static class RepeatDetailsExtensions
    {
        public static ProcessorArgumentCollection AsProcessorArgumentCollection(this RepeatDetails repeatDetails)
        {
            return new ProcessorArgumentCollection()
            {
                { "relatedSecurityKey", repeatDetails.RelatedSecurityKey },
                { "relatedVpsTxId", repeatDetails.RelatedVpsTxId },
                { "relatedVendorTxCode", repeatDetails.RelatedVendorTxCode },
                { "relatedTxAuthNo", repeatDetails.RelatedTxAuthNo },
                { "cv2", repeatDetails.CV2 ?? "" }
            };
        }

        public static RepeatDetails AsRepeatDetails(this ProcessorArgumentCollection args)
        {
            return new RepeatDetails()
            {
                RelatedSecurityKey = args.ArgValue("relatedSecurityKey"),
                RelatedVpsTxId = args.ArgValue("relatedVpsTxId"),
                RelatedVendorTxCode = args.ArgValue("relatedVendorTxCode"),
                RelatedTxAuthNo = args.ArgValue("relatedTxAuthNo"),
                CV2 = args.ArgValue("cv2")
            };
        }


        private static string ArgValue(this ProcessorArgumentCollection args, string key)
        {
            return args.ContainsKey(key) ? args[key] : string.Empty;
        }
    }
}
