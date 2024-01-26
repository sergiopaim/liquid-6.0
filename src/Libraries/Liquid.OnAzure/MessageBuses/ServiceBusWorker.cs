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
using System.Threading.Tasks;

namespace Liquid.OnAzure
{
    /// <summary>
    /// Implementation of the communication component between queues and topics of the Azure, this class is specific to azure
    /// </summary>
    public class ServiceBusWorker : LightWorker, IWorkBenchHealthCheck
    {
        /// <summary>
        /// Implementation of the start process queue and process topic. It must be called  parent before start processes.
        /// </summary>
        public override void Initialize()
        {
            Console.WriteLine("Starting Service Bus Worker");
            base.Initialize();
            ProcessQueue();
            ProcessSubscription();
            Console.WriteLine("Service Bus Worker started");
        }

        /// <summary>
        /// Implementation of connection service bus
        /// </summary>
        /// <typeparam name="T">Type of the queue or topic</typeparam>
        /// <param name="item">Item queue or topic</param>
        /// <returns>StringConnection of the ServiceBus</returns>
        private static string GetConnection<T>(KeyValuePair<MethodInfo, T> item)
        {
            MethodInfo method = item.Key;
            string connectionKey = GetKeyConnection(method);
            ServiceBusConfiguration config = null;
            if (string.IsNullOrEmpty(connectionKey)) // Load specific settings if provided
                config = LightConfigurator.LoadConfig<ServiceBusConfiguration>($"{nameof(ServiceBus)}");
            else
                config = LightConfigurator.LoadConfig<ServiceBusConfiguration>($"{nameof(ServiceBus)}_{connectionKey}");

            return config.ConnectionString;
        }


        /// <summary>
        /// If  an error occurs in the processing, this method going to called
        /// </summary>
        /// <param name="args">The Exception Received Event</param>
        /// <returns>The task of processs</returns>
        public static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs args)
        {
            if (args.Exception is not ObjectDisposedException &&
                args.Exception is not MessagingEntityDisabledException &&
                args.Exception is not OperationCanceledException)
                WorkBench.Telemetry.TrackException(args?.Exception);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process the queue defined on the Azure 
        /// </summary>
        /// <returns>The task of Process Queue</returns> 
        public void ProcessQueue()
        {
            try
            {
                foreach (var queue in Queues)
                {
                    MethodInfo method = GetMethod(queue);
                    string queueName = queue.Value.QueueName;
                    ReceiveMode receiveMode = (queue.Value.DeleteAfterRead) ? ReceiveMode.ReceiveAndDelete : ReceiveMode.PeekLock;
                    int maxConcurrentCalls = queue.Value.MaxConcurrentCalls;

                    var _managementClient = new ManagementClient(GetConnection(queue));

                    try
                    {
                        _ = _managementClient.CreateQueueAsync(queueName).Result;
                    }
                    catch (Exception e) when (e.InnerException is MessagingEntityAlreadyExistsException) { }

                    var queueClient = new QueueClient(GetConnection(queue), queueName, receiveMode, ServiceBus.RetryPolicy);

                    //Register the method to process receive message
                    //The RegisterMessageHandler is validate for all register exist on the queue, without need loop for items
                    queueClient.RegisterMessageHandler(
                        async (message, token) =>
                        {
                            try
                            {
                                InvokeProcess(method, message.Body);
                                await queueClient.CompleteAsync(message.SystemProperties.LockToken);
                            }
                            catch (Exception ex)
                            {
                                Exception moreInfo = new LightException($"Exception reading message from queue {queueName}. See inner exception for details. Message={ex.Message}", ex);
                                //Use the class instead of interface because tracking exceptions directly is not supposed to be done outside AMAW (i.e. by the business code)
                                ((LightTelemetry)WorkBench.Telemetry).TrackException(moreInfo);

                                var reason = $"{ex.Message}";

                                //If there is a business error or an invalid input, set DeadLetter on register
                                if (queueClient.ReceiveMode == ReceiveMode.PeekLock)
                                {

                                    if (ex.InnerException is not null)
                                    {
                                        reason = $"{reason} \n {ex.InnerException?.Message}";

                                        var errorDescription = $"EXCEPTION: {ex.InnerException}";
                                        if (errorDescription.Length > 4096)
                                            errorDescription = errorDescription[..4092] + "(..)";

                                        if (ex is OptimisticConcurrencyLightException)
                                        {
                                            var inputErrors = (ex.InnerException as InvalidInputLightException).InputErrors;

                                            string jsonString = (new
                                            {
                                                critics = new Critic()
                                                {
                                                    Code = "OPTIMISTIC_CONCURRENCY_CONFLICT",
                                                    Message = ex.Message,
                                                    Type = CriticType.Error
                                                }
                                            }).ToJsonString();

                                            reason = $"Optimistic conflict error occurred: \n {jsonString}";

                                            await SendToDeadLetter(queueClient, message, reason, errorDescription);
                                        }
                                        else if (ex.InnerException is InvalidInputLightException)
                                        {
                                            var inputErrors = (ex.InnerException as InvalidInputLightException).InputErrors;

                                            string jsonString = (new { critics = inputErrors }).ToJsonString();
                                            reason = $"Invalid (message) input errors occurred: \n {jsonString}";

                                            await SendToDeadLetter(queueClient, message, reason, errorDescription);
                                        }
                                        else if (ex.InnerException is BusinessValidationLightException)
                                        {
                                            var inputErrors = (ex.InnerException as BusinessValidationLightException).InputErrors;

                                            string jsonString = (new { critics = inputErrors }).ToJsonString();
                                            reason = $"Critical business errors occurred: \n {jsonString}";

                                            await SendToDeadLetter(queueClient, message, reason, errorDescription);
                                        }
                                        else
                                        {
                                            await SendToDeadLetter(queueClient, message, "General unhandled exception", errorDescription);
                                        }
                                    }
                                    else
                                    {
                                        var errorDescription = $"EXCEPTION: {ex}";
                                        if (errorDescription.Length > 4096)
                                            errorDescription = errorDescription[..4092] + "(..)";

                                        await SendToDeadLetter(queueClient, message, "General unhandled exception", errorDescription);
                                    }
                                }
                            }
                        },
                        new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = maxConcurrentCalls, AutoComplete = false });
                }
            }
            catch (Exception exception)
            {
                Exception moreInfo = new LightException($"Error setting up queue consumption from service bus. See inner exception for details. Message={exception.Message}", exception);
                //Use the class instead of interface because tracking exceptions directly is not supposed to be done outside AMAW (i.e. by the business code)
                WorkBench.Telemetry.TrackException(moreInfo);
            }
        }

