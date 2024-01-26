using Liquid.Domain;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class ChannelType : LightLocalizedEnum<ChannelType>
    {
        public static readonly ChannelType Email = new(nameof(Email));
        public static readonly ChannelType Phone = new(nameof(Phone));
        public static readonly ChannelType App = new(nameof(App));

        public ChannelType(string code) : base(code) { }

    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
