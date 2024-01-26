using Liquid.Base;
using Liquid.Runtime;
using System.Collections.Generic;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace Liquid.Domain
{
    /// <summary>
    /// A CQRS query prototype (ancestor)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LightQuery<T> : LightDomain
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
        /// The parameters for the query
        /// </summary>
        protected T Query { get; set; }

        /// <summary>
        /// Runs the query
        /// </summary>
        /// <param name="query">Query parameters</param>
        /// <returns>Domain response</returns>
        public async Task<DomainResponse> Run(T query)
        {
            //Injects the command and call business domain logic to handle it
            Query = query;

            Telemetry.TrackEvent($"Query {this.GetType().Name}", $"userId: {CurrentUserId}");

            //Calls execute operation asyncronously
            return await Execute();
        }

        /// <summary>
        /// Method to implement the actual execution of the query
        /// </summary>
        /// <returns></returns>
        protected abstract Task<DomainResponse> Execute();
        /// <summary>
        /// Returns an instance of a domain LightService 
        /// responsible for delegate (business) functionality
        /// </summary>
        /// <typeparam name="T">the delegate LightDomain class</typeparam>
        /// <returns>Instance of the LightDomain class</returns>
#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
        protected virtual T Service<T>() where T : LightDomain, new()
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
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

    /// <summary>
    /// A generic query without any parameters
    /// </summary>
    public abstract class LightQuery : LightQuery<EmptyQueryRequest>
    {

        /// <summary>
        /// Runs the query without any parameters
        /// </summary>
        /// <returns>Domain response</returns>
        public async Task<DomainResponse> Run()
        {
            Query = default;

            Telemetry.TrackEvent($"Query {this.GetType().Name.Replace("Query", "")}");

            //Calls execute operation asyncronously
            return await Execute();
        }

        internal override void ExternalInheritanceNotAllowed()
        { }
    }
}

