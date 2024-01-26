using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Diagnostics;
using System.Net;

namespace Liquid.OnAzure.Telemetry
{
    internal class LightTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = "API";

            var operationId = Activity.Current?.GetBaggageItem("operationId");
            if (operationId is not null)
                telemetry.Context.Operation.Id = operationId;

            var requestTelemetry = telemetry as RequestTelemetry;
            if (requestTelemetry is not null &&
                Int32.TryParse(requestTelemetry.ResponseCode, out int code))
            {
                if (code == 0)
                {
                    requestTelemetry.Success = true;
                    // One can search for the below property in Aplication Insights
                    requestTelemetry.Properties["OverriddenHTTPCodes"] = "true";
                }

                switch ((HttpStatusCode)code)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.NotFound:
                        requestTelemetry.Success = true;
                        requestTelemetry.Properties["OverriddenHTTPCodes"] = "true";
                        break;
                    default:
                        // else leave the SDK to set the Success property
                        break;
                }
            }
        }
    }
}
