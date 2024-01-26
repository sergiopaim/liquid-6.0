using FluentValidation;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class SwaggerGenerationConfig : LightConfig<SwaggerGenerationConfig>
    {
        public string AzureStorageConnStr { get; set; }
        public override void Validate()
        {
            RuleFor(d => AzureStorageConnStr).NotEmpty().WithError("AzureStorageConnStr settings should not be empty.");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}