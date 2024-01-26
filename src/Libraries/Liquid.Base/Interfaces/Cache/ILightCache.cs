using System.Threading.Tasks;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Cache interface for Microservice
    /// </summary>
    public interface ILightCache : IWorkBenchHealthCheck
    {
        T Get<T>(string key);
        Task<T> GetAsync<T>(string key);

        void Set<T>(string key, T value);
        Task SetAsync<T>(string key, T value);

        void Refresh(string key);
        Task RefreshAsync(string key);

        void Remove(string key);
        Task RemoveAsync(string key);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
