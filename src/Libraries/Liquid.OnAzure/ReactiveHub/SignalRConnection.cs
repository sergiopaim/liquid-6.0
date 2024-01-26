using Liquid.Activation;
using Liquid.Runtime;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Utilities;
using System;
using System.Threading.Tasks;

namespace Liquid.OnAzure
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Connectiong to the ReactiveHub
    /// </summary>
    public class SignalRConnection : LightHubConnection
    {
        public SignalRConnection(string hubEndpoint)
        {
            connection = new HubConnectionBuilder()
                .WithUrl($"{SignalRConfiguration.GetReactiveHubHost()}{hubEndpoint}?token={JwtSecurityCustom.Config.SysAdminJWT}")
                .WithAutomaticReconnect( new TimeSpan[] { new TimeSpan(5000),
                                                          new TimeSpan(20000),
                                                          new TimeSpan(20000),
                                                          new TimeSpan(30000),
                                                          new TimeSpan(30000),
                                                          new TimeSpan(50000),
                                                          new TimeSpan(50000),
                                                          new TimeSpan(100000),
                                                          new TimeSpan(100000),
                                                          new TimeSpan(150000),
                                                          new TimeSpan(150000),
                                                          new TimeSpan(200000),
                                                          new TimeSpan(200000),
                                                          new TimeSpan(300000),
                                                          new TimeSpan(300000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000),
                                                          new TimeSpan(600000) })
                .ConfigureLogging(logging =>
                {
                    if(SignalRConfiguration.GetDebugLog())
                    { 
                        // Log to the Console
                        logging.AddConsole();

                        // This will set ALL logging to Debug level
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                }).Build();

            Task.Delay(2000); // wait server setup
            connection.StartAsync();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
