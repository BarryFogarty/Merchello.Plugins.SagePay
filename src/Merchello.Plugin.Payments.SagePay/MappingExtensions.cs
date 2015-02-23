using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Merchello.Core.Models;
using Merchello.Plugin.Payments.SagePay.Models;
using Newtonsoft.Json;

namespace Merchello.Plugin.Payments.SagePay
{
    public static class MappingExtensions
    {

        /// <summary>
        /// Saves the processor settings to an extended data collection
        /// </summary>
        /// <param name="extendedData">The <see cref="ExtendedDataCollection"/></param>
        /// <param name="processorSettings">The <see cref="SagePayProcessorSettings"/> to be serialized and saved</param>
        public static void SaveProcessorSettings(this ExtendedDataCollection extendedData, SagePayProcessorSettings processorSettings)
        {
            var settingsJson = JsonConvert.SerializeObject(processorSettings);

            extendedData.SetValue(Constants.ExtendedDataKeys.ProcessorSettings, settingsJson);
        }

        /// <summary>
        /// Get the processor settings from the extended data collection
        /// </summary>
        /// <param name="extendedData">The <see cref="ExtendedDataCollection"/></param>
        /// <returns>The deserialized <see cref="SagePayProcessorSettings"/></returns>
        public static SagePayProcessorSettings GetProcessorSettings(this ExtendedDataCollection extendedData)
        {
            if (!extendedData.ContainsKey(Constants.ExtendedDataKeys.ProcessorSettings)) return new SagePayProcessorSettings();

            return
                JsonConvert.DeserializeObject<SagePayProcessorSettings>(
                    extendedData.GetValue(Constants.ExtendedDataKeys.ProcessorSettings));
        }
    }
}
