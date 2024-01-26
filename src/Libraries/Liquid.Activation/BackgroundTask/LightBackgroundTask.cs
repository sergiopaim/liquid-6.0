using Liquid.Base;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCrontab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Abstract class based on IHostedService that run over OWIN and LighBackgroundTask 
    /// prepare and execute a back ground tasks signed's.
    /// </summary>
    public abstract class LightBackgroundTask : IHostedService, IDisposable
    {
        // https://github.com/pgroene/ASPNETCoreScheduler/tree/master/ASPNETCoreScheduler
        private readonly CrontabSchedule _schedule;
        private DateTime NextRun;

        private readonly IServiceScopeFactory ServiceScopeFactory;
        private readonly Dictionary<string, object[]> _inputValidationErrors = new();
        protected ILightTelemetry Telemetry { get; } = WorkBench.Telemetry?.CloneService() as ILightTelemetry;
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

        public ICriticHandler CriticHandler { get; } = new CriticHandler();

        private Task ExecutingTask;
        private readonly CancellationTokenSource CancellationToken = new();

        /// <summary>
        /// Crontab expression
        /// </summary>
        protected virtual string Schedule { get; } = "* * * * *"; // runs every minute by default

        public LightBackgroundTask(IServiceScopeFactory serviceScopeFactory)
        {
            _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = Schedule.Split(' ').Length == 6 });
            NextRun = WorkBench.UtcNow;  // run on startup
            ServiceScopeFactory = serviceScopeFactory;
        }

        protected T Factory<T>() where T : LightDomain, new()
        {
            // Verify if there's erros
            if (_inputValidationErrors.Count > 0)
            {
                // Throws the error code from errors list of input validation to View Model
                throw new InvalidInputLightException(_inputValidationErrors);
            }
            var domain = LightDomain.FactoryDomain<T>();
            domain.Cache = Cache;
            domain.Context = Context ?? new LightContext
            {
                OperationId = WorkBench.GenerateNewOperationId(),
                User = JwtSecurityCustom.DecodeToken(JwtSecurityCustom.Config.SysAdminJWT)
            };
            domain.Telemetry = Telemetry;
            domain.Telemetry.OperationId = domain.Context.OperationId;
            domain.CritictHandler = CriticHandler;
            return domain;
        }

        /// <summary>
        /// Start a background task async
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            ExecutingTask = ExecuteAsync(CancellationToken.Token);

            if (ExecutingTask.IsCompleted)
                return ExecutingTask;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop a background task async
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (ExecutingTask is null)
                return;

            try
            {
                CancellationToken.Cancel();
            }
            finally
            {
                await Task.WhenAny(ExecutingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }

        /// <summary>
        /// Execute a background Task async
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        protected virtual async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            do
            {
                var now = WorkBench.UtcNow;
                if (now > NextRun)
                {
                    await Process();
                    NextRun = _schedule.GetNextOccurrence(now);
                }
                await Task.Delay(5000, cancellationToken);
            }
            while (!cancellationToken.IsCancellationRequested);
        }

        /// <summary>
        /// Process a brackground task async.
        /// </summary>
        /// <returns></returns>
        protected async Task Process()
        {
            using var scope = ServiceScopeFactory.CreateScope();
            await ProcessInScope(scope.ServiceProvider);
        }

        public abstract Task ProcessInScope(IServiceProvider serviceProvider);

        public void Dispose()
        {
            CancellationToken.Dispose();
            GC.SuppressFinalize(this);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}