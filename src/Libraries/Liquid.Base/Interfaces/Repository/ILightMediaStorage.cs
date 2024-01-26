using System.IO;
using System.Threading.Tasks;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Public interface for all Media Storage Implementations 
    /// </summary>
    public interface ILightMediaStorage : IWorkBenchHealthCheck
    {
        string Connection { get; set; }
        string Container { get; set; }

        Task<ILightAttachment> GetAsync(string resourceId, string id);
        Task<Stream> GetBlockAsync(string resourceId, string id, int blockNumber);
        Task InsertUpdateAsync(ILightAttachment attachment); 
        Task ReplaceAsync(ILightAttachment attachment); 
        Task AppendBlockAsync(string resourceId, string id, Stream block, int blockNumber);
        Task Remove(ILightAttachment attachment);
        Task RemoveDirectoryAsync(string dirName);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
