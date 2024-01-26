using Liquid.Base.Test;
using Liquid.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Liquid
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class WorkBench
    {
        /// <summary>
        /// _singletonCache exposed to be used by Health Check
        /// </summary>
        private static readonly Dictionary<WorkBenchServiceType, IWorkBenchService> singletonCache = new();
        public static Dictionary<WorkBenchServiceType, IWorkBenchService> SingletonCache { get => singletonCache; }

        public static ILightRepository Repository => GetService<ILightRepository>(WorkBenchServiceType.Repository);
        public static ILightMediaStorage MediaStorage => GetService<ILightMediaStorage>(WorkBenchServiceType.MediaStorage);
        public static ILightTelemetry Telemetry => GetService<ILightTelemetry>(WorkBenchServiceType.Telemetry);
        public static ILightCache Cache => GetService<ILightCache>(WorkBenchServiceType.Cache, false);
        public static IMessageBrokerWrapper Event => GetService<IMessageBrokerWrapper>(WorkBenchServiceType.DataHub, false);
        public static ILightLogger Logger => GetService<ILightLogger>(WorkBenchServiceType.Logger, false);
        public static IConfiguration Configuration { get; set; }

        // Indicates if the Workbench Services are initialized.
        private static bool _isServicesUp;

        private static string consoleLog = string.Empty;
        private static bool htmlResult = true;

        /// <summary>
        /// Gets the console messages log in memory and clears it
        /// </summary>
        /// <returns>The console messages log stored so far</returns>
        public static void WriteLog(HttpResponse response = null)
        {
            string logSoFar = consoleLog;
            consoleLog = string.Empty;

            if (response is not null)
            {
                if (htmlResult)
                {
                    response.ContentType = "text/html";
                    response.WriteAsync($"<!DOCTYPE html><html style=\"font-family:courier;\"><head><title>Output {Environment.MachineName}</title></head><body>{logSoFar}</body></html>").Wait();
                }
                else
                {
                    response.ContentType = "text/plain";
                    response.WriteAsync(logSoFar).Wait();
                }
            }
        }

        /// <summary>
        /// Clears the console
        /// </summary>
        public static void ConsoleClear()
        {
            consoleLog = string.Empty;
            Console.Clear();
        }

        /// <summary>
        /// Clears the console
        /// </summary>
        public static void ConsoleResultFormat(bool html = true)
        {
            htmlResult = html;
        }

        /// <summary>
        /// Writes a new line message to the console and stores it in memory for later recovery
        /// </summary>
        /// <returns>The console log stored so far</returns>
        public static void ConsoleWriteLine(string message = "", params object[] parms)
        {
            message = InterpolateMessage(message, parms);
            Console.WriteLine(message);

            if (string.IsNullOrEmpty(message))
            {
                if (htmlResult)
                    consoleLog += $"<br/>";
                else
                    consoleLog += $"\n";

            }
            else
            {
                if (htmlResult)
                    consoleLog += $"<p>{message.Replace("\n", "<br/>")}</p>";
                else
                    consoleLog += $"\n" + message;
            }

            //Defensively limites the log up to 10KB
            if (consoleLog.Length > 10240)
                consoleLog = consoleLog[..10240];
        }

        /// <summary>
        /// Writes a new line message (hightlighted as error) to the console and stores it in memory for later recovery
        /// </summary>
        /// <returns>The console log stored so far</returns>
        public static void ConsoleWriteErrorLine(string message = "", params object[] parms)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.Yellow;

            message = InterpolateMessage(message, parms);
            Console.WriteLine(message);

            if (string.IsNullOrEmpty(message))
            {
                if (htmlResult)
                    consoleLog += $"<br/>";
                else
                    consoleLog += "\n";
            }
            else
            {
                if (htmlResult)
                    consoleLog += $"<p style=\"background-color:darkred;color:yellow\">{message.Replace("\n", "<br/>")}</p>";
                else
                    consoleLog += $"\n***\n*** {message.Replace("\n", "\n***")}\n***\n";
            }

            //Defensively limites the log up to 10KB
            if (consoleLog.Length > 10240)
                consoleLog = consoleLog[..10240];

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Writes a new line message (hightlighted) to the console and stores it in memory for later recovery
        /// </summary>
        /// <returns>The console log stored so far</returns>
        public static void ConsoleWriteHighlightedLine(string message = "", params object[] parms)
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;

            message = InterpolateMessage(message, parms);
            Console.WriteLine(message);

            if (string.IsNullOrEmpty(message))
            {
                if (htmlResult)
                    consoleLog += $"<br/>";
                else
                    consoleLog += "\n";
            }
            else
            {
                if (htmlResult)
                    consoleLog += $"<p style=\"background-color:gray;color:black\">{message.Replace("\n", "<br/>")}</p>";
                else
                    consoleLog += "\n" + message.ToUpper();
            }

            //Defensively limites the log up to 10KB
            if (consoleLog.Length > 10240)
                consoleLog = consoleLog[..10240];

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Writes a message to the console and stores it in memory for later recovery
        /// </summary>
        /// <returns>The console log stored so far</returns>
        public static void ConsoleWrite(string message = "", params object[] parms)
        {
            message = InterpolateMessage(message, parms);
            Console.Write(message);

            if (htmlResult)
                consoleLog += message.Replace("\n", "<br/>");
            else
                consoleLog += message;

            //Defensively limites the log up to 10KB
            if (consoleLog.Length > 10240)
                consoleLog = consoleLog[..10240];
        }

        /// <summary>
        /// Writes a (hightlighted as error) message to the console and stores it in memory for later recovery
        /// </summary>
        /// <returns>The console log stored so far</returns>
        public static void ConsoleWriteError(string message = "", params object[] parms)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.Yellow;

            message = InterpolateMessage(message, parms);
            Console.Write(message);

            if (htmlResult)
                consoleLog += $"<span style=\"background-color:darkred;color:yellow\">{message.Replace("\n", "<br/>")}</span>";
            else
                consoleLog += $"*** {message.Replace("\n", "\n***")} ***";

            //Defensively limites the log up to 10KB
            if (consoleLog.Length > 10240)
                consoleLog = consoleLog[..10240];

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Writes a (hightlighted) message to the console and stores it in memory for later recovery
        /// </summary>
        /// <returns>The console log stored so far</returns>
        public static void ConsoleWriteHighlighted(string message = "", params object[] parms)
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;

            message = InterpolateMessage(message, parms);
            Console.Write(message);

            if (htmlResult)
                consoleLog += $"<span style=\"background-color:gray;color:black\">{message.Replace("\n", "<br/>")}</span>";
            else
                consoleLog += message.ToUpper();

            //Defensively limites the log up to 10KB
            if (consoleLog.Length > 10240)
                consoleLog = consoleLog[..10240];

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void AddToCache(WorkBenchServiceType singletonType, IWorkBenchService singleton)
        {
            if (singletonCache.ContainsKey(singletonType))
                throw new ArgumentException($"The SingletonType '{singletonType}' has been already set. Only one Workbench service of a given type is allowed.", nameof(singletonType));

            singletonCache.Add(singletonType, singleton);
        }

        public static void UseMediaStorage<T>() where T : ILightMediaStorage, new()
        {
            AddToCache(WorkBenchServiceType.MediaStorage, new T());
        }

        public static void UseRepository<T>() where T : ILightRepository, new()
        {
            AddToCache(WorkBenchServiceType.Repository, new T());
        }

        public static void UseWorker<T>() where T : ILightWorker, new()
        {
            AddToCache(WorkBenchServiceType.Worker, new T());
        }

        public static void UseScheduler<T>() where T : ILightWorker, new()
        {
            AddToCache(WorkBenchServiceType.Scheduler, new T());
        }

        public static void UseReactiveHub<T>() where T : ILightReactiveHub, new()
        {
            AddToCache(WorkBenchServiceType.ReactiveHub, new T());
        }

        public static void UseTelemetry<T>() where T : ILightTelemetry, new()
        {
            AddToCache(WorkBenchServiceType.Telemetry, new T());
        }

        public static void UseDataHub<T>(string tagConfigName = "ANALYTICAL", string endpointName = "data/ingestion") where T : IMessageBrokerWrapper, new()
        {
            var dataHub = new T();
            dataHub.Config(tagConfigName, endpointName);
            AddToCache(WorkBenchServiceType.DataHub, dataHub);
        }

        public static void UseCache<T>() where T : ILightCache, new()
        {
            AddToCache(WorkBenchServiceType.Cache, new T());
        }

        public static void UseLogger<T>() where T : ILightLogger, new()
        {
            AddToCache(WorkBenchServiceType.Logger, new T());
        }

        public static bool ServiceIsRegistered(WorkBenchServiceType serviceType)
        {
            return GetRegisteredService(serviceType) is not null;
        }

        public static object GetRegisteredService(WorkBenchServiceType serviceType)
        {
            if (!_isServicesUp)
            {
                // Trigger initializations over Workbench Services on demand
                InitializeServices();
            }

            singletonCache.TryGetValue(serviceType, out IWorkBenchService IWorkBenchService);

            return IWorkBenchService ?? null;
        }

        internal static T GetService<T>(WorkBenchServiceType singletonType, bool mandatoryParam = false)
        {
            if (!singletonCache.TryGetValue(singletonType, out IWorkBenchService service))
            {
                if (mandatoryParam)
                    throw new ArgumentException($"No Workbench service of type '{singletonType}' was injected on Startup.");
            }

            return (T)service;
        }

        private static void InitializeServices()
        {
            // Foreach service registered on WorkBench cache, the Initialize method should be called to apply specific configurations
            foreach (WorkBenchServiceType serviceType in singletonCache.Keys)
            {
                singletonCache.TryGetValue(serviceType, out IWorkBenchService IWorkBenchService);
                IWorkBenchService.Initialize();
            }

            //prevent from discoveries to run twice or more
            _isServicesUp = true;
        }

        /// <summary>
        /// Prepare Workbench to start Unit Test
        /// </summary>
        /// <param name="settingsFileName">Name of settings file in Unit Test Project in current directory</param>
        /// <param name="environmentName">The environment name the tests will be run on</param>
        public static void PrepareUnitTestMode(string settingsFileName = "appsettings", string environmentName = "")
        {
            if (!string.IsNullOrEmpty(environmentName))
            {
                settingsFileName += $".{environmentName}";
            }
            settingsFileName += ".json";
            var config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile(settingsFileName)
               .Build();

            Configuration = config;
        }

        /// <summary>
        /// Initializes cartridges before run test
        /// </summary>
        public static void RunUnitTestMode()
        {
            InitializeServices();
        }

        public static DateTime UtcNow
        {
            get
            {
                return AdjustableClock.UtcNow;
            }

            set
            {
                AdjustableClock.UtcNow = value;
            }
        }

        public static DateTime Today => AdjustableClock.Today;
        public static DateTime Now => AdjustableClock.Now;

        public static string GenerateNewOperationId() => Guid.NewGuid().ToString();
        public static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        public static bool IsDevelopmentEnvironment => EnvironmentName == "Development";
        public static bool IsIntegrationEnvironment => EnvironmentName == "Integration";
        public static bool IsQualityEnvironment => EnvironmentName == "Quality";
        public static bool IsDemonstrationEnvironment => EnvironmentName == "Demonstration";

        private static readonly bool isProductionEnvironment = EnvironmentName == "Production" || EnvironmentName == "Staging";
        public static bool IsProductionEnvironment => isProductionEnvironment;

        public static bool ForceStubMicroserviceCalls { get; set; }
        public static bool ShouldStubMicroserviceCalls => (IsDevelopmentEnvironment || IsIntegrationEnvironment) && ForceStubMicroserviceCalls;

        private static string InterpolateMessage(string message, object[] parms)
        {
            try
            {
                return string.Format(message, parms);
            }
            catch
            {
                return message;
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}