using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Plugin.Payments.SagePay.Models
{
        public class ApiSettings
        {

            public bool LiveMode { get; set; }
            public string VendorName { get; set; }
            public string EncyptionPassword { get; set; }
            //public string VendorUrl { get; set; }
            //public string Currency { get; set; }

            public string ApiVersion { get; set; }
        }
}
