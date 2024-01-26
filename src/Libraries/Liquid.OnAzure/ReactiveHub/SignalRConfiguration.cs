using FluentValidation;
using Liquid.Runtime;

namespace Liquid.OnAzure
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class SignalRConfiguration : LightConfig<SignalRConfiguration>
    {
        /// <summary>
        /// String of connection with the Azure SignalR service.
        /// </summary>
        public string SignalRServiceConnStr { get; set; }

        public string ReactiveHubHost { get; set; }
        public bool DebugLog { get; set; }

        /// <summary>
        ///  The method used to properties validation of Configuration.
        /// </summary>
        public override void Validate()
        {
            if (!WorkBench.IsDevelopmentEnvironment)
            {
                RuleFor(d => SignalRServiceConnStr).NotEmpty().WithError("'SignalRServiceConnectionString' property was not informed in 'SignalRService' configuration session.");
            }

            if (!string.IsNullOrEmpty(SignalRServiceConnStr))
            {
                RuleFor(d => SignalRServiceConnStr).Matches("Endpoint=").WithError("No Endpoint member was found in property 'SignalRServiceConnectionString' in 'SignalRService' configuration session.");
                RuleFor(d => SignalRServiceConnStr).Matches("AccessKey=").WithError("No AcessKeymember was found in property 'SignalRServiceConnectionString' in 'SignalRService' configuration session.");
            }
            RuleFor(d => ReactiveHubHost).NotEmpty().WithError("'ReactiveHubHost' property was not informed in 'SignalRService' configuration session.");
        }

        private static SignalRConfiguration _config;
        private static SignalRConfiguration Config
        {
            get
            {
                _config ??= LightConfigurator.LoadConfig<SignalRConfiguration>("SignalRHub");
                return _config;
            }
        }

        /// <summary>
        /// Gets the ConnectionString to SignalR Service
        /// </summary>
        /// <returns></returns>
        //public static string GetConnectionString() => WorkBench.IsDevelopmentEnvironment ? "" : Config.SignalRServiceConnStr;
        public static string GetConnectionString() => Config.SignalRServiceConnStr;
        /// <summary>
        /// Gets the LocalServer URI
        /// </summary>
        /// <returns></returns>
        public static string GetReactiveHubHost() => Config.ReactiveHubHost;
        /// <summary>
        /// Gets the DebugLog option
        /// </summary>
        /// <returns></returns>
        public static bool GetDebugLog() => Config.DebugLog;

    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}