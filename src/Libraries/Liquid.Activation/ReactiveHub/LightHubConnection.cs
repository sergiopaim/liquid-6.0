using Liquid.Base;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA2211 // Non-constant fields should not be visible
    /// <summary>
    /// Connectiong to the ReactiveHub
    /// </summary>
    public abstract class LightHubConnection
    {
        protected static HubConnection connection;

        public static async Task<bool> InvokeAsync(string methodName, params object[] args)
        {
            if (connection.State != HubConnectionState.Connected)
            {
                string warning = $"HubConnection is '{connection.State}'. Trying to reconnect. This should not happen during message invoking. Investigate further.";
                WorkBench.Telemetry.TrackException(new LightException(warning));
                await connection.StartAsync();

                if (connection.State != HubConnectionState.Connected)
                {
                    string error = $"Could not send message through ReactiveHub because the connection is still in state '{connection.State}' while calling SendCoreAsync with '{methodName}' and id '{args[0]}'";
                    WorkBench.Telemetry.TrackException(new LightException(error));
                    Console.WriteLine(error);
                    return true;  // could not presume the operation is invalid
                }
            }

            bool success = true;

            try
            {
                await connection.SendCoreAsync(methodName, args);
            }
            catch (InvalidOperationException)
            {
                success = false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"General exception during ReactiveHub connection ('{connection.State}') while calling SendCoreAsync with '{methodName}' and id '{args[0]}'");
                Console.WriteLine(e);
                WorkBench.Telemetry.TrackException(e);
            }

            return success;
        }
    }
#pragma warning restore CA2211 // Non-constant fields should not be visible
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
