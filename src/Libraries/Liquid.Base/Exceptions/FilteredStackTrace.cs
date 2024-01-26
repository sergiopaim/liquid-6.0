using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Liquid.Base
{
    /// <summary>
    /// Extension for handling exceptions
    /// </summary>
    public static class FilteredStackTraceExtensions
    {
        /// <summary>
        /// Filters the stack trace to only relevant ones
        /// </summary>
        /// <param name="exception">The exception to get the stack trace filtered</param>
        public static void FilterRelevantStackTrace(this Exception exception)
        {
            FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString",
                                                                          BindingFlags.Instance | BindingFlags.NonPublic);
            remoteStackTraceString.SetValue(exception, FilteredStackTrace.Filter(exception.StackTrace));
        }
    }

    internal class FilteredStackTrace
    {
        internal static string Filter(string toFilter)
        {
            List<string> filtered = new();

            if (toFilter is not null)
                filtered.AddRange(toFilter.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            else
            {
                StackTrace currentStackTrace = new();
                filtered.AddRange(currentStackTrace.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            }

            filtered.RemoveAll(x => x.Contains("Liquid.Base") ||
                                    x.StartsWith("   at Swashbuckle.AspNetCore.") ||
                                    x.StartsWith("   at Microsoft.AspNetCore.") ||
                                    x.StartsWith("   at Liquid.Middleware.") ||
                                    x.StartsWith("   at Liquid.Domain.WorkbenchMiddleware.") ||
                                    x.StartsWith("   at Liquid.Runtime.OpenApiMiddleware.") ||
                                    x.StartsWith("   at Liquid.Runtime.Telemetry.TelemetryMiddleware.") ||
                                    x.StartsWith("   at System.Runtime.") ||
                                    x.StartsWith("   at System.Threading"));
            return string.Join(Environment.NewLine, filtered.ToArray());
        }
    }
}