using Liquid.Base;
using Liquid.Runtime;
using System.Collections.Generic;
using System;
using System.Security.Claims;
using System.Linq;

namespace Liquid.Domain
{
    /// <summary>
    /// Basic class to implement business domain logic in Object/Component orientation style
    /// </summary>
    public abstract class LightService : LightDomain
    {
        /// <summary>
        /// List of lightDomain delegates factoried
        /// </summary>
        protected readonly Dictionary<Type, object> delegates = new();
        /// <summary>
        /// Gets the id of the current user
        /// </summary>
        protected new string CurrentUserId => Context.User?.FindFirstValue("sub") ?? Context.User?.FindFirstValue(JwtClaimTypes.UserId);

        /// <summary>
        /// Gets the first name of the current user
        /// </summary>
        protected new string CurrentUserFirstName => Context.User?.FindFirstValue("GivenName") ?? "";

        /// <summary>
        /// Gets the full name of the current user
        /// </summary>
        protected new string CurrentUserFullName => CurrentUserFirstName + " " + Context.User?.FindFirstValue("Surname") ?? "";

        /// <summary>
        /// Returns an instance of a domain LightService 
        /// responsible for delegate (business) functionality
        /// </summary>
        /// <typeparam name="T">the delegate LightDomain class</typeparam>
        /// <returns>Instance of the LightDomain class</returns>
        protected virtual T Service<T>() where T : LightService, new()
        {
            T domain = (T)delegates.FirstOrDefault(s => s.Key == typeof(T)).Value;

            if (domain is null)
            {
                domain = FactoryDomain<T>();
                domain.CritictHandler = CritictHandler;
                domain.Telemetry = Telemetry;
                domain.Context = Context;
                domain.Logger = Logger;
                domain.Cache = Cache;

                delegates.Add(typeof(T), domain);
            }
            return domain;
        }

        internal override void ExternalInheritanceNotAllowed()
        { }
    }
}
