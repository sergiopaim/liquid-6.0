using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace Liquid.Runtime.Telemetry
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class LightTelemetryDependencyFilter : ITelemetryProcessor
    {
        public ITelemetryProcessor Next { get; set; }

        // next will point to the next TelemetryProcessor in the chain.
        public LightTelemetryDependencyFilter(ITelemetryProcessor next)
        {
            Next = next;
        }

        public void Process(ITelemetry item)
        {
            // To filter out an item, return without calling the next processor.
            if (!OKtoSend(item))
                return; 

            Next.Process(item);
        }

        //only sends unsuccess dependencies
        public bool OKtoSend(ITelemetry item)
        {
            var dependency = item as DependencyTelemetry;
            if (dependency == null) 
                return true;

            return dependency.Success != true;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}