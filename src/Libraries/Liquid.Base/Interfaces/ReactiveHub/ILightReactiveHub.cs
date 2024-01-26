namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Inteface to LightReactiveHub
    /// </summary>
    public interface ILightReactiveHub : IWorkBenchService
    {
        string GetHubEndpoint();
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
