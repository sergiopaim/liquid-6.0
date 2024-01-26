using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using Liquid.Interfaces;
using Liquid.Runtime;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Implementation of the communication component between queues and topics, 
    /// to carry out the good practice of communication
    /// between micro services. In order to use this feature it is necessary 
    /// to implement the inheritance of this class.
    /// </summary>
    public abstract class LightWorker : ILightWorker
    {
        protected readonly static Dictionary<MethodInfo, QueueAttribute> Queues = new();
        protected readonly static Dictionary<MethodInfo, TopicAttribute> Topics = new();
        private readonly Dictionary<string, object[]> _inputValidationErrors = new();
        public ILightTelemetry Telemetry { get; set; } = WorkBench.Telemetry?.CloneService() as ILightTelemetry;
        protected static ILightCache Cache => WorkBench.Cache;
        public ILightContext Context { get; set; }

        /// <summary>
        /// Gets the id of the current user
        /// </summary>
        protected string CurrentUserId => Context.User?.FindFirstValue("sub") ?? Context.User?.FindFirstValue(JwtClaimTypes.UserId);

        /// <summary>
        /// Gets the first name of the current user
        /// </summary>
        protected string CurrentUserFirstName => Context.User?.FindFirstValue("GivenName") ?? "";

        /// <summary>
        /// Gets the full name of the current user
        /// </summary>
        protected string CurrentUserFullName => CurrentUserFirstName + " " + Context.User?.FindFirstValue("Surname") ?? "";

        /// <summary>
        /// Gets the e-mail address of the current user
        /// </summary>
        protected string CurrentUserEmail => Context.User?.FindFirstValue("Email") ?? "";

        /// <summary>
        /// Checks if the current user is in the given security role
        /// </summary>
        /// <param name="role">Security role</param>
        /// <returns>True if the user is in the role</returns>
        protected bool CurrentUserIsInRole(string role) => Context.User?.IsInRole(role) ?? false;

        /// <summary>
        /// Checks if the current user is in any of the given security roles
        /// </summary>
        /// <param name="roles">Security roles in a comma separated string</param>
        /// <returns>True if the user is in any role</returns>
        protected bool CurrentUserIsInAnyRole(string roles)
        {
            if (Context.User is null)
                return false;

            return roles.Split(",")
                        .Any(r => Context.User.IsInRole(r.Trim()));
        }

        /// <summary>
        /// Checks if the current user is in any of the given security roles
        /// </summary>
        /// <param name="roles">List of security roles</param>
        /// <returns>True if the user is in any role</returns>
        protected bool CurrentUserIsInAnyRole(params string[] roles)
        {
            if (Context.User is null)
                return false;

            return roles.Any(r => Context.User.IsInRole(r.Trim()));
        }

        /// <summary>
        /// Instance of CriticHandler to inject on the others classes
        /// </summary>
        public ICriticHandler CriticHandler { get; } = new CriticHandler();

        /// <summary>
        /// Discovery the key connection defined on the implementation of the LightWorker
        /// </summary>
        /// <param name="method">Method related the queue or topic</param>
        /// <returns>String key connection defined on the implementation of the LightWorker</returns>
        protected static string GetKeyConnection(MethodInfo method)
        {
            var attributes = method?.ReflectedType.CustomAttributes;
            string connectionKey = "";
            if (attributes.Any())
                connectionKey = attributes.ToArray()[0].ConstructorArguments[0].Value.ToString();
            return connectionKey;
        }

        /// <summary>
        /// Check if it was declared attribute of the Key Connection on the implementation of the LightWorker
        /// </summary>
        /// <param name="method">Method related the queue or topic</param>
        /// <returns>Will true, if there is it</returns>
        private static bool IsDeclaredConnection(MethodInfo method)
        {
            return string.IsNullOrEmpty(GetKeyConnection(method));
        }

        /// <summary>
        /// Get the method related the queue or topic
        /// </summary>
        /// <typeparam name="T">Type of the queue or topic</typeparam>
        /// <param name="item">Item related dictionary of queue or topic</param>
        /// <returns>Method related the queue or topic</returns>
        protected virtual MethodInfo GetMethod<T>(KeyValuePair<MethodInfo, T> item)
        {
            return item.Key;
        }

        /// <summary>
        /// Implementation of the start process to discovery by reflection the Worker
        /// </summary>
        public virtual void Initialize()
        {
            Discovery();
        }

        /// <summary>
        /// Method for discovery all methods that use a LightQueue or LightTopic.
        /// </summary>
        private static void Discovery()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<MethodInfo[]> _methodsSigned = (from assembly in assemblies
                                                 where !assembly.IsDynamic
                                                 from type in assembly.ExportedTypes
                                                 where type.BaseType is not null && type.BaseType == typeof(LightWorker)
                                                 select type.GetMethods()).ToList();

            foreach (var methods in _methodsSigned)
            {
                foreach (var method in methods)
                {
                    foreach (TopicAttribute topic in (TopicAttribute[])method.GetCustomAttributes(typeof(TopicAttribute), false))
                    {
                        if (!IsDeclaredConnection(method))
                        {
                            if (Topics.Values.FirstOrDefault(x => x.TopicName == topic.TopicName && x.Subscription == topic.Subscription) is null)
                            {
                                Topics.Add(method, topic);
                            }
                            else
                            {
                                throw new LightException($"Duplicated worker: there's already a worker for the same topic (\"{topic.TopicName}\") and subscription(\"{topic.Subscription}\")");
                            }
                        }
                        else
                        {
                            // if there isn't Custom Attribute with string connection, will be throw exception.
                            throw new LightException($"No Attribute MessageBus with a configuration string has been informed on the worker \"{method.DeclaringType}\".");
                        }
                    }
                    foreach (QueueAttribute queue in (QueueAttribute[])method.GetCustomAttributes(typeof(QueueAttribute), false))
                    {
                        if (!IsDeclaredConnection(method))
                        {
                            if (Queues.Values.FirstOrDefault(x => x.QueueName == queue.QueueName) is null)
                                Queues.Add(method, queue);
                            else
                                throw new LightException($"There is already Queue defined with the name \"{queue.QueueName}\".");
                        }
                        else
                        {
                            //If there isn't Custom Attribute with string connection, will be throw exception.
                            throw new LightException($"No Attribute MessageBus with a configuration string has been informed on the worker \"{method.DeclaringType}\".");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method created to process by reflection the Workers declared
        /// </summary>
        /// <returns>object</returns>
        public static object InvokeProcess(MethodInfo method, byte[] message)
        {
            object result = null;
            if (method is not null)
            {
                ParameterInfo[] parameters = method.GetParameters();
                object lightWorker = Activator.CreateInstance(method.ReflectedType, null);

                if (parameters.Length == 0)
                {
                    result = method.Invoke(lightWorker, null);
                }
                else
                {
                    dynamic lightMessage = JsonSerializer.Deserialize(Encoding.UTF8.GetString(message), parameters[0].ParameterType, LightGeneralSerialization.IgnoreCase);
                    //Check if it needs authorization, unless that there isn't AuthorizeAttribute
                    foreach (AuthorizeAttribute authorize in (AuthorizeAttribute[])method.GetCustomAttributes(typeof(AuthorizeAttribute), false))
                    {
                        if ((lightMessage.Context is null) || ((lightMessage.Context is not null) && (lightMessage.Context.User is null)))
                        {
                            //If there isn't Context, will be throw exception.
                            throw new LightException("No TokenJwt has been informed on the message sent to the worker.");
                        }
                        if ((authorize.Policy is not null) && (lightMessage.Context.User.FindFirst(authorize.Policy) is null))
                        {
                            throw new LightException($"No Policy \"{authorize.Policy}\" has been informed on the message sent to the worker.");
                        }
                        if ((authorize.Roles is not null) && (!lightMessage.Context.User.IsInRole(authorize.Roles)))
                        {
                            throw new LightException($"No Roles \"{authorize.Roles}\" has been informed on the message sent to the worker.");
                        }
                    }

                    ((LightWorker)lightWorker).Context = ((ILightMessage)lightMessage).TransactionContext;
                    ((LightWorker)lightWorker).Telemetry = (ILightTelemetry)WorkBench.Telemetry.CloneService();
                    ((LightWorker)lightWorker).Telemetry.OperationId = ((ILightMessage)lightMessage).OperationId;
                    object[] parametersArray = new object[] { lightMessage };
                    result = method.Invoke(lightWorker, parametersArray);
                }

                if (result is not null)
                {
                    var resultTask = (result as System.Threading.Tasks.Task);
                    resultTask.Wait();
                    if (resultTask.IsFaulted)
                    {
                        resultTask.Exception?.FilterRelevantStackTrace();
                        throw resultTask.Exception;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Method for create a instance of LightDomain objects
        /// </summary>
        /// <typeparam name="T">Type of LightDomain</typeparam>
        /// <returns></returns>
        protected T Factory<T>() where T : LightDomain, new()
        {
            //Verify if there's erros
            if (_inputValidationErrors.Count > 0)
            {
                // Throws the error code from errors list of input validation to View Model
                throw new InvalidInputLightException(_inputValidationErrors);
            }
            var domain = LightDomain.FactoryDomain<T>();
            domain.Cache = Cache;
            domain.Context = Context ?? new LightContext
            {
                OperationId = WorkBench.GenerateNewOperationId()
            };
            domain.Telemetry = Telemetry;
            domain.Telemetry.OperationId = domain.Context.OperationId;
            domain.CritictHandler = CriticHandler;
            return domain;
        }

        /// <summary>
        /// The method receives the error code to add on errors list of input validation.
        /// </summary>
        /// <param name="message">The error message</param>
        protected void AddInputError(string message)
        {
            if (!_inputValidationErrors.ContainsKey(message))
                _inputValidationErrors.TryAdd(message, null);
        }

        private void AddInputValidationErrorCode(string error)
        {
            if (!_inputValidationErrors.ContainsKey(error))
                _inputValidationErrors.TryAdd(error, null);
        }

        private void AddInputValidationErrorCode(string error, params object[] args)
        {
            if (!_inputValidationErrors.ContainsKey(error))
                _inputValidationErrors.TryAdd(error, args);
        }

        /// <summary>
        /// The method receive the ViewModel to input validation and add on errors list
        /// (if there are errors after validation ViewModel.)
        /// </summary>
        /// <param name="viewModel">The ViewModel to input validation</param>
        protected void ValidateInput(dynamic viewModel)
        {
            if (viewModel is null)
                return;

            viewModel.InputErrors = _inputValidationErrors;
            viewModel.Validate();
            ResultValidation result = viewModel.Validator.Validate(viewModel);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    //receive the error code to add on errors list of input validation.
                    AddInputValidationErrorCode(error.Key, error.Value);
                }
            }

            //By reflection, browse viewModel by identifying all property attributes and lists for validation.  
            foreach (PropertyInfo propInfo in viewModel.GetType().GetProperties())
            {
                dynamic child = propInfo.GetValue(viewModel);

                //When the child is a list, validate each of its members  
                if (child is IList)
                {
                    var children = (IList)propInfo.GetValue(viewModel);
                    foreach (var item in children)
                    {
                        //Check, if the property is a Light ViewModel, only they will validation Lights ViewModel
                        if (item is not null
                             && (item.GetType().BaseType != typeof(object))
                             && (item.GetType().BaseType != typeof(System.ValueType))
                             && (item.GetType().BaseType.IsGenericType
                                  && (item.GetType().BaseType.Name.StartsWith("LightViewModel")
                                       || item.GetType().BaseType.Name.StartsWith("LightValueObject"))))
                        {
                            dynamic obj = item;
                            //Check, if the attribute is null for verification of the type.
                            if (obj is not null)
                                ValidateInput(obj);
                        }
                    }
                }
                else
                {
                    //Otherwise, validate the very child once. 
                    if (child is not null)
                    {

                        //Check, if the property is a Light ViewModel, only they will validation Lights ViewModel
                        if ((child.GetType().BaseType != typeof(object))
                             && (child.GetType().BaseType != typeof(System.ValueType))
                             && (child.GetType().BaseType.IsGenericType
                                  && (child.GetType().BaseType.Name.StartsWith("LightViewModel")
                                       || child.GetType().BaseType.Name.StartsWith("LightValueObject"))))
                        {

                            ValidateInput(child);
                        }
                    }
                }
            }

            //By reflection, browse viewModel by identifying all field attributes and lists for validation.  
            foreach (FieldInfo fieldInfo in viewModel.GetType().GetFields())
            {
                dynamic child = fieldInfo.GetValue(viewModel);

                //When the child is a list, validate each of its members  
                if (child is IList)
                {
                    var children = (IList)fieldInfo.GetValue(viewModel);
                    foreach (var item in children)
                    {
                        //Check, if the property is a Light ViewModel, only they will validation Lights ViewModel
                        if (item is not null
                             && (item.GetType().BaseType != typeof(object))
                             && (item.GetType().BaseType != typeof(System.ValueType))
                             && (item.GetType().BaseType.IsGenericType
                                  && (item.GetType().BaseType.Name.StartsWith("LightViewModel")
                                       || item.GetType().BaseType.Name.StartsWith("LightValueObject"))))
                        {
                            dynamic obj = item;
                            //Check, if the attribute is null for verification of the type.
                            if (obj is not null)
                                ValidateInput(obj);
                        }
                    }
                }
                else
                {
                    //Otherwise, validate the very child once. 
                    if (child is not null)
                    {

                        //Check, if the property is a Light ViewModel, only they will validation Lights ViewModel
                        if ((child.GetType().BaseType != typeof(object))
                             && (child.GetType().BaseType != typeof(System.ValueType))
                             && (child.GetType().BaseType.IsGenericType
                                  && (child.GetType().BaseType.Name.StartsWith("LightViewModel")
                                       || child.GetType().BaseType.Name.StartsWith("LightValueObject"))))
                        {

                            ValidateInput(child);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method for send response.
        /// </summary>
        protected void Terminate()
        {
            //Verify if there's errors
            if (!MessageBusInterceptor.ShouldInterceptMessages && CriticHandler.HasCriticalErrors())
            {
                // Throws the error code from errors list of input validation to View Model
                throw new BusinessValidationLightException(CriticHandler.GetCriticalErrors());
            }
            else if (!MessageBusInterceptor.ShouldInterceptMessages && CriticHandler.HasBusinessWarnings)
            {
                Telemetry.TrackException(new LightBusinessWarningException(CriticHandler.Critics.Where(c => c.Type == CriticType.Warning).ToJsonString()));
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}