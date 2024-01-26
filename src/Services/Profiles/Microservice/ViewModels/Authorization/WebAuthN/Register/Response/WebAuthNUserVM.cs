using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{ 
    /// <summary>
    /// The user information to use in WebauthN dialogs
    /// </summary>
    public class WebAuthNUserVM : LightViewModel<WebAuthNUserVM>
    {
        /// <summary>
        ///  User id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        ///  User name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///  User name to display
        /// </summary>
        public string DisplayName { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}

