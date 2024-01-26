using Liquid.Domain.API;
using Liquid.Interfaces;
using Liquid.Runtime;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;

namespace Liquid.Base
{
    /// <summary>
    /// Class responsible for business logic and operations implemented as methods from the Domain Classes
    /// </summary>
    public abstract class LightDomain : ILightDomain
    {
        /// <summary>
        /// The current active repository service
        /// </summary>
        protected static ILightRepository Repository => WorkBench.Repository;

        /// <summary>
        /// The current active media storage service
        /// </summary>
        protected static ILightMediaStorage MediaStorage => WorkBench.MediaStorage;

        /// <summary>
        /// The current active telemetry service
        /// </summary>
        public ILightTelemetry Telemetry { get; set; }

        /// <summary>
        /// Current operation context
        /// </summary>
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
        /// The current active cache service
        /// </summary>
        public ILightCache Cache { get; set; }

        /// <summary>
        /// The current active logger service
        /// </summary>
        public ILightLogger Logger { get; set; }

        /// <summary>
        /// Indicates whether at least one Business error has been issued
        /// </summary>
        protected bool HasBusinessErrors => PrivateCriticHandler is not null && PrivateCriticHandler.HasBusinessErrors;

        /// <summary>
        /// Indicates whether at least NoContent error has been issued
        /// </summary>
        protected bool HasNoContentError => PrivateCriticHandler is not null && PrivateCriticHandler.HasNoContentError;

        /// <summary>
        /// Indicates whether at least Conflict error has been issued
        /// </summary>
        protected bool HasConflictError => PrivateCriticHandler is not null && PrivateCriticHandler.HasConflictError;

        /// <summary>
        /// Resets the any NoContent error critic status
        /// </summary>
        protected void ResetNoContentError()
        {
            PrivateCriticHandler?.ResetNoContentError();
        }

        /// <summary>
        /// Resets the any Conflict error critic status
        /// </summary>
        protected void ResetConflictError()
        {
            PrivateCriticHandler?.ResetConflictError();
        }

        private ICriticHandler PrivateCriticHandler { get; set; }
        /// <summary>
        /// The critic handler
        /// </summary>
        public ICriticHandler CritictHandler { get { return PrivateCriticHandler; } set { PrivateCriticHandler = value; } }
        internal abstract void ExternalInheritanceNotAllowed();

        /// <summary>
        /// Instanciates a LightApi injecting current domain context
        /// </summary>
        /// <param name="apiName">The name of the API</param>
        /// <returns></returns>
        protected virtual LightApi FactoryLightApi(string apiName)
        {
            var token = JwtSecurityCustom.GetJwtToken(Context.User?.Identity as ClaimsIdentity);

            return new(apiName, token, this);
        }

        /// <summary>
        /// Factories a LightMessage 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandType"></param>
        /// <returns></returns>
        protected virtual T FactoryLightMessage<T>(ILightEnum commandType) where T : ILightMessage, new()
        {
            var message = new T
            {
                TransactionContext = Context,
                CommandType = commandType?.Code
            };
            return message;
        }

        #region NoContent

        /// <summary>
        /// Add to the scope that some critic has a not found type of error
        /// </summary>
        protected DomainResponse NoContent()
        {
            CritictHandler.StatusCode = StatusCode.NoContent;
            return Response();
        }

        #endregion

        #region Unauthorized

        /// <summary>
        /// Add to the scope that some critic has a unauthorized type of error
        /// </summary>
        protected DomainResponse Unauthorized()
        {
            CritictHandler.StatusCode = StatusCode.Unauthorized;
            return Response();
        }

        /// <summary>
        /// Add to the scope that some critic has a unauthorized type of error
        /// <param name="errorCode">error code</param>
        /// </summary>
        protected DomainResponse Unauthorized(string errorCode)
        {
            return Unauthorized(errorCode, default(string));
        }

        /// <summary>
        /// Add to the scope that some critic has a unauthorized type of error
        /// <param name="errorCode">error code</param>
        /// <param name="message">error message</param>
        /// </summary>
        protected DomainResponse Unauthorized(string errorCode, string message)
        {
            AddBusinessError(errorCode, message);
            return Unauthorized();
        }

        /// <summary>
        /// Add to the scope that some critic has a unauthorized type of error
        /// <param name="errorCode">error code</param>
        /// <param name="args">Arguments to interpolate</param>
        /// </summary>
        protected DomainResponse Unauthorized(string errorCode, params object[] args)
        {
            AddBusinessError(errorCode, args);
            return Unauthorized();
        }

        #endregion

        #region Forbidden

        /// <summary>
        /// Add to the scope that some critic has a forbidden type of error
        /// </summary>
        protected DomainResponse Forbidden()
        {
            CritictHandler.StatusCode = StatusCode.Forbidden;
            return Response();
        }

