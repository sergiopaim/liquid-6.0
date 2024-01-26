using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Collections.Generic;

namespace Microservice.ViewModels
{
    /// <summary>
    /// An account record with all of its attributes
    /// </summary>
    public class AccountVM : LightViewModel<AccountVM>
    {
        /// <summary>
        /// Id of the user account
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// List of roles the user has from the account
        /// </summary>
        public List<string> Roles { get; set; }

        /// <summary>
        /// The source of user account
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// </summary>
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
    }
}