        /// <summary>
        /// Method created to connect and process the Topic/Subscription in the azure.
        /// </summary>
        /// <returns></returns>
        private void ProcessSubscription()
        {
            const string FILTER_RULE_NAME = "SqlFilter";

            try
            {
                foreach (var topic in Topics)
                {

                    MethodInfo method = GetMethod(topic);

                    var args = method.CustomAttributes.Where(a => a.AttributeType == typeof(TopicAttribute)).FirstOrDefault().ConstructorArguments.LastOrDefault();
                    string sqlFilterArgs = args.ArgumentType == typeof(string) ? args.Value.ToString() : "1 = 1";
                    if (string.IsNullOrEmpty(sqlFilterArgs))
                        sqlFilterArgs = "1 = 1";

                    string topicName = topic.Value.TopicName;
                    string subscriptionName = topic.Value.Subscription;

                    var _managementClient = new ManagementClient(GetConnection(topic));
                    var filterRuleDescription = new RuleDescription(FILTER_RULE_NAME, new SqlFilter(sqlFilterArgs));

                    try
                    {
                        _ = _managementClient.CreateTopicAsync(topicName).Result;
                    }
                    catch (Exception e) when (e.InnerException is MessagingEntityAlreadyExistsException) { }

                    try
                    {
                        var subscriptionDescription = new SubscriptionDescription(topicName, subscriptionName);
                        _ = _managementClient.CreateSubscriptionAsync(subscriptionDescription, filterRuleDescription).Result;
                    }
                    catch (Exception e) when (e.InnerException is MessagingEntityAlreadyExistsException)
                    {
                        RuleDescription rule = null;
                        try
                        {
                            rule = _managementClient.GetRuleAsync(topicName, subscriptionName, FILTER_RULE_NAME).Result;
                        }
                        catch { }

                        if (rule is null)
                        {
                            _ = _managementClient.CreateRuleAsync(topicName, subscriptionName, filterRuleDescription).Result;
                        }
                        else if (!rule.Filter.Equals(filterRuleDescription.Filter))
                        {
                            _ = _managementClient.UpdateRuleAsync(topicName, subscriptionName, filterRuleDescription).Result;
                        }
                    }

                    ReceiveMode receiveMode = ReceiveMode.PeekLock;
                    if (topic.Value.DeleteAfterRead)
                    {
                        receiveMode = ReceiveMode.ReceiveAndDelete;
                    }
                    int maxConcurrentCalls = topic.Value.MaxConcurrentCalls;

                    var subscriptionClient = new SubscriptionClient(GetConnection(topic), topicName, subscriptionName, receiveMode, ServiceBus.RetryPolicy);

                    //Register the method to process receive message
                    //The RegisterMessageHandler is validate for all register exist on the queue, without need loop for items
                    subscriptionClient.RegisterMessageHandler(
                        async (message, cancellationToken) =>
                        {
                            try
                            {
                                InvokeProcess(method, message.Body);
                                await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                            }
                            catch (Exception exRegister)
                            {
                                Exception moreInfo = new LightException($"Exception reading message from topic {topicName} and subscription {subscriptionName}. See inner exception for details. Message={exRegister.Message}", exRegister);
                                //Use the class instead of interface because tracking exceptions directly is not supposed to be done outside AMAW (i.e. by the business code)
                                ((LightTelemetry)WorkBench.Telemetry).TrackException(moreInfo);

                                var reason = $"{exRegister.Message}";

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
                                        else if(exRegister.InnerException is InvalidInputLightException)
                                        {
                                            var inputErrors = (exRegister.InnerException as InvalidInputLightException).InputErrors;

                                            string jsonString = (new { critics = inputErrors }).ToJsonString();
                                            reason = $"{reason} \n {jsonString}";

                                            await SendToDeadLetter(subscriptionClient, message, reason, errorDescription);
                                        }
                                        else if (exRegister.InnerException is BusinessValidationLightException)
                                        {
                                            var inputErrors = (exRegister.InnerException as BusinessValidationLightException).InputErrors;

                                            string jsonString = (new { critics = inputErrors }).ToJsonString();
                                            reason = $"{reason} \n {jsonString}";

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
            }
            catch (Exception exception)
            {
                Exception moreInfo = new LightException($"Error setting up subscription consumption from service bus. See inner exception for details. Message={exception.Message}", exception);
                //Use the class instead of interface because tracking exceptions directly is not supposed to be done outside AMAW (i.e. by the business code)
                ((LightTelemetry)WorkBench.Telemetry).TrackException(moreInfo);
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// Method to run Health Check for Service Bus
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LightHealth.HealthCheckStatus HealthCheck(string serviceKey, string value)
        {
            try
            {
                if (Topics.Count > 0)
                {
                    foreach (KeyValuePair<MethodInfo, TopicAttribute> item in Topics)
                    {
                        if (!string.IsNullOrEmpty(item.Value.TopicName.ToString()))
                        {
                            TopicClient topicClient = new(GetConnection(item), item.Value.TopicName, ServiceBus.RetryPolicy);

                            var scheduledMessageId = topicClient.ScheduleMessageAsync(
                                    new Message(Encoding.UTF8.GetBytes("HealthCheckTestMessage")),
                                    new DateTimeOffset(WorkBench.UtcNow).AddHours(2));

                            topicClient.CancelScheduledMessageAsync(scheduledMessageId.Result);
                        }
                    }
                }

                if (Queues.Count > 0)
                {
                    foreach (KeyValuePair<MethodInfo, QueueAttribute> item in Queues)
                    {

                        if (!string.IsNullOrEmpty(item.Value.QueueName.ToString()))
                        {
                            QueueClient queueReceiver = new(GetConnection(item), item.Value.QueueName, ReceiveMode.ReceiveAndDelete, ServiceBus.RetryPolicy);
                            var scheduledMessageId = queueReceiver.ScheduleMessageAsync(
                                    new Message(Encoding.UTF8.GetBytes("HealthCheckTestMessage")),
                                    new DateTimeOffset(WorkBench.UtcNow).AddHours(2));

                            queueReceiver.CancelScheduledMessageAsync(scheduledMessageId.Result);
                        }
                    }
                }
                return LightHealth.HealthCheckStatus.Healthy;
            }
            catch
            {
                return LightHealth.HealthCheckStatus.Unhealthy;
            }
        }

        private static async Task SendToDeadLetter(QueueClient queueClient, Message message, string reason, string errorDescription)
        {
            WorkBench.Telemetry.TrackException(new DeadLetterLightException(reason, errorDescription));

            await queueClient.DeadLetterAsync(message.SystemProperties.LockToken, reason, errorDescription);
        }

        private static async Task SendToDeadLetter(SubscriptionClient subscriptionClient, Message message, string reason, string errorDescription)
        {
            WorkBench.Telemetry.TrackException(new DeadLetterLightException(reason, errorDescription));

            await subscriptionClient.DeadLetterAsync(message.SystemProperties.LockToken, reason, errorDescription);
        }
    }
}
