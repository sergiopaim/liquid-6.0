using Liquid.Base;
using Liquid.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Liquid.Domain
{
    /// <summary>
    /// A CQRS command prototype (ancestor)
    /// </summary>
    /// <typeparam name="T"></typeparam>

    public abstract class LightCommand<T> : LightDomain
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
        /// The parameters for the command
        /// </summary>
        protected T Command { get; set; }

        /// <summary>
        /// Runs the command
        /// </summary>
        /// <param name="command">Command parameters</param>
        /// <returns>Domain response</returns>
        public async Task<DomainResponse> Run(T command)
        {
            //Injects the command and call business domain logic to handle it
            Command = command;

            Telemetry.TrackEvent($"Command {this.GetType().Name}", $"userId: {CurrentUserId}");

            //Calls execute operation asyncronously
            return await Execute();
        }

        /// <summary>
        /// Method to implement the actual execution of the command
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

        /// <summary>
        /// Returns an instance of a (sub) LightCommand
        /// responsible for delegate (business) functionality
        /// </summary>
        /// <typeparam name="T">the delegate LightDomain class</typeparam>
        /// <returns>Instance of the LightDomain class</returns>
#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
        protected virtual T SubCommand<T>() where T : LightDomain, new()
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
        {
            return Service<T>();
        }

        internal override void ExternalInheritanceNotAllowed()
        { }

    }

    /// <summary>
    /// A generic command without any parameters
    /// </summary>
    public abstract class LightCommand : LightCommand<EmptyCommandRequest>
    {

        /// <summary>
        /// Runs the command without any parameters
        /// </summary>
        /// <returns>Domain response</returns>
        public async Task<DomainResponse> Run()
        {
            Command = default;

            Telemetry.TrackEvent($"Command {this.GetType().Name.Replace("Command", "")}");

            //Calls execute operation asyncronously
            return await Execute();
        }

        /// <summary>
        /// Returns an instance of a domain LightService 
        /// responsible for delegate (business) functionality
        /// </summary>
        /// <typeparam name="T">the delegate LightDomain class</typeparam>
        /// <returns>Instance of the LightDomain class</returns>
        protected virtual new T Service<T>() where T : LightService, new()
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

