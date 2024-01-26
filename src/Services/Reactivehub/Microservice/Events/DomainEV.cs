using Liquid.Activation;

namespace Microservice.Events
{
    /// <summary>
    /// Reactive event to notify application logic of domain events
    /// </summary>
    public class DomainEV : LightReactiveEvent<DomainEV>
    {
        /// <summary>
        /// User´s name 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Generic payload object
        /// </summary>
        public object Payload { get; set; }
        /// <summary>
        /// Short message for user notification
        /// </summary>
        public string ShortMessage { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate() { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}