using Liquid.Base;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// This Controller and its action method handles incoming browser requests, 
    /// retrieves necessary model data and returns appropriate responses.
    /// </summary>
    public abstract class LightController : Controller
    {
        private readonly Dictionary<string, object[]> _inputValidationErrors = new();
        //Cloning TLightelemetry service singleton because it services multiple LightDomain instances from multiple threads with instance variables
        private ILightTelemetry telemetry = null;
        protected ILightTelemetry Telemetry
        {
            get
            {
                telemetry ??= WorkBench.Telemetry?.CloneService() as ILightTelemetry;
                return telemetry;
            }
        }

        /// <summary>
        /// Gets the id of the current user
        /// </summary>
        protected string CurrentUserId => User?.FindFirstValue("sub") ?? User?.FindFirstValue(JwtClaimTypes.UserId);

        /// <summary>
        /// Gets the first name of the current user
        /// </summary>
        protected string CurrentUserFirstName => User?.FindFirstValue("GivenName") ?? "";

        /// <summary>
        /// Gets the full name of the current user
        /// </summary>
        protected string CurrentUserFullName => CurrentUserFirstName + " " + User?.FindFirstValue("Surname") ?? "";

        /// <summary>
        /// Gets the e-mail address of the current user
        /// </summary>
        protected string CurrentUserEmail => User?.FindFirstValue("Email") ?? "";

        /// <summary>
        /// Checks if the current user is in the given security role
        /// </summary>
        /// <param name="role">Security role</param>
        /// <returns>True if the user is in the role</returns>
        protected bool CurrentUserIsInRole(string role) => User?.IsInRole(role) ?? false;

        /// <summary>
        /// Checks if the current user is in any of the given security roles
        /// </summary>
        /// <param name="roles">Security roles in a comma separated string</param>
        /// <returns>True if the user is in any role</returns>
        protected bool CurrentUserIsInAnyRole(string roles)
        {
            if (User is null)
                return false;

            return roles.Split(",")
                        .Any(r => User.IsInRole(r.Trim()));
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

            return roles.Any(r => User.IsInRole(r.Trim()));
        }

        protected static ILightLogger Logger => WorkBench.Logger;
        protected static ILightCache Cache => WorkBench.Cache;

        private LightContext context;
        protected LightContext Context 
        { 
            get => context; 
            set
            { 
                context = value;
                telemetry.OperationId = value.OperationId;
            }
        }
        public IHttpContextAccessor HttpContextAccessor { get; set; }

        //Instance of CriticHandler to inject on the others classes
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

            return new(HttpContextAccessor)
            {
                User = HttpContext?.User,
                OperationId = operationId
            };
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
        /// Builds a IActionResult based on the DomainResponse
        /// </summary>
        /// <param name="response"></param>
        /// <returns>IAResponsible</returns>
        protected IActionResult Result(DomainResponse response)
        {
            if (response is null)
                return Ok();

            if (response.NotContent)
                return NoContent();

            if (response.ConflictMessage)
                return Conflict(response);

            if (response.BadRequestMessage)
                return BadRequest(response);

            if (response.GenericReturnMessage) 
                return StatusCode((int) response.StatusCode, response);

            return Ok(response);
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
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}