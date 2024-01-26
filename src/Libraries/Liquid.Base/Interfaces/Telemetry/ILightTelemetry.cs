using System;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ILightTelemetry : IWorkBenchHealthCheck
    {
        public string OperationId { get; set; }
        void TrackTrace(params object[] trace);
        void TrackEvent(string name, string context = null);
        void TrackMetric(string metricLabel, double value);
        void TrackException(Exception exception);

        void ComputeMetric(string metricLabel, double value);
        void BeginMetricComputation(string metricLabel);
        void EndMetricComputation(string metricLabel);
        void EnqueueContext(string parentId, object value = null, string operationId = "");        
        void DequeueContext();
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
