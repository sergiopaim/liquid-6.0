using System.Collections.Generic;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Public interface for pagination features on database queries
    /// </summary>
    public interface ILightPaging<T>
    {
        ICollection<T> Data { get; set; }
        int ItemsPerPage { get; set; }
        string ContinuationToken { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}