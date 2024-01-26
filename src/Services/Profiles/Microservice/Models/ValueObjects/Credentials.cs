using IdentityModel;
using Liquid;
using Liquid.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class Credentials : LightValueObject<Credentials>
    {
        private static readonly Random random = new();

        const int DefaultOTPExpiration = 120;  // in minutes

        public string OTP { get; set; }
        public DateTime OTPExpiresAt { get; set; }
        public string Secret { get; set; }
        public int SecretTries;
        public List<WebAuthN> WebAuthN { get; set; } = new();
        public string WebAuthNChallenge { get; set; }

        public override void Validate() { }

        internal void GenerateNewOTP()
        {
            OTP = Convert.ToBase64String(CryptoRandom.CreateRandomKey(6));
            OTPExpiresAt = WorkBench.UtcNow.AddMinutes(DefaultOTPExpiration);
        }

        internal static string OneWayEncript(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return "invalid";

            var sha = SHA256.Create();
            var encriptedBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));

            StringBuilder encripted = new(encriptedBytes.Length * 2);
            foreach (byte b in encriptedBytes)
                encripted.AppendFormat("{0:x2}", b);

            return encripted.ToString().ToUpper();
        }

        internal static string RandomizeSecret()
        {
            const int lenght = 14;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            return new(Enumerable.Repeat(chars, lenght).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