        /// <summary>
        /// Add to the scope that some critic has a forbidden type of error
        /// <param name="errorCode">error code</param>
        /// </summary>
        protected DomainResponse Forbidden(string errorCode)
        {
            return Forbidden(errorCode, default(string));
        }

        /// <summary>
        /// Add to the scope that some critic has a forbidden type of error
        /// <param name="errorCode">error code</param>
        /// <param name="message">error message</param>
        /// </summary>
        protected DomainResponse Forbidden(string errorCode, string message)
        {
            AddBusinessError(errorCode, message);
            return Forbidden();
        }

        /// <summary>
        /// Add to the scope that some critic has a forbidden type of error
        /// <param name="errorCode">error code</param>
        /// <param name="args">Arguments to interpolate</param>
        /// </summary>
        protected DomainResponse Forbidden(string errorCode, params object[] args)
        {
            AddBusinessError(errorCode, args);
            return Forbidden();
        }

        #endregion

        #region Conflict

        /// <summary>
        /// Add to the scope that some critic has a conflict type of error
        /// </summary>
        protected DomainResponse Conflict()
        {
            CritictHandler.StatusCode = StatusCode.Conflict;
            return Response();
        }

        /// <summary>
        /// Add to the scope that some critic has a conflict type of error
        /// <param name="errorCode">error code</param>
        /// </summary>
        protected DomainResponse Conflict(string errorCode)
        {
            return Conflict(errorCode, default(string));
        }

        /// <summary>
        /// Add to the scope that some critic has a conflict type of error
        /// <param name="errorCode">error code</param>
        /// <param name="message">error message</param>
        /// </summary>
        protected DomainResponse Conflict(string errorCode, string message)
        {
            AddBusinessError(errorCode, message);
            return Conflict();
        }

        /// <summary>
        /// Add to the scope that some critic has a conflict type of error
        /// <param name="errorCode">error code of the message</param>
        /// <param name="args">Arguments to interpolate</param>
        /// </summary>
        protected DomainResponse Conflict(string errorCode, params object[] args)
        {
            AddBusinessError(errorCode, args);
            return Conflict();
        }

        #endregion

        #region BadRequest

        /// <summary>
        /// Add to the scope that some critic has a bad request type of error
        /// </summary>
        protected DomainResponse BadRequest()
        {
            CritictHandler.StatusCode = StatusCode.BadRequest;
            return Response();
        }

        /// <summary>
        /// Add to the scope that some critic has a bad request type of error
        /// <param name="errorCode">error code</param>
        /// </summary>
        protected DomainResponse BadRequest(string errorCode)
        {
            return BadRequest(errorCode, default(string));
        }

        /// <summary>
        /// Add to the scope that some critic has a bad request type of error
        /// <param name="errorCode">error code</param>
        /// <param name="message">error message</param>
        /// </summary>
        protected DomainResponse BadRequest(string errorCode, string message)
        {
            AddBusinessError(errorCode, message);
            return BadRequest();
        }

        /// <summary>
        /// Add to the scope that some critic has a bad request type of error
        /// <param name="errorCode">error code</param>
        /// <param name="args">Arguments to interpolate</param>
        /// </summary>
        protected DomainResponse BadRequest(string errorCode, params object[] args)
        {
            AddBusinessError(errorCode, args);
            return BadRequest();
        }

        #endregion

        #region AddBusinessError and BusinessError

        /// <summary>
        /// Method to return the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="errorCode">Error code (to be also localized in current culture)</param>
        protected DomainResponse BusinessError(string errorCode)
        {
            AddBusinessError(errorCode);
            return Response();
        }

        /// <summary>
        /// Method to return the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="errorCode">error code</param>
        /// <param name="message">error message</param>
        protected DomainResponse BusinessError(string errorCode, string message)
        {
            AddBusinessError(errorCode, new[] { message });
            return Response();
        }

        /// <summary>
        /// Method to return the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="errorCode">Error code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on errorCode</param>
        protected DomainResponse BusinessError(string errorCode, params object[] args)
        {
            AddBusinessError(errorCode, args);
            return Response();
        }

        /// <summary>
        /// Method add the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="errorCode">Error code (to be also localized in current culture)</param>
        protected void AddBusinessError(string errorCode)
        {
            PrivateCriticHandler.AddBusinessError(errorCode);
        }

        /// <summary>
        /// Method add the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="errorCode">error code</param>
        /// <param name="message">error message</param>
        protected void AddBusinessError(string errorCode, string message)
        {
            PrivateCriticHandler.AddBusinessError(errorCode, new[] { message });
        }

