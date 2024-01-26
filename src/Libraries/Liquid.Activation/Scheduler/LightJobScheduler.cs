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

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Implementation of the CRON-like scheduler inside a Microservice based on a centralized CRON dispatcher via MessageBus messages
    /// </summary>
    public abstract class LightJobScheduler : ILightWorker
    {
        protected readonly static Dictionary<MethodInfo, JobAttribute> Jobs = new();
        private readonly Dictionary<string, object[]> InputValidationErrors = new();

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
        ///Instance of CriticHandler to inject on the others classes
        /// </summary>
        public ICriticHandler CriticHandler { get; } = new CriticHandler();

        /// <summary>
        /// Discovery the key connection defined on the implementation of the LightJobWorker
        /// </summary>
        /// <param name="method">Method related the queue or topic</param>
        /// <returns>String key connection defined on the implementation of the LightJobWorker</returns>
        protected static string GetConnectionKey(MethodInfo method)
        {
            var attributes = method?.ReflectedType.CustomAttributes;
            string connectionKey = "";
            if (attributes.Any())
                connectionKey = attributes.ToArray()[0].ConstructorArguments[0].Value.ToString();
            return connectionKey;
        }

        /// <summary>
        /// Discovery of the subscription name as defined on the declaration of the LightJobScheduler
        /// </summary>
        /// <param name="method">Method related the scheduler</param>
        /// <returns>The subscription name</returns>
        protected static string GetSubscriptionName(MethodInfo method)
        {
            var attributes = method?.ReflectedType.CustomAttributes;
            string subscriptionName = "";
            if (attributes.Any())
                subscriptionName = attributes.ToArray()[0].ConstructorArguments[1].Value.ToString();
            return subscriptionName;
        }

        protected static int GetMaxConcurrentCalls(MethodInfo method)
        {
            var attributes = method?.ReflectedType.CustomAttributes;
            string maxConcurrentCalls = "";
            if (attributes.Any())
                maxConcurrentCalls = attributes.ToArray()[0].ConstructorArguments[2].Value.ToString();
            return Convert.ToInt32(maxConcurrentCalls);
        }

        /// <summary>
        /// Check if attribute was declared with Key Connection for the LightJobWorker
        /// </summary>
        /// <param name="method">Method related the queue or topic</param>
        /// <returns>Will true, if there is it</returns>
        private static bool IsDeclaredConnection(MethodInfo method)
        {
            return string.IsNullOrEmpty(GetConnectionKey(method));
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
            Console.WriteLine("Starting Job Scheduler");
            Discovery();
        }

        /// <summary>
        /// Implementation of the start process to discovery by reflection the Worker
        /// </summary>
        protected static void Initialized()
        {
            Console.WriteLine("Job Scheduler started");
        }

        /// <summary>
        /// Method for discovery all methods that use a Job attribute.
        /// </summary>
        private static void Discovery()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<MethodInfo[]> _methodsSigned = (from assembly in assemblies
                                                 where !assembly.IsDynamic
                                                 from type in assembly.ExportedTypes
                                                 where type.BaseType is not null && type.BaseType == typeof(LightJobScheduler)
                                                 select type.GetMethods()).ToList();

            foreach (var methods in _methodsSigned)
            {
                foreach (var method in methods)
                {
                    foreach (JobAttribute job in method.GetCustomAttributes(typeof(JobAttribute), false))
                    {
                        if (!IsDeclaredConnection(method))
                        {
                            Jobs.Add(method, job);
                            RegisteredJobs.RegisterJob(method.Name, LightJobStatus.Running.Code);
                        }
                        else
                        {
                            // if there isn't Custom Attribute with string connection, will be throw exception.
                            throw new LightException($"No Attribute MessageBus with a configuration string has been informed on the worker \"{method.DeclaringType}\".");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method created to process by reflection the Jobs declared
        /// </summary>
        /// <returns>True if job is found</returns>
        public static bool TryInvokeProcess(JobDispatchMSG message)
        {
            // find registered job with method name that matches job name sent or ignores the invocation
            var job = Jobs.FirstOrDefault(j => j.Key.Name == message.Job);
            if (job.Equals(default(KeyValuePair<MethodInfo, JobAttribute>)))
                return false; //meaning notfound

            MethodInfo method = job.Key;
            if (message.CommandType == JobDispatchCMD.Abort.Code)
                RegisteredJobs.UpdateJobStatus(message.Job, LightJobStatus.Aborted.Code);
            else
            {
                //Check if it needs authorization, unless there isn't AuthorizeAttribute
                foreach (AuthorizeAttribute authorize in method.GetCustomAttributes(typeof(AuthorizeAttribute), false))
                {
                    if (message.TransactionContext?.User is null)
                    {
                        //If there isn't Context, should throw exception.
                        throw new LightException("No TokenJwt has been informed on the message sent to the worker.");
                    }
                    if ((authorize.Policy is not null) && (message.TransactionContext.User.FindFirst(authorize.Policy) is null))
                    {
                        throw new LightException($"No Policy \"{authorize.Policy}\" has been informed on the message sent to the worker.");
                    }
                    if ((authorize.Roles is not null) && (!message.TransactionContext.User.IsInRole(authorize.Roles)))
                    {
                        throw new LightException($"No Roles \"{authorize.Roles}\" has been informed on the message sent to the worker.");
                    }
                }

                try
                {
                    object lightJobWorker = Activator.CreateInstance(method.ReflectedType, null);
                    ((LightJobScheduler)lightJobWorker).Context = ((ILightMessage)message).TransactionContext;
                    ((LightJobScheduler)lightJobWorker).Telemetry = (ILightTelemetry)WorkBench.Telemetry.CloneService();
                    ((LightJobScheduler)lightJobWorker).Telemetry.OperationId = message.OperationId;
                    object[] parametersArray = new object[] { message.Activation, message.Partition };
                    method.Invoke(lightJobWorker, parametersArray);

                }
                catch (Exception e)
                {
                    var lightEx = new LightException($"Error trying to invoke '{method.ReflectedType}' of microservice '{message.Microservice}' with parms {{ Activation: {message.Activation}, Partition: {message.Partition} }}", e);
                    WorkBench.Telemetry.TrackException(lightEx);
                    throw lightEx;
                }
            }

            return true;
        }

        /// <summary>
        /// Method for create a instance of LightDomain objects
        /// </summary>
        /// <typeparam name="T">Type of LightDomain</typeparam>
        /// <returns></returns>
        protected T Factory<T>() where T : LightDomain, new()
        {
            // Verify if there's erros
            if (InputValidationErrors.Count > 0)
            {
                // Throws the error code from errors list of input validation to View Model
                throw new InvalidInputLightException(InputValidationErrors);
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
            if (!InputValidationErrors.ContainsKey(message))
                InputValidationErrors.TryAdd(message, null);
        }

        private void AddInputValidationErrorCode(string error)
        {
            if (!InputValidationErrors.ContainsKey(error))
                InputValidationErrors.TryAdd(error, null);
        }

        private void AddInputValidationErrorCode(string error, params object[] args)
        {
            if (!InputValidationErrors.ContainsKey(error))
                InputValidationErrors.TryAdd(error, args);
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

            viewModel.InputErrors = InputValidationErrors;
            viewModel.Validate();
            ResultValidation result = viewModel.Validator.Validate(viewModel);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    // Receives the error code to add on errors list of input validation.
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