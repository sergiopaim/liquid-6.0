using Liquid.Domain;

namespace Liquid.Platform
{
    /// <summary>
    /// A directory user's summary profile 
    /// </summary>
    public class DirectoryUserSummaryVM : LightViewModel<DirectoryUserSummaryVM>
    {
        /// <summary>
        /// User id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// User´s name 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The user's email address
        /// </summary>
        public string Email { get; set; }
        
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
