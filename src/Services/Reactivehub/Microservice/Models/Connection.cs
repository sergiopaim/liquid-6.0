using FluentValidation;
using Liquid.Repository;
using Liquid.Runtime;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class Connection : LightModel<Connection>
    {
        [PartitionKey]
        public string UserId { get; set; }

        public override void Validate()
        {
            RuleFor(i => i.Id).NotEmpty().WithError("id must not be empty");
            RuleFor(i => i.UserId).NotEmpty().WithError("userId must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
