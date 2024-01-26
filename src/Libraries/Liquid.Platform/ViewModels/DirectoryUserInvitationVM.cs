using Liquid.Domain;

namespace Liquid.Platform
{
    /// <summary>
    /// An invitation to an external person to become a directory guest user
    /// </summary>
    public class DirectoryUserInvitationVM : LightViewModel<DirectoryUserInvitationVM>
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
        /// <summary>
        /// The invite redeem URL 
        /// </summary>
        public string InviteRedeemUrl { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
