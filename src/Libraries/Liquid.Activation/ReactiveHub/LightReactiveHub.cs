using Liquid.Base;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class LightReactiveHub : Hub, ILightReactiveHub
    {
        private readonly Dictionary<string, object[]> _inputValidationErrors = new();
        //Cloning TLightelemetry service singleton because it services multiple LightDomain instances from multiple threads with instance variables
        protected ILightTelemetry Telemetry { get; } = WorkBench.Telemetry?.CloneService() as ILightTelemetry;
        protected static ILightLogger Logger => WorkBench.Logger;
        protected static ILightCache Cache => WorkBench.Cache;

        protected LightContext LightContext { get; set; }

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

        protected HttpContext HttpContext => Context.GetHttpContext();

        private static LightHubConnection connection;
        protected static LightHubConnection Connection { get => connection; set => connection = value; }

        /// <summary>
        ///Instance of CriticHandler to inject on the others classes
        /// </summary>
        private readonly CriticHandler _criticHandler = new();

        private LightContext GetContext()
        {
            string operationId = null;

            if (HttpContext.Request.Headers.TryGetValue("traceparent", out StringValues headerValue))
            {
                string traceContext = headerValue;
                var splits = traceContext.Split("-");
                if (splits.Length > 1)
                    operationId = splits[1];
            }
            else if (HttpContext.Request.Headers.TryGetValue("Operation-Id", out headerValue))
                operationId = headerValue;

            operationId ??= WorkBench.GenerateNewOperationId();

            return new()
            {
                User = Context.User,
                OperationId = operationId
            };
        }

        public string GetHubEndpoint()
        {
            var reactiveHubAttribute = GetType().CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Equals(typeof(ReactiveHubAttribute)));
            if (reactiveHubAttribute is not null)
            {
                var hubEndpointPosition = reactiveHubAttribute.Constructor.GetParameters().FirstOrDefault(arg => arg.Name == "hubEndpoint")?.Position;
                return hubEndpointPosition.HasValue ?
                    reactiveHubAttribute.ConstructorArguments[hubEndpointPosition.Value].Value.ToString()
                    : null;
            }
            return null;
        }

        /// <summary>
        /// Method to build domain class
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <returns></returns>
        protected T Factory<T>() where T : LightDomain, new()
        {
            // Verify if there's erros
            if (_inputValidationErrors.Count > 0)
            {
                // Throws the error code from errors list of input validation to View Model
                throw new InvalidInputLightException(_inputValidationErrors);
            }

            var domain = LightDomain.FactoryDomain<T>();
            domain.Logger = Logger;
            domain.Context = GetContext();
            domain.Telemetry = Telemetry;
            domain.Telemetry.OperationId = domain.Context.OperationId;
            domain.Cache = Cache;
            domain.CritictHandler = _criticHandler;

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
        /// The method receives the ViewModel to input validation and add on errors list.
        /// (if there are errors after validation ViewModel.)
        /// </summary>
        /// <param name="viewModel">The ViewModel to input validation</param>
        protected void ValidateInput(dynamic viewModel)
        {
            if (viewModel is null)
            {
                AddInputError("paremeters malformed or empty");
                return;
            }

            viewModel.InputErrors = _inputValidationErrors;
            viewModel.Validate();
            ResultValidation result = viewModel.Validator.Validate(viewModel);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    // The method receive the error code to add on errors list of input validation.
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

        public abstract void Initialize();

        /// <summary>
        /// Invoked on new connection to the client hub
        /// </summary>
        /// <returns></returns>
        public override Task OnConnectedAsync() { return base.OnConnectedAsync(); }

        /// <summary>
        /// Invoked on connection dropped from the client hub
        /// </summary>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception exception) { return base.OnDisconnectedAsync(exception); }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}