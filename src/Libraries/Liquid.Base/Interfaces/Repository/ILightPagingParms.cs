namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Public interface for pagination parameters on database queries
    /// </summary>
    public interface ILightPagingParms
    {
        int ItemsPerPage { get; set; }
        string ContinuationToken { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}