using System.Threading.Tasks;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ILightJobMessage
    {
        string CommandType { get; set; }
        string Microservice { get; set; }
        string Job { get; set; }
        string OperationId { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
