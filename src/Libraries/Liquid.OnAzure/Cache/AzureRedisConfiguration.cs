using FluentValidation;
using Liquid.Runtime;

namespace Liquid.OnAzure
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AzureRedisConfiguration : LightConfig<AzureRedisConfiguration>
    {
        public string Configuration { get; set; }
        public string InstanceName { get; set; }
        public int SlidingExpirationSeconds { get; set; }
        public int AbsoluteExpirationRelativeToNowSeconds { get; set; }
        public override void Validate()
        {
            RuleFor(b => b.Configuration).NotEmpty().WithError("configuration must not be empty");
            RuleFor(b => b.InstanceName).NotEmpty().WithError("instanceNameE must not be empty");
            RuleFor(b => b.SlidingExpirationSeconds).NotEmpty().WithError("slidingExpirationSeconds must not be empty");
            RuleFor(b => b.AbsoluteExpirationRelativeToNowSeconds).NotEmpty().WithError("AbsoluteExpirationRelativeToNowSeconds must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
