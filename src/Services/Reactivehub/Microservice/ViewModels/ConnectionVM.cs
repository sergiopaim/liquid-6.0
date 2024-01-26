using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{
    /// <summary>
    /// A user's profile with its editable attributes
    /// </summary>
    public class ConnectionVM : LightViewModel<ConnectionVM>
    {
        /// <summary>
        /// User's id
        /// </summary>
        public string UserId { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
  
}
