using Liquid.Activation;
using Liquid.Base;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Runtime;
using Liquid.Runtime.Telemetry;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Liquid.OnAzure
{
    /// <summary>
    /// Scheduler job base class 
    /// </summary>
    public class Scheduler : LightJobScheduler, IWorkBenchHealthCheck
    {
        private static readonly int OPERATION_TIME_OUT = 15;

        /// <summary>
        /// Initialize the job scheduler
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            _ = ProcessSubscription();
        }

        private static MessageBrokerConfiguration GetConfigFile<T>(KeyValuePair<MethodInfo, T> item)
        {
            MethodInfo method = item.Key;
            string connectionKey = GetConnectionKey(method);

            if (string.IsNullOrEmpty(connectionKey)) // Load specific settings if provided
                return LightConfigurator.LoadConfig<MessageBrokerConfiguration>($"{nameof(ServiceBus)}");

            return LightConfigurator.LoadConfig<MessageBrokerConfiguration>($"{nameof(ServiceBus)}_{connectionKey}");
        }

        /// <summary>
        /// If  an error occurs in the processing, this method going to called
        /// </summary>
        /// <param name="args">The Exception Received Event</param>
        /// <returns>The task of processs</returns>
        public static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs args)
        {
            if (args.Exception is not ObjectDisposedException &&
                args.Exception is not MessagingEntityDisabledException)
                WorkBench.Telemetry.TrackException(args?.Exception);

            return Task.CompletedTask;
        }

        private static async Task ProcessSubscription()
        {
            if (Jobs.Count <= 0) return;
            try
            {
                var refJob = Jobs.First();
                var config = GetConfigFile(refJob);
                var connectionKey = GetConnectionKey(refJob.Key);

                var dispatchEndpoint = SchedulerMessageBus<ServiceBus>.DispatchEndpoint;
                var subscriptionName = GetSubscriptionName(refJob.Key);

                var schedulerMessageBus = new SchedulerMessageBus<ServiceBus>(connectionKey);

                foreach (var job in Jobs)
                {
                    var j = job.Value;
                    var jobName = job.Key.Name;

                    var message = new JobCommandMSG
                    {
                        CommandType = JobCommandCMD.Register.Code,
                        Microservice = subscriptionName,
                        Job = jobName,
                        Frequency = j.Frequency.Code,
                        PartitionCount = j.PartitionCount,
                        DayOfMonth = j.DayOfMonth,
                        DayOfWeek = j.DayOfWeek,
                        Hour = j.Hour,
                        Minute = j.Minute,
                        Status = LightJobStatus.Running.Code
                    };

                    await schedulerMessageBus.SendCommand(message);
                }

                var _managementClient = new ManagementClient(config.ConnectionString);
                var filterRuleDescription = new RuleDescription("JobFilter", new SqlFilter($"sys.Label = '{subscriptionName}'"));

                try
                {
                    _ = _managementClient.CreateTopicAsync(dispatchEndpoint).Result;
                }
                catch (Exception e) when (e.InnerException is MessagingEntityAlreadyExistsException) { }

                try
                {
                    var subscriptionDescription = new SubscriptionDescription(dispatchEndpoint, subscriptionName);
                    _ = _managementClient.CreateSubscriptionAsync(subscriptionDescription, filterRuleDescription).Result;
                }
                catch (Exception e) when (e.InnerException is MessagingEntityAlreadyExistsException) { }

                var maxConcurrentCalls = GetMaxConcurrentCalls(refJob.Key);

                //Register Trace on the telemetry 
                var subscriptionClient = new SubscriptionClient(config.ConnectionString, dispatchEndpoint, subscriptionName, ReceiveMode.PeekLock, null)
                {
                    OperationTimeout = TimeSpan.FromMinutes(OPERATION_TIME_OUT)
                };

                //Register the method to process receive message
                //The RegisterMessageHandler is validate for all register exist on the queue, without need loop for items
                subscriptionClient.RegisterMessageHandler(
                    async (message, cancellationToken) =>
                    {
                        var dispatchMessage = JsonSerializer.Deserialize<JobDispatchMSG>(Encoding.UTF8.GetString(message.Body), LightGeneralSerialization.IgnoreCase);

                        if (dispatchMessage is null)
                            return;

                        try
                        {
                            var found = TryInvokeProcess(dispatchMessage);

                            await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);

                            if (!found)
                            {
                                dispatchMessage.CommandType = null; // commands are of different types, so mapping directly throws an error
                                var notFoundMessage = JobCommandMSG.FactoryFrom(dispatchMessage);
                                notFoundMessage.CommandType = JobCommandCMD.NotFound.Code;

                                await schedulerMessageBus.SendCommand(notFoundMessage);
                            }
                            else
                            {
                                dispatchMessage.CommandType = null; // commands are of different types, so mapping directly throws an error
                                var ackMessage = JobCommandMSG.FactoryFrom(dispatchMessage);
                                ackMessage.CommandType = JobCommandCMD.Acknowledge.Code;

                                await schedulerMessageBus.SendCommand(ackMessage);
                            }
                        }
                        catch (Exception exRegister)
                        {
                            Exception moreInfo = new LightException($"Exception reading message from topic {dispatchEndpoint} and subscription {subscriptionName}. See inner exception for details. Message={exRegister.Message}", exRegister);
                            //Use the class instead of interface because tracking exceptions directly is not supposed to be done outside AMAW (i.e. by the business code)
                            ((LightTelemetry)WorkBench.Telemetry).TrackException(moreInfo);

                            var reason = exRegister.Message;

                            //If there is a business error or an invalid input, set DeadLetter on register
                            if (subscriptionClient.ReceiveMode == ReceiveMode.PeekLock)
                            {
                                if (exRegister.InnerException is not null)
                                {
                                    reason = $"{reason} \n {exRegister.InnerException?.Message}";

                                    var errorDescription = $"EXCEPTION: {exRegister.InnerException}";
                                    if (errorDescription.Length > 4096)
                                        errorDescription = errorDescription[..4092] + "(..)";

                                    if (exRegister is OptimisticConcurrencyLightException)
                                    {
                                        var inputErrors = (exRegister.InnerException as InvalidInputLightException).InputErrors;

                                        string jsonString = (new
                                        {
                                            critics = new Critic()
                                            {
                                                Code = "OPTIMISTIC_CONCURRENCY_CONFLICT",
                                                Message = exRegister.Message,
                                                Type = CriticType.Error
                                            }
                                        }).ToJsonString();

                                        reason = $"Optimistic conflict error occurred: \n {jsonString}";

                                        await SendToDeadLetter(subscriptionClient, message, reason, errorDescription);
                                    }
                                    else if (exRegister.InnerException is InvalidInputLightException)
                                    {
                                        var inputErrors = (exRegister.InnerException as InvalidInputLightException).InputErrors;

                                        string jsonString = (new { critics = inputErrors }).ToJsonString();
                                        reason = $"Invalid (message) input errors occurred: \n {jsonString}";

                                        await SendToDeadLetter(subscriptionClient, message, reason, errorDescription);
                                    }
                                    else if (exRegister.InnerException is BusinessValidationLightException)
                                    {
                                        var inputErrors = (exRegister.InnerException as BusinessValidationLightException).InputErrors;

                                        string jsonString = (new { critics = inputErrors }).ToJsonString();
                                        reason = $"Critical business errors occurred: \n {jsonString}";

                                        await SendToDeadLetter(subscriptionClient, message, reason, errorDescription);
                                    }
                                    else
                                    {
                                        await SendToDeadLetter(subscriptionClient, message, "General unhandled exception", errorDescription);
                                    }
                                }
                                else
                                {
                                    var errorDescription = $"EXCEPTION: {exRegister}";
                                    if (errorDescription.Length > 4096)
                                        errorDescription = errorDescription[..4092] + "(..)";

                                    await SendToDeadLetter(subscriptionClient, message, "General unhandled exception", errorDescription);
                                }
                            }
                        }

                    }, new MessageHandlerOptions((e) => ExceptionReceivedHandler(e)) { AutoComplete = false, MaxConcurrentCalls = maxConcurrentCalls });
            }
            catch (Exception exception)
            {
                Exception moreInfo = new LightException($"Error setting up subscription consumption from service bus. See inner exception for details. Message={exception.Message}", exception);
                //Use the class instead of interface because tracking exceptions directly is not supposed to be done outside AMAW (i.e. by the business code)
                WorkBench.Telemetry.TrackException(moreInfo);
                Console.WriteLine(exception);
                throw;
            }

            Initialized();
        }

        /// <summary>
        /// Method to run Health Check for Service Bus
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LightHealth.HealthCheckStatus HealthCheck(string serviceKey, string value)
        {
            //try
            //{
            //    foreach (KeyValuePair<MethodInfo, JobAttribute> item in Jobs)
            //    {
            //        TopicClient topicClient = new(GetConfigFile(item).ConnectionString, item.Key.Name, ServiceBus.RetryPolicy);

            //        var scheduledMessageId = topicClient.ScheduleMessageAsync(
            //                new Message(Encoding.UTF8.GetBytes("HealthCheckTestMessage")),
            //                new DateTimeOffset(WorkBench.UtcNow).AddHours(2));

            //        topicClient.CancelScheduledMessageAsync(scheduledMessageId.Result);
            //    }
                return LightHealth.HealthCheckStatus.Healthy;
            //}
            //catch
            //{
            //    return LightHealth.HealthCheckStatus.Unhealthy;
            //}
        }

        private static async Task SendToDeadLetter(SubscriptionClient subscriptionClient, Message message, string reason, string errorDescription)
        {
            await subscriptionClient.DeadLetterAsync(message.SystemProperties.LockToken, reason, errorDescription);

            WorkBench.Telemetry.TrackException(new SchedulerLightException(reason, errorDescription));
        }
    }
}