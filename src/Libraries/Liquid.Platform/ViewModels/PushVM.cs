using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Liquid.Platform
{
    /// <summary>
    /// A user's profile with its editable attributes
    /// </summary>
    public class PushVM : LightViewModel<PushVM>
    {
        /// <summary>
        /// User's id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Short message (up to 140 chars) to be promptly shown to the user
        /// </summary>
        public string ShortMessage { get; set; }
        /// <summary>
        /// Relative URI to open the event/do the action
        /// </summary>
        public string ContextUri { get; set; }

        /// <summary>
        /// </summary>
        public override void Validate()
        {
            RuleFor(i => i.UserId).NotEmpty().WithError("id must not be empty");
            RuleFor(i => i.ShortMessage).NotEmpty().WithError("shortMessage must not be empty");
            RuleFor(i => i.ShortMessage).MaximumLength(140).WithError("shortMessage must be up to 140 chars");
        }
    }
}
