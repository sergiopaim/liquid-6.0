using FluentValidation;
using Liquid;
using Liquid.Platform;
using Liquid.Repository;
using Liquid.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Model of an Account
    /// </summary>
    public class Account : LightValueObject<Account>
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public List<string> Roles { get; set; } = new();
        public Credentials Credentials { get; set; } = new();

        public override void Validate() 
        {
            RuleFor(i => i.Source).Must(m => AccountSource.IsValid(m)).WithError("source is invalid");
        }


        internal static Account FactoryFromAADUser(DirectoryUserSummaryVM user, List<string> roles)
        {
            return new()
            {
                Source = AccountSource.AAD.Code,
                Id = user.Id,
                Roles = roles
            };
        }

        internal static Account FactoryFromAADClaims(ClaimsPrincipal userClaims)
        {
            Account factored = new();

            if (IssuedByAAD(userClaims))
                factored.Source = AccountSource.AAD.Code;
            else
                factored.Source = AccountSource.IM.Code;

            factored.Id = userClaims.FindFirstValue(JwtClaimTypes.UserId);
            factored.Roles = userClaims.FindAll(ClaimsIdentity.DefaultRoleClaimType)
                                       .Select(x => x.Value)
                                       .ToList();

            //TODO Remove telemetry
            WorkBench.Telemetry.TrackEvent("Profile account factored", $"id: {factored.Id}, source: {factored.Source}");
            var claims = $"[ '{string.Join("', '", userClaims.Claims.Select(c => c.ToString()))}']";
            WorkBench.Telemetry.TrackTrace(claims);

            return factored;
        }

        internal static bool IssuedByAAD(ClaimsPrincipal userClaims)
        {
            return userClaims.FindFirstValue("iss")?.ToLower().Contains("microsoftonline") ?? false;
        }
    }

    class AccountComparer : IEqualityComparer<Account>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(Account x, Account y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            //Check whether the products' properties are equal.
            return x.Id == y.Id;
        }

        // If Equals() returns true for a pair of objects 
        // then GetHashCode() must return the same value for these objects.
        public int GetHashCode(Account account)
        {
            //Check whether the object is null
            if (account is null) return 0;

            //Get hash code for the Name field if it is not null.
            int hashId = account.Id is null ? 0 : account.Id.GetHashCode();

            //Calculate the hash code for the product.
            return hashId;
        }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}