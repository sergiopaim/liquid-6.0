using Liquid.Activation;

namespace Liquid.OnAzure
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Cartridge to use SignalR as the reactive hub
    /// </summary>
    public class SignalRHub : LightReactiveHub
    {
        public override void Initialize()
        {
            Connection = new SignalRConnection(GetHubEndpoint());
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
