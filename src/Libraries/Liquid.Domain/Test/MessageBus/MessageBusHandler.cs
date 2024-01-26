using Liquid.Base;
using Liquid.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Liquid.Domain.Test
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    internal class MessageBusHandler
    {
        static readonly List<MethodInfo[]> _methodsSigned = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                                             where !assembly.IsDynamic
                                                             from type in assembly.ExportedTypes
                                                             where type.BaseType is not null
                                                                && type.GetInterfaces().Contains(typeof(ILightWorker))
                                                             select type.GetMethods()).ToList();

        static object GetPropertyByName(object obj, string propertyName) => obj.GetType().GetProperty(propertyName)?.GetValue(obj);

        internal static bool HandleHttpInvoke(ref HttpContext context, string pathToCheck)
        {
            if (pathToCheck.StartsWith("/messageBus"))
            {
                if (!WorkBench.IsDevelopmentEnvironment && !WorkBench.IsIntegrationEnvironment)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return false;
                }

                string busPath = pathToCheck.Replace("/messageBus", "").TrimStart('/');

                if (!HandleSend(ref context, pathToCheck: busPath))
                    return false;

                if (!HandleIntercept(ref context, pathToCheck: busPath))
                    return false;
                else
                    return true;
            }
            else
                return true;
        }

        private static bool HandleIntercept(ref HttpContext context, string pathToCheck)
        {
            string message;

            if (pathToCheck.StartsWith("intercept"))
            {
                string mode;
                if (pathToCheck == "intercept")
                {
                    mode = MessageBusInterceptor.InterceptMessages ? "enabled" : "disabled";
                    message = $"Intercept service bus mode is currently set to {mode}. Use `/messageBus/intercept/<enable|disable>` to change it, " +
                              $"`/messageBus/intercept/messages/<id>/<messageType>` to retrieve intercepted messages (`id` and `messageType` fields are optional - either none or both), " +
                              $"or `/messageBus/intercept/messages/<id>/clear` to clear intercepted messages (`id` field is optional).";
                    context.Response.WriteAsync(new { message, mode }.ToJsonString());
                    return false;
                }

                var invalidPath = false;
                // formats: 
                // `intercept/<enable|disable>`
                // `intercept/messages` -> get all || `intercept/message/<id>/<messageType>` -> get by id and message type
                // `intercept/messages/clear` -> clear all || `intercept/messages/<id>/clear` -> clear by id
                var splitPath = pathToCheck.Split('/');

                if (splitPath.Length >= 2 && splitPath[1] == "messages")
                {
                    if (!MessageBusInterceptor.InterceptMessages)
                    {
                        message = "Message interceptor is currently disabled. Use `/messageBus/intercept/enable` to enable it.";
                        context.Response.WriteAsync(new { message, mode = "disabled" }.ToJsonString());
                        return false;
                    }
                    if (splitPath.Length == 2)
                    {
                        // get all
                        var interceptions = MessageBusInterceptor.InterceptedMessages;
                        var domainResponse = new DomainResponse(interceptions.ToJsonDocument());

                        context.Response.WriteAsync(domainResponse.ToJsonString());
                        return false;
                    }
                    if (splitPath.Length == 3)
                    {
                        if (splitPath[2] == "clear")
                        {
                            // clear all
                            MessageBusInterceptor.ClearMessages();
                            message = $"Cleared all intercepted messages.";
                            context.Response.WriteAsync(new { message }.ToJsonString());
                            return false;
                        }

                        // get by operation id 
                        var operationId = splitPath[2];

                        var interceptions = MessageBusInterceptor.InterceptedMessagesByOperationId(operationId);
                        var domainResponse = new DomainResponse(interceptions.ToJsonDocument(), operationId);

                        context.Response.WriteAsync(domainResponse.ToJsonString());
                        return false;
                    }
                    if (splitPath.Length == 4)
                    {
                        if (splitPath[3] == "clear")
                        {
                            // clear by operation id
                            var id = splitPath[2];
                            MessageBusInterceptor.ClearMessages(id);
                            message = $"Cleared messages for id {id}.";
                            context.Response.WriteAsync(new { message }.ToJsonString());
                            return false;
                        }
                        // get by operation id and message type
                        var operationId = splitPath[2];
                        var messageType = splitPath[3];

                        var interceptions = MessageBusInterceptor.InterceptedMessagesByOperationIdAndMessageType(operationId, messageType);
                        var domainResponse = new DomainResponse(interceptions.Select(m => m.Message).ToList().ToJsonDocument(), operationId);

                        context.Response.WriteAsync(domainResponse.ToJsonString());
                        return false;
                    }
                    invalidPath = true;
                }

                if (splitPath.Length != 2)
                    invalidPath = true;

                bool? intercept = null;
                if (!invalidPath)
                {
                    switch (splitPath[1])
                    {
                        case "enable":
                            intercept = true;
                            break;
                        case "disable":
                            intercept = false;
                            break;
                        default:
                            invalidPath = true;
                            break;
                    }
                }

                if (invalidPath)
                {
                    message = "Failed to use message bus interception. Get correct usage by calling `/messageBus/intercept`.";
                    context.Response.WriteAsync(new { message }.ToJsonString());
                    return false;
                }

                MessageBusInterceptor.InterceptMessages = intercept.Value;
                mode = intercept.Value ? "enabled" : "disabled";
                message = $"Setting intercept service bus mode to {mode}.";

                context.Response.WriteAsync(new { message }.ToJsonString());
                return false;
            }
            else
                return true;
        }

        private static bool HandleSend(ref HttpContext context, string pathToCheck)
        {
            string message;

            if (pathToCheck.StartsWith("send"))
            {
                // force intercept when using send command, then return to previous state
                var splitPath = pathToCheck.Split('/');

                if (splitPath.Length < 3)
                {
                    message = "Missing attributes for sending message. Correct usage: `/messageBus/send/<queue|topic>/<endpointName>`";
                    context.Response.WriteAsync(new { message }.ToJsonString());
                    return false;
                }

                var interceptMode = MessageBusInterceptor.InterceptMessages;
                MessageBusInterceptor.InterceptMessages = true;

                var endpointType = splitPath[1];

                // entity name may contain slashes
                var endpointName = string.Join('/', splitPath.Skip(2));
                endpointName = MessageBrokerHelper.BuildNonProductionEnvironmentEndpointName(endpointName);

                if (endpointType != "queue" && endpointType != "topic")
                {
                    message = "Invalid entity type for sending message. Use either `queue` or `topic`: `/messageBus/send/<queue|topic>/<endpointName>`";
                    context.Response.WriteAsync(new { message }.ToJsonString());
                    return false;
                }

                endpointType = endpointType == "topic" ? "TopicAttribute" : "QueueAttribute";

                using var reader = new StreamReader(context.Request.Body);
                string messageBody = reader.ReadToEndAsync().Result;

                if (string.IsNullOrEmpty(messageBody))
                {
                    message = "Missing message body.";
                    context.Response.WriteAsync(new { message }.ToJsonString());
                    return false;
                }

                MethodInfo toTestMethod = null;
                DomainResponse domainResponse = null;

                if (endpointName.EndsWith(SchedulerMessageBus<MessageBrokerWrapper>.JOBS_ENDPOINT))
                {
                    JobDispatchMSG jobMsg;
                    try
                    {
                        jobMsg = messageBody.ToJsonDocument().ToObject<JobDispatchMSG>();
                    }
                    catch
                    {
                        message = "Invalid message body. Check the JSON formatting.";
                        context.Response.WriteAsync(messageBody);
                        return false;
                    }

                    // search for the job method
                    foreach (var methods in _methodsSigned)
                    {
                        toTestMethod = methods.FirstOrDefault(m => m.Name == jobMsg.Job);
                        if (toTestMethod is not null)
                            break;
                    }

                    if (toTestMethod is null)
                        throw new LightException($"The method {jobMsg.Job} was not found in job of the microservice.");

                    var lightJob = (ILightWorker)Activator.CreateInstance(toTestMethod.ReflectedType, null);

                    lightJob.Context = jobMsg.TransactionContext;
                    lightJob.Telemetry = (ILightTelemetry)WorkBench.Telemetry.CloneService();
                    lightJob.Telemetry.OperationId = jobMsg.OperationId;
                    object[] parametersArray = new object[] { jobMsg.Activation, jobMsg.Partition };

                    var task = toTestMethod.Invoke(lightJob, parametersArray);

                    if (task?.GetType().IsSubclassOf(typeof(Task)) == true)
                    {
                        (task as Task).Wait();
                    }

                    domainResponse = new(null, lightJob.Context, lightJob.CriticHandler);
                }
                else
                {
                    // search for the worker method 
                    foreach (var methods in _methodsSigned)
                    {
                        foreach (var method in methods)
                        {
                            foreach (var endpoint in method.GetCustomAttributes(typeof(Attribute), false))
                            {
                                var typeId = GetPropertyByName(endpoint, "TypeId");
                                var name = GetPropertyByName(typeId, "Name");

                                if (endpointType == (string)name)
                                {
                                    if (endpointName == (string)GetPropertyByName(endpoint, "TopicName")
                                        || endpointName == (string)GetPropertyByName(endpoint, "QueueName"))
                                    {
                                        toTestMethod = method;
                                        goto exitSearchWorkerMethod;
                                    }
                                }
                            }
                        }
                    }

                exitSearchWorkerMethod:
                    if (toTestMethod is null)
                        throw new LightException($"The {endpointType.Replace("Attribute", "").ToLower()} named '{endpointName}' was not found in attributes of any worker of the microservice.");

                    var messageType = toTestMethod.GetParameters()[0].ParameterType;
                    
                    ILightMessage workerMsg = null;
                    try
                    {
                        workerMsg = (ILightMessage)JsonSerializer.Deserialize(messageBody, messageType, LightGeneralSerialization.IgnoreCase);
                    }
                    catch
                    {
                        message = "Invalid message body. Check the JSON formatting.";
                        context.Response.WriteAsync(new { message }.ToJsonString());
                        return false;
                    }

                    var lightWorker = (ILightWorker)Activator.CreateInstance(toTestMethod.ReflectedType, null);

                    lightWorker.Context = workerMsg.TransactionContext;
                    lightWorker.Telemetry = (ILightTelemetry)WorkBench.Telemetry.CloneService();
                    lightWorker.Telemetry.OperationId = workerMsg.OperationId;

                    var task = toTestMethod.Invoke(lightWorker, new object[] { workerMsg });

                    if (task?.GetType().IsSubclassOf(typeof(Task)) == true)
                    {
                        (task as Task).Wait();
                    }

                    domainResponse = new(null, lightWorker.Context, lightWorker.CriticHandler);
                }

                var messages = MessageBusInterceptor.InterceptedMessagesByOperationId(domainResponse.OperationId);

                WriteToResult(context, domainResponse);

                // force intercept when using send command, then return to previous state
                MessageBusInterceptor.InterceptMessages = interceptMode;
                return false;
            }
            else
                return true;
        }

        private static void WriteToResult(HttpContext context, DomainResponse response)
        {
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.WriteAsync(response.ToJsonString());
        }
    }

    /// <summary>
    /// Class available in Liquid.Domain, but circular dependency :(
    /// </summary>
    public static class MessageBrokerHelper
    {
        public static string BuildNonProductionEnvironmentEndpointName(string endpointName)
        {
            if (WorkBench.IsDevelopmentEnvironment)
            {
                var mn = Environment.MachineName;
                if (mn.Length > 7)
                    // assuming MachineName of the format `DESKTOP-XXXXXXX`
                    mn = mn[^7..];

                endpointName = $"{mn}-{endpointName}";
            }
            else if (WorkBench.IsIntegrationEnvironment)
                endpointName = $"int-{endpointName}";
            else if (WorkBench.IsQualityEnvironment)
                endpointName = $"qa-{endpointName}";
            else if (WorkBench.IsDemonstrationEnvironment)
                endpointName = $"demo-{endpointName}";
            return endpointName;
        }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
