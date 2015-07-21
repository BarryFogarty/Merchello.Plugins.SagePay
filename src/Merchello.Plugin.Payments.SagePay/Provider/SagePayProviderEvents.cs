using Merchello.Core.Models;
using Merchello.Core.Services;
using Merchello.Plugin.Payments.SagePay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;

namespace Merchello.Plugin.Payments.SagePay.Provider
{
    class SagePayProviderEvents : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            base.ApplicationStarted(umbracoApplication, applicationContext);

            LogHelper.Info<SagePayProviderEvents>("Initializing Sagepay payment gateway provider registration binding events");

            GatewayProviderService.Saving += GatewayProviderServiceOnSaved;
        }

        private void GatewayProviderServiceOnSaved(IGatewayProviderService sender, SaveEventArgs<IGatewayProviderSettings> args)
        {
            var key = new Guid(Constants.GatewayProviderSettingsKey);
            var provider = args.SavedEntities.FirstOrDefault(x => key == x.Key && !x.HasIdentity);
            if (provider == null) return;

            MappingExtensions.SaveProcessorSettings(provider.ExtendedData, new SagePayProcessorSettings());
        }
    }
}
