using Liquid.Base;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Interface message inheritance to use a liquid framework
    /// </summary> 
    public interface ILightMessage
    {
        [JsonIgnore]
        ILightContext TransactionContext { get; set; }
        string CommandType { get; set; }
        string TokenJwt { get; set; }
        string OperationId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        long? ClockDisplacement { get; set; }
        Dictionary<string, object> GetUserProperties();
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}