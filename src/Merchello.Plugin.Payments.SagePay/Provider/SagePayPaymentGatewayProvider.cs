namespace Merchello.Plugin.Payments.SagePay.Provider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Merchello.Core.Gateways;
    using Merchello.Core.Gateways.Payment;
    using Merchello.Core.Models;
    using Merchello.Core.Services;

    using Umbraco.Core.Cache;
    using Umbraco.Core.Logging;

    /// <summary>
    /// Represents the SagePayPaymentGatewayProvider for Merchello.
    /// </summary>
    [GatewayProviderEditor("SagePay Payment Provider", "Configuration settings for the SagePay Payment Provider", "~/App_Plugins/Merchello.SagePay/payment.sagepay.providersettings.html")]
    [GatewayProviderActivation(Constants.GatewayProviderSettingsKey, "SagePay Payment Provider", "SagePay Payment Provider")]
    public class SagePayPaymentGatewayProvider : PaymentGatewayProviderBase, ISagePayPaymentGatewayProvider
    {
        #region AvailableResources

        /// <summary>
        /// The available resources.
        /// </summary>
        internal static readonly IEnumerable<IGatewayResource> AvailableResources = new List<IGatewayResource>
        {
            new GatewayResource("SagePay Form", "SagePay Form Payment Transaction"),
            new GatewayResource("SagePay Direct", "SagePay Direct Payment Transaction")
        };

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SagePayPaymentGatewayProvider"/> class.
        /// </summary>
        /// <param name="gatewayProviderService">
        /// The <see cref="GatewayProviderService"/>.
        /// </param>
        /// <param name="gatewayProviderSettings">
        /// The <see cref="GatewayProviderSettings"/>.
        /// </param>
        /// <param name="runtimeCacheProvider">
        /// Umbraco's <see cref="IRuntimeCacheProvider"/>.
        /// </param>
        public SagePayPaymentGatewayProvider(
            IGatewayProviderService gatewayProviderService, 
            IGatewayProviderSettings gatewayProviderSettings, 
            IRuntimeCacheProvider runtimeCacheProvider)
            : base(gatewayProviderService, gatewayProviderSettings, runtimeCacheProvider)
        {
        }

        /// <summary>
        /// The list resources offered.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable{IGatewayResource}"/>.
        /// </returns>
        public override IEnumerable<IGatewayResource> ListResourcesOffered()
        {
            return AvailableResources.Where(x => PaymentMethods.All(y => y.PaymentCode != x.ServiceCode));
        }

        /// <summary>
        /// Responsible for creating a 
        /// </summary>
        /// <param name="gatewayResource">
        /// The gateway resource.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="description">
        /// The description.
        /// </param>
        /// <returns>
        /// The <see cref="IPaymentGatewayMethod"/>.
        /// </returns>
        public override IPaymentGatewayMethod CreatePaymentMethod(IGatewayResource gatewayResource, string name, string description)
        {
            var available = ListResourcesOffered().FirstOrDefault(x => x.ServiceCode == gatewayResource.ServiceCode);

            if (available == null)
            {
                var error = new InvalidOperationException("The GatewayResource has already been assigned.");

                LogHelper.Error<SagePayPaymentGatewayProvider>("GatewayResource has alread been assigned", error);

                throw error;
            }

            var attempt = GatewayProviderService.CreatePaymentMethodWithKey(GatewayProviderSettings.Key, name, description, available.ServiceCode);

            if (attempt.Success)
            {
                PaymentMethods = null;

                return available.ServiceCode == "SagePay Form" ?
                    (IPaymentGatewayMethod)new SagePayFormPaymentGatewayMethod(GatewayProviderService, attempt.Result, GatewayProviderSettings.ExtendedData) :
                    new SagePayDirectPaymentGatewayMethod(GatewayProviderService, attempt.Result, GatewayProviderSettings.ExtendedData);
            }

            LogHelper.Error<SagePayPaymentGatewayProvider>(string.Format("Failed to create a payment method name: {0}, description {1}, paymentCode {2}", name, description, available.ServiceCode), attempt.Exception);

            throw attempt.Exception;
        }

        /// <summary>
        /// Get's a <see cref="SagePayFormPaymentGatewayMethod"/> by it's database key.
        /// </summary>
        /// <param name="paymentMethodKey">
        /// The payment method key.
        /// </param>
        /// <returns>
        /// The <see cref="IPaymentGatewayMethod"/>.
        /// </returns>
        public override IPaymentGatewayMethod GetPaymentGatewayMethodByKey(Guid paymentMethodKey)
        {
            var paymentMethod = PaymentMethods.FirstOrDefault(x => x.Key == paymentMethodKey);


            if (paymentMethod != null)
            {
                return paymentMethod.PaymentCode == "SagePay Form" ?
                    (IPaymentGatewayMethod)new SagePayFormPaymentGatewayMethod(GatewayProviderService, paymentMethod, GatewayProviderSettings.ExtendedData) :
                    new SagePayDirectPaymentGatewayMethod(GatewayProviderService, paymentMethod, GatewayProviderSettings.ExtendedData);

            }

            throw new NullReferenceException("Failed to find PaymentMethod with key specified");

        }

        /// <summary>
        /// Get's a <see cref="SagePayFormPaymentGatewayMethod"/> by it's payment code (service code).
        /// </summary>
        /// <param name="paymentCode">
        /// The payment code.
        /// </param>
        /// <returns>
        /// The <see cref="IPaymentGatewayMethod"/>.
        /// </returns>
        public override IPaymentGatewayMethod GetPaymentGatewayMethodByPaymentCode(string paymentCode)
        {
            var paymentMethod = PaymentMethods.FirstOrDefault(x => x.PaymentCode == paymentCode);


            if (paymentMethod != null)
            {
                return paymentMethod.PaymentCode == "SagePay Form" ?
                    (IPaymentGatewayMethod)new SagePayFormPaymentGatewayMethod(GatewayProviderService, paymentMethod, GatewayProviderSettings.ExtendedData) :
                    new SagePayDirectPaymentGatewayMethod(GatewayProviderService, paymentMethod, GatewayProviderSettings.ExtendedData);

            }

            throw new NullReferenceException("Failed to find PaymentMethod with key specified");

        }

      
    }
}