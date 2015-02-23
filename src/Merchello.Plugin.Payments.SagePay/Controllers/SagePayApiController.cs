using System;
using Umbraco.Core.Logging;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using Merchello.Core;
using Merchello.Core.Services;
using Merchello.Plugin.Payments.SagePay.Provider;
using System.Text;

namespace Merchello.Plugin.Payments.SagePay.Controllers
{
    /// <summary>
    /// The SagePay API controller.
    /// </summary>
    [PluginController("MerchelloSagePay")]
    public class SagePayApiController : UmbracoApiController
    {
        /// <summary>
        /// Merchello context
        /// </summary>
        private readonly IMerchelloContext _merchelloContext;

        /// <summary>
        /// The SagePay payment processor.
        /// </summary>
        private readonly SagePayPaymentProcessor _processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SagePayApiController"/> class.
        /// </summary>
        public SagePayApiController()
            : this(MerchelloContext.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SagePayApiController"/> class.
        /// </summary>
        /// <param name="merchelloContext">
        /// The <see cref="IMerchelloContext"/>.
        /// </param>
        public SagePayApiController(IMerchelloContext merchelloContext)
        {
            if (merchelloContext == null) throw new ArgumentNullException("merchelloContext");

            var providerKey = new Guid(Constants.GatewayProviderSettingsKey);
            var provider =
                (SagePayPaymentGatewayProvider) merchelloContext.Gateways.Payment.GetProviderByKey(providerKey);

            if (provider == null)
            {
                var ex =
                    new NullReferenceException(
                        "The SagePayPaymentGatewayProvider could not be resolved.  The provider must be activiated");
                LogHelper.Error<SagePayApiController>("SagePayPaymentGatewayProvider not activated.", ex);
                throw ex;
            }

            _merchelloContext = merchelloContext;
            _processor = new SagePayPaymentProcessor(provider.ExtendedData.GetProcessorSettings());
        }


    }
}
