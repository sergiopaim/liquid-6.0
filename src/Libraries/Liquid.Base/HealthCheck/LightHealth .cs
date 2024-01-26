using Liquid.Interfaces;
using System;
using System.Collections.Generic;

namespace Liquid.Base
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// LightCheck is reponsable to run health check methods into AMAW cartridges
    /// </summary>
    public static class LightHealth
    {
        public static Dictionary<string, HealthCheckStatus> CartridgesStatus { get; private set; }

        /// <summary>
        /// Enum used as return status for cartridges health check
        /// </summary>
        public enum HealthCheckStatus { Healthy, Unhealthy };        
        
        /// <summary>
        /// Start getting active cartridges at workbench
        /// </summary>
        /// <returns></returns>
        public static void CheckHealth(LightHealthResult lightHealthResult)
        {
            CheckActiveServices(lightHealthResult);
        }
        
        /// <summary>
        /// Method that calls the Cartridge Health Check method.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HealthCheckStatus CheckUp(WorkBenchServiceType serviceType, string value)
        {
            IWorkBenchHealthCheck workBenchHealCheck = GetService<IWorkBenchHealthCheck>(serviceType);
            string serviceKey = serviceType.ToString();
            var checkup = workBenchHealCheck.HealthCheck(serviceKey, value);
            return checkup;
        }

        /// <summary>
        /// Check active services, calls the HealthCheck for each active cartridges and return the Dictionary for response
        /// </summary>
        /// <returns></returns>
        private static void CheckActiveServices(LightHealthResult lightHealthResult)
        {
            foreach (var keys in WorkBench.SingletonCache.Keys)
            {
                LightHealthCartridgeResult cartridgeResult = new()
                {
                    Name = keys.ToString(),
                    Status = CheckUp(keys, WorkBench.SingletonCache[keys].ToString()).ToString()
                };
                lightHealthResult.CartridgesStatus.Add(cartridgeResult);                
            }
        }
        
        /// <summary>
        /// Get active cartridges from exposed _singletonCache property from Workbench
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="singletonType"></param>
        /// <param name="mandatoryParam"></param>
        /// <returns></returns>
        internal static T GetService<T>(WorkBenchServiceType singletonType, Boolean mandatoryParam = true)
        {
            if (!WorkBench.SingletonCache.TryGetValue(singletonType, out IWorkBenchService service))
            {
                if (mandatoryParam)
                    throw new ArgumentException($"No Workbench service of type '{singletonType}' was injected on Startup.");
            }

            return (T)service;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}