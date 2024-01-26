using FluentValidation;
using Liquid.Runtime;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MediaStorageConfiguration : LightConfig<MediaStorageConfiguration>
    {
        public string ConnectionString { get; set; }
        public string Container { get; set; }
        public string Permission { get; set; }

        public override void Validate()
        {
            RuleFor(d => ConnectionString).NotEmpty().WithError("'ConnectionString' on MediaStorage settings should not be empty.");

            RuleFor(d => Container).NotEmpty().WithError("'Container' on MediaStorage settings should not be empty.");

        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}