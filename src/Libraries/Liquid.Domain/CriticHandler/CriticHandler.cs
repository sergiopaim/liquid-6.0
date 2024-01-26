using Liquid.Base;
using Liquid.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Liquid.Domain
{
    /// <summary>
    /// Handler of (business) domain critics
    /// </summary>
    public class CriticHandler : ICriticHandler
    {
        /// <summary>
        /// List of domain critics
        /// </summary>
        public List<ICritic> Critics { get; private set; } = new();
        /// <summary>
        /// Status code of the domain operation
        /// </summary>
        public StatusCode StatusCode { get; set; } = StatusCode.OK;
        /// <summary>
        /// Indication whether a NoContent error was issued
        /// </summary>
        public bool HasNoContentError => StatusCode == StatusCode.NoContent;
        /// <summary>
        /// Indication whether a Conflict error was issued
        /// </summary>
        public bool HasConflictError => StatusCode == StatusCode.Conflict;
        /// <summary>
        /// Indication whether the status code is not OK
        /// </summary>
        public bool HasNotGenericReturn => StatusCode != StatusCode.OK;
        /// <summary>
        /// Indication whether a BadRequest error was issued
        /// </summary>
        public bool HasBadRequestError => StatusCode == StatusCode.BadRequest;
        /// <summary>
        /// Indication whether one or more business errors were issued
        /// </summary>
        public bool HasBusinessErrors => Critics.Exists(c => c.Type == CriticType.Error);
        /// <summary>
        /// Indication whether one or more business warnings were issued
        /// </summary>
        public bool HasBusinessWarnings => Critics.Exists(c => c.Type == CriticType.Warning);
        /// <summary>
        /// Indication whether one or more business information messages were issued
        /// </summary>
        public bool HasBusinessInfo => Critics.Exists(c => c.Type == CriticType.Info);
        /// <summary>
        /// Indication whether one or more business messages were issued
        /// </summary>
        public bool HasMessages => Critics.Count > 0;
        /// <summary>
        /// Creates a critic handler from another domain response
        /// </summary>
        /// <param name="response">The domain response containing critics</param>
        /// <returns></returns>
        public static CriticHandler FromResponse(DomainResponse response)
        {
            var handler = new CriticHandler();

            if (response?.Critics is not null)
            {
                handler.Critics.AddRange(response?.Critics);
            }

            return handler;
        }

        /// <summary>
        /// Creates a list of object Critic. 
        /// Each Critic contains the error code.
        /// and the CriticType is a Error.
        /// </summary>
        /// <param name="errorCode">Error code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on errorCode</param>
        public void AddBusinessError(string errorCode, params object[] args)
        {
            Critic critic = new();
            critic.AddError(errorCode, LocalizeMessage(errorCode, args));
            Critics.Add(critic);
        }

        /// <summary>
        /// Creates a list of object Critic. 
        /// Each Critic contains the error code.
        /// and the CriticType is a Info.
        /// </summary>
        /// <param name="infoCode">Info code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on infoCode</param>
        public void AddBusinessInfo(string infoCode, params object[] args)
        {
            Critic critic = new();
            critic.AddInfo(infoCode, LocalizeMessage(infoCode, args));
            Critics.Add(critic);
        }

        /// <summary>
        /// Creates a list of object Critic. 
        /// Each Critic contains the error code.
        /// and the CriticType is a Warning.
        /// </summary>
        /// <param name="warningCode">Warning code (to be also localized in current culture)</param>
        /// <param name="args">List of parameters to expand inside localized message based on warningCode</param>
        public void AddBusinessWarning(string warningCode, params object[] args)
        {
            Critic critic = new();
            critic.AddWarning(warningCode, LocalizeMessage(warningCode, args));
            Critics.Add(critic);
        }

        /// <summary>
        /// Localizes the critic message
        /// </summary>
        /// <param name="code">code entry to localized</param>
        /// <param name="args">arguments to interpolate</param>
        /// <returns></returns>
        public static string LocalizeMessage(string code, params object[] args)
        {
            string message = LightLocalizer.Localize(code, args);

            if (message == code && args?.Length > 0 && args[0]?.GetType() == typeof(string))
                return args[0] as string;
            else
                return message;
        }

        /// <summary>
        /// Resets the NoContent error
        /// </summary>
        public void ResetNoContentError()
        {
            if (StatusCode == StatusCode.NoContent)
                StatusCode = StatusCode.OK;
        }

        /// <summary>
        /// Resets the Conflict error
        /// </summary>
        public void ResetConflictError()
        {
            if (StatusCode == StatusCode.Conflict)
                StatusCode = StatusCode.OK;
        }

        /// <summary>
        /// Indication whether any critical error was issued
        /// </summary>
        /// <returns></returns>
        public bool HasCriticalErrors()
        {
            return HasBusinessErrors || HasNotGenericReturn;
        }

        /// <summary>
        /// Returns the critical errors issued
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object[]> GetCriticalErrors()
        {
            var errors = Critics.ToDictionary(c => c.Code, c => new object[] { c.Message });

            if (HasBadRequestError)
                errors.Add("400", new object[] { "bad request" });

            if (HasNoContentError)
                errors.Add("204", new object[] { "no content" });

            if (HasConflictError)
                errors.Add("409", new object[] { "conflict" });

            return errors;
        }
    }
}
