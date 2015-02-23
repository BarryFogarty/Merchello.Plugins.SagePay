using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using System.Text;
using Merchello.Plugin.Payments.SagePay.Models;
using SagePay.IntegrationKit.Messages;
using SagePay.IntegrationKit;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// Summary description for SagePayIntegration
/// </summary>
public class SagePayAPIIntegration : SagePayIntegration
{
    static Random random = new Random();
    static SagePayProcessorSettings _settings;

    public SagePayAPIIntegration(SagePayProcessorSettings settings)
    {
        _settings = settings;
    }

    public static string GetNewVendorTxCode()
    {
        TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        // 18 char max -13 chars - 6 chars
        return string.Format("{0}-{1}-{2}",
            _settings.VendorName.Substring(0, Math.Min(18, _settings.VendorName.Length)),
            (long)ts.TotalMilliseconds, random.Next(100000, 999999));
    }

    public static string GetNewRelatedVtx(string pref, string vtx)
    {
        TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        return string.Format("{0}{1}-{2}{3}", pref, vtx.Substring(0, 15), (long)ts.TotalMilliseconds, RandomString(3));
    }

    public static string RandomString(int length)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(Convert.ToChar(random.Next(65, 90)));
        }
        return sb.ToString();
    }
}