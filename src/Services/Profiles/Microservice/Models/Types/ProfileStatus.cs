using Liquid.Domain;

namespace Microservice.ViewModels
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ProfileStatus : LightEnum<ProfileStatus>
    {
        public static readonly ProfileStatus Active = new(nameof(Active));
        public static readonly ProfileStatus Inactive = new(nameof(Inactive));
        public ProfileStatus(string code) : base(code) { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

}
