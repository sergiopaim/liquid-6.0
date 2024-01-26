using FluentValidation;
using IdentityModel;
using Liquid.Repository;
using System;

namespace Microservice.Models
{
    /// <summary>
    /// WebAuthN credential of the user's account
    /// </summary>
    public class WebAuthN : LightValueObject<WebAuthN>
    {
        /// <summary>
        /// Device unique identifyer
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// Id of the credential
        /// </summary>
        public string CredentialId { get; set; }
        /// <summary>
        /// PK used by the credential
        /// </summary>
        public string PublicKey { get; set; }
        /// <summary>
        /// Algorithm used by the credential
        /// </summary>
        public string Algorithm { get; set; }
        /// <summary>
        /// Protect against replay attacks
        /// </summary>
        public uint Counter { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}

