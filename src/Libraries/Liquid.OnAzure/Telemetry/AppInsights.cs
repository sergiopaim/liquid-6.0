using Liquid.Base;
using Liquid.Interfaces;
using Liquid.Runtime;
using Liquid.Runtime.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using System;
using System.Diagnostics;

namespace Liquid.OnAzure
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// The AppInsights class is the lowest level of integration of WorkBench with Azure AppInsights.
    /// It directly provides a client to send the messages to the cloud.
    /// So it is possible to trace all logs, events, traces, exceptions in an aggregated and easy-to-use form.
    /// </summary>
    public class AppInsights : LightTelemetry, ILightTelemetry
    {
        //TelemetryClient is responsible for sending all telemetry requests to the azure.
        //It is still possible to make settings regarding the hierarchy of messages.
        //This setting is changeable as desired by the developer.
        private static TelemetryClient TelemetryClient;

        public new string OperationId
        {
            get => TelemetryClient?.Context?.Operation?.Id;
            set
            {
                if (TelemetryClient?.Context?.Operation is not null)
                    TelemetryClient.Context.Operation.Id = value;

                if (Activity.Current is not null)
                    Activity.Current.SetBaggage("operationId", value);
            }
        }

        public AppInsights() { }

        //TrackEvent is a wrapper that sends messages in the event format to AppInsights.
        public override void TrackEvent(string name, string context = null)
        {
            var eventTelemetry = new EventTelemetry() { Name = name };
            eventTelemetry.Context.Operation.Id = OperationId;

            if (context is not null)
                eventTelemetry.Properties.Add("Context", context);

            TelemetryClient.TrackEvent(eventTelemetry);
        }
        //TrackMetric sends to the AppInsights metrics related to some point of view. 
        //For example, you can measure how much time was spent to persist data in the database.
        public override void TrackMetric(string metricLabel, double value)
        {
            var metric = new MetricTelemetry() { Name = metricLabel, Sum = value };
            metric.Context.Operation.Id = OperationId;

            TelemetryClient.TrackMetric(metric);
        }
        //TrackTrace will be called when it is necessary to make a diagnosis of any specific problems.
        //In this case the trace event can be customized by passing an object with more details of the problem.
        public override void TrackTrace(params object[] trace)
        {
            var traceTelemetry = new TraceTelemetry() { Message = (string)trace?[0] };
            traceTelemetry.Context.Operation.Id = OperationId;

            TelemetryClient.TrackTrace(traceTelemetry);
        }
        //TrackAggregateMetric contains the aggregation logic of just send to the AppInsights when the BeginComputeMetric and EndComputeMeric 
        //is called. With some key is not registred
        public override void TrackAggregateMetric(object metricTelemetry)
        {
            TelemetryClient.TrackMetric((MetricTelemetry)metricTelemetry);
        }

        //TrackException will send the entire monitored exception from WorkBench to AppInsights.
        public override void TrackException(Exception exception)
        {
            var exceptionTelemetry = new ExceptionTelemetry() { Exception = exception };
            exceptionTelemetry.Context.Operation.Id = OperationId;

            TelemetryClient.TrackException(exceptionTelemetry);
        }

        // Initialize will retrieve the authentication token from the configuration file set in "appsettings.json". 
        // Also, it will instantiate a telemetry client to send all Azure portal pro requests.        
        public override void Initialize()
        {
            AppInsightsConfiguration appInsightsConfiguration = LightConfigurator.LoadConfig<AppInsightsConfiguration>("ApplicationInsights");

            TelemetryConfiguration aiConfig = new()
            {
                InstrumentationKey = appInsightsConfiguration.InstrumentationKey
            };

            // enables live metrics
            if (!WorkBench.IsDevelopmentEnvironment)
            {
                QuickPulseTelemetryProcessor processor = null;

                aiConfig.TelemetryProcessorChainBuilder
                    .Use((next) =>
                    {
                        processor = new QuickPulseTelemetryProcessor(next);
                        return processor;
                    })
                    .Build();

                using var QuickPulse = new QuickPulseTelemetryModule();

                // Secure the control channel.
                // This is optional, but recommended.
                QuickPulse.AuthenticationApiKey = appInsightsConfiguration.QuickPulseApiKey;
                QuickPulse.Initialize(aiConfig);
                QuickPulse.RegisterTelemetryProcessor(processor);
            }

            // automatically correlate all telemetry data with request
            aiConfig.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            TelemetryClient ??= new(aiConfig);
            TelemetryClient.Context.Cloud.RoleName = "API";
        }

        //This WrapperTelemetry was created to be an anonymous method, so it can be called by other methods as a function pointer. 
        //It receives as parameters the attributes that will be set for the telemetry client
        private delegate void WrapperTelemetry(string ParentID, object Value, string OperationID, TelemetryClient telemetryClient);

        //Instance of our delgate that will receive the parameters of SetContext and will configure the entire instance of the client.
        private readonly WrapperTelemetry wrapper = (parent, value, operation, telemtry) =>
        {
            telemtry.Context.Operation.ParentId = parent;
            telemtry.Context.Operation.Id = !string.IsNullOrEmpty(operation)
                                                ? operation
                                                : Guid.NewGuid().ToString();

            telemtry.Context.Operation.Name = $"{parent}/{operation}";
        };

        //SetContext is responsible for receiving a context of some method from which it was called, 
        //thus creating a view hierarchy in the AppInsights dashboard. Some parameters can be ommitted.
        //The develop will track the logic that need see on Azure portal. ParentID is necessary to track all events and organize it.
        public override void EnqueueContext(string parentId, object value = null, string operationId = "")
        {
            wrapper.Invoke(parentId, value, operationId ?? OperationId, TelemetryClient);
        }

        // Whenever a SetContext is declared it is necessary to terminate its operations, that is, 
        //when completing all the operations trace, it is necessary to reconfigure all telemetry client changes in order to avoid any data inconsistencies.
        public override void DequeueContext()
        {
            TelemetryClient.Context.Operation.ParentId = null;
            TelemetryClient.Context.Operation.Id = null;
            TelemetryClient.Context.Operation.Name = null;
        }

        /// <summary>
        /// Method to run Health Check for Appinsight telemetry
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override LightHealth.HealthCheckStatus HealthCheck(string serviceKey, string value)
        {
            try
            {
                TelemetryClient.TrackEvent("Method invoked");
                return LightHealth.HealthCheckStatus.Healthy;
            }
            catch
            {
                return LightHealth.HealthCheckStatus.Unhealthy;
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}