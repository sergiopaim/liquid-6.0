namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Base inteface for (business) domain classes
    /// </summary>
    public interface ILightDomain
    {
        ICriticHandler CritictHandler { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