        /// <summary>
        /// Method add the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="errorCode">Error code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on errorCode</param>
        protected void AddBusinessError(string errorCode, params object[] args)
        {
            PrivateCriticHandler.AddBusinessError(errorCode, args);
        }

        #endregion

        #region AddBusinessWarning and BusinessWarning

        /// <summary>
        /// Method return the warning to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="warningCode">Warning code (to be also localized in current culture)</param>
        protected DomainResponse BusinessWarning(string warningCode)
        {
            AddBusinessWarning(warningCode);
            return Response();
        }


        /// <summary>
        /// Method return the warning to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="warningCode">Warning code (to be also localized in current culture)</param>
        /// <param name="message">error message</param>
        protected DomainResponse BusinessWarning(string warningCode, string message)
        {
            AddBusinessWarning(warningCode, new[] { message });
            return Response();
        }

        /// <summary>
        /// Method return the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="warningCode">Warning code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on warningCode</param>
        protected DomainResponse BusinessWarning(string warningCode, params object[] args)
        {
            AddBusinessWarning(warningCode, args);
            return Response();
        }

        /// <summary>
        /// Method add the warning to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="warningCode">Warning code (to be also localized in current culture)</param>
        protected void AddBusinessWarning(string warningCode)
        {
            PrivateCriticHandler.AddBusinessWarning(warningCode);
        }


        /// <summary>
        /// Method add the warning to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="warningCode">Warning code (to be also localized in current culture)</param>
        /// <param name="message">error message</param>
        protected void AddBusinessWarning(string warningCode, string message)
        {
            PrivateCriticHandler.AddBusinessWarning(warningCode, new[] { message });
        }

        /// <summary>
        /// Method add the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="warningCode">Warning code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on warningCode</param>
        protected void AddBusinessWarning(string warningCode, params object[] args)
        {
            PrivateCriticHandler.AddBusinessWarning(warningCode, args);
        }

        #endregion

        #region AddBusinessInfo and BusinessInfo

        /// <summary>
        /// /// Method to return the information to the Critic Handler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="infoCode">Info code (to be also localized in current culture)</param>
        protected DomainResponse BusinessInfo(string infoCode)
        {
            AddBusinessInfo(infoCode);
            return Response();
        }

        /// <summary>
        /// /// Method to return the information to the Critic Handler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="infoCode">Info code (to be also localized in current culture)</param>
        /// <param name="message">error message</param>
        protected DomainResponse BusinessInfo(string infoCode, string message)
        {
            AddBusinessInfo(infoCode, new[] { message });
            return Response();
        }

        /// <summary>
        /// Method to return the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="infoCode">Info code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on infoCode</param>
        protected DomainResponse BusinessInfo(string infoCode, params object[] args)
        {
            AddBusinessInfo(infoCode, args);
            return Response();
        }

        /// <summary>
        /// /// Method add the information to the Critic Handler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="infoCode">Info code (to be also localized in current culture)</param>
        protected void AddBusinessInfo(string infoCode)
        {
            PrivateCriticHandler.AddBusinessInfo(infoCode);
        }

        /// <summary>
        /// /// Method add the information to the Critic Handler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="infoCode">Info code (to be also localized in current culture)</param>
        /// <param name="message">error message</param>
        protected void AddBusinessInfo(string infoCode, string message)
        {
            PrivateCriticHandler.AddBusinessInfo(infoCode, new[] { message });
        }

        /// <summary>
        /// Method add the error code to the CriticHandler
        /// and add in Critics list to build the object InvalidInputLightException
        /// </summary>
        /// <param name="infoCode">Info code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on infoCode</param>
        protected void AddBusinessInfo(string infoCode, params object[] args)
        {
            PrivateCriticHandler.AddBusinessInfo(infoCode, args);
        }

        #endregion

        /// <summary>
        /// Returns a DomainResponse class with data serialized on JSON
        /// </summary>
        /// <typeparam name="T">The desired type LightViewModel</typeparam>
        /// <returns>Instance of the specified DomainResponse</returns>
        protected DomainResponse Response<T>(T data)
        {
            return new DomainResponse(data.ToJsonDocument(), Context, PrivateCriticHandler);
        }

        /// <summary>
        /// Returns a DomainResponse class with empty data serialized as JSON
        /// </summary>
        /// <returns>Instance of the specified DomainResponse</returns>
        protected DomainResponse Response()
        {
            return Response(new ExpandoObject());
        }

        /// <summary>
        /// Returns a  instance of a LightDomain class for calling business domain logic
        /// </summary>
        /// <typeparam name="T">desired LightDomain subtype</typeparam>
        /// <returns>Instance of the specified LightDomain subtype</returns>
        public static T FactoryDomain<T>() where T : LightDomain, new()
        {
            ILightDomain service = (ILightDomain)new T();
            return (T)service;
        }
    }
}
