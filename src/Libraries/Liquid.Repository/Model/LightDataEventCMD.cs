using Liquid.Domain;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class LightDataEventCMD : LightEnum<LightDataEventCMD>
    {
        public static readonly LightDataEventCMD Insert = new(nameof(Insert));
        public static readonly LightDataEventCMD Update = new(nameof(Update));
        public static readonly LightDataEventCMD Delete = new(nameof(Delete));

        public LightDataEventCMD(string code) : base(code) { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
