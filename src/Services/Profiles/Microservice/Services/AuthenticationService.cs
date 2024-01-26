using Liquid;
using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Runtime;
using Microservice.Configuration;
using Microservice.Models;
using Microservice.ViewModels;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Microservice.Services
{
    internal class AuthenticationService : LightService
    {
        private static readonly int MAX_SECRET_ATTEMPTS = 5;

        private static readonly AuthenticationConfig config = LightConfigurator.LoadConfig<AuthenticationConfig>("Authentication");
        public static X509Certificate2 privateKey = new(config.JWTP12CertificateLocation, config.JWTCertificatePassword);

        public static readonly TokenValidationParameters validationParameters = new()
                                                          {
                                                              ValidateIssuerSigningKey = true,
                                                              ValidateAudience = true,
                                                              ValidateIssuer = true,
                                                              ValidateLifetime = false,
                                                              IssuerSigningKey = new X509SecurityKey(privateKey),
                                                              ValidAudience = config.JWTSelfIssuedAudience,
                                                              ValidIssuer = config.JWTSelfIssuer
                                                          };

        public static readonly JwtSecurityTokenHandler tokenHandler = new();
        
        internal DomainResponse RequestLogin(string id, string connectionId)
        {
            Telemetry.TrackEvent("Request Login", $"id: {id} connectionId: {connectionId}");

            var profile = Repository.Get<Profile>(p => p.Accounts[0].Id == id)
                                    .AsEnumerable()
                                    .FirstOrDefault();

            if (profile is null || profile.Accounts.First().Source == AccountSource.AAD.Code)
            {
                return NoContent();
            }
            else
            {
                DomainEventMSG login = FactoryLightMessage<DomainEventMSG>(DomainEventCMD.Notify);
                login.Name = "requestLogin";
                login.ShortMessage = LightLocalizer.Localize("REQUEST_LOGIN_SHORT_MESSAGE");
                login.UserIds.Add(id);
                login.PushIfOffLine = true;

                JsonNode json = login.Payload.ToJsonNode();
                json.AsObject().Add("connectionId", JsonNode.Parse($"\"{connectionId}\""));
                login.Payload = json.ToJsonDocument();

                PlatformServices.SendDomainEvent(login);
            }

            return Response();
        }

        internal async Task<DomainResponse> AllowLogin(string id, string connectionId, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Allow Login", $"id: {id} connectionId: {connectionId} tryNum:{tryNum}");

            var profile = Repository.Get<Profile>(p => p.Accounts[0].Id == id)
                                    .AsEnumerable()
                                    .FirstOrDefault();

            if (profile is null || profile.Accounts.First().Source == AccountSource.AAD.Code)
            {
                return NoContent();
            }
            else
            {
                try
                {
                    profile.Accounts.First().Credentials.GenerateNewOTP();

                    await Repository.UpdateAsync(profile);

                    DomainEventMSG allowLogin = FactoryLightMessage<DomainEventMSG>(DomainEventCMD.Notify);
                    allowLogin.Name = "allowLogin";
                    allowLogin.ShortMessage = "Login allowed (message not to be shown as push)";
                    allowLogin.AnonConns.Add(connectionId);

                    JsonNode editablePayload = allowLogin.Payload.ToJsonNode();
                    editablePayload.AsObject().Add("otp", JsonNode.Parse($"\"{profile.Accounts.First().Credentials.OTP}\""));
                    allowLogin.Payload = editablePayload.ToJsonDocument();

                    PlatformServices.SendDomainEvent(allowLogin);
                }
                catch (OptimisticConcurrencyLightException)
                {
                    if (tryNum <= 3)
                        return await AllowLogin(id, connectionId, ++tryNum);
                    else
                        throw;
                }
            }

            return Response();
        }

        internal async Task<DomainResponse> SendAuthLink(string accountId, string channelType, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Resend Authentication Link", $"accountId: {accountId} channelType: {channelType} tryNum:{tryNum}");

            var toUpdate = await Repository.GetByIdAsync<Profile>(accountId);

            if (toUpdate is null)
                return NoContent();

            var account = toUpdate.Accounts.First(a => a.Id == accountId);

            if (account is null)
                return BusinessError("USER_HAS_NO_ACCOUNT");

            account.Credentials.GenerateNewOTP();

            try
            {
                var updated = await Repository.UpdateAsync(toUpdate);

                if (channelType == ChannelType.Email.Code)
                    SendAuthLinkByEmail(account.Id, account.Credentials.OTP);
                else
                    SendAuthLinkByText(account.Id, account.Credentials.OTP);

                AddBusinessInfo("AUTHENTICATION_LINK_SENT_SUCCESSFULLY");
            }
            catch (OptimisticConcurrencyLightException)
            {
                if (tryNum <= 3)
                    return await SendAuthLink(accountId, channelType, ++tryNum);
                else
                    throw;
            }
            return Response();
        }

        public async Task<DomainResponse> RefreshJWTAsync(string oldToken)
        {
            if (!ValidatePrescribedJWT(oldToken, out SecurityToken validatedToken))
                return BusinessError("TOKEN_INVALID");

            var accountId = ((JwtSecurityToken)validatedToken).Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub).Value;

            Telemetry.TrackEvent("Refresh Token", accountId);

            var profile = Repository.Get<Profile>(p => p.Accounts[0].Id == accountId).AsEnumerable().FirstOrDefault();

            if (profile is null)
                return NoContent();

            profile = await Service<ProfileService>().UpdateRolesForAADUserAsync(profile, 
                                                                                 AADService.CheckRolesToUpdate(profile));

            return Response(new TokenVM
                                {
                                    IssuedTo = accountId,
                                    Token = GenerateNewJWT(accountId, profile)
                                });
        }

        private async Task ValidateOTP(string accountId, Profile profile, string otp, string channelType, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Validate OTP", $"accountId: {accountId} channelType: {channelType} tryNum:{tryNum}");

            var before = profile.CloneComparable();

            var account = profile.Accounts.First(a => a.Id == accountId);
            if (account is null)
            {
                AddBusinessError("USER_HAS_NO_ACCOUNT");
                return;
            }

            if (account.Credentials?.OTP != otp)
            {
                AddBusinessError("OTP_INVALID");
                return;
            }

            if (WorkBench.UtcNow > account.Credentials.OTPExpiresAt)
            {
                AddBusinessError("OTP_EXPIRED");
                return;
            }

            profile.Channels.MarkAsValid(channelType);

            account.Credentials.OTPExpiresAt = WorkBench.UtcNow;

            try
            {
                var updated = await Repository.UpdateAsync(profile);
                await Service<ProfileService>().NotifySubscribersOfChangesBetween(before, updated);
            }
            catch (OptimisticConcurrencyLightException)
            {
                if (tryNum <= 3)
                {
                    await ValidateOTP(accountId, profile, otp, channelType, ++tryNum);
                    return;
                }
                else
                    throw;
            }

            return;
        }

        public async Task<DomainResponse> RequestNewOTP(string accountId, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Request New OTP", $"accountId: {accountId} tryNum:{tryNum}");

            var profile = Repository.Get<Profile>(p => p.Accounts[0].Id == accountId)
                                    .AsEnumerable()
                                    .FirstOrDefault();

            if (profile is null)
                return NoContent();

            var account = profile.Accounts.First(a => a.Id == accountId);

            if (account is null)
                return BusinessError("USER_HAS_NO_ACCOUNT");

            if (!account.Roles.Contains(AccountRole.Member.Code))
                return Forbidden();

            account.Credentials.GenerateNewOTP();

            Profile updated;
            try
            {
                updated = await Repository.UpdateAsync(profile);
            }
            catch (OptimisticConcurrencyLightException)
            {
                if (tryNum <= 3)
                    return await RequestNewOTP(accountId, ++tryNum);
                else
                    throw;
            }

            return Response(updated.FactoryWithOTPVM());
        }

        public async Task<DomainResponse> AuthenticateByOTP(string accountId, string otp, string channelType)
        {
            Telemetry.TrackEvent("Authenticate by OTP", accountId);

            var profile = Repository.Get<Profile>(p => p.Accounts.Where(a => a.Id == accountId).ToList()[0] != null)
                                      .AsEnumerable().FirstOrDefault();

            if (profile is null)
                return NoContent();

            await ValidateOTP(accountId, profile, otp, channelType);

            if (HasBusinessErrors)
                return Response();

            return Response(new TokenVM
                                {
                                    IssuedTo = accountId,
                                    Token = GenerateNewJWT(accountId, profile)
                                });
        }

        public async Task<DomainResponse> AuthenticateBySecret(string accountId, string secret, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Authenticate by Secret", $"accountId: {accountId} tryNum:{tryNum}");

            var profilesQueried = Repository.Get<Profile>(p => p.Accounts[0].Id == accountId);
            var profile = profilesQueried.AsEnumerable().FirstOrDefault();

            if (profile is null)
                return Unauthorized("USER_NOT_FOUND");

            var account = profile.Accounts.FirstOrDefault(a => a.Id == accountId);

            try
            {
                if (!account.Roles.Contains(AccountRole.ServiceAccount.Code))
                    return Forbidden();

                if (account.Credentials.SecretTries >= MAX_SECRET_ATTEMPTS)
                    return BusinessError("ACCOUNT_IS_DISABLED_BY_INVALID_ATTEMPTS");

                if (Credentials.OneWayEncript(secret) != account.Credentials.Secret)
                {
                    if (++account.Credentials.SecretTries < MAX_SECRET_ATTEMPTS)
                        BusinessError("INVALID_SECRET", account.Credentials.SecretTries, MAX_SECRET_ATTEMPTS);
                    else
                        BusinessError("ACCOUNT_WAS_DISABLED_BY_INVALID_ATTEMPTS", MAX_SECRET_ATTEMPTS);

                    await Repository.UpdateAsync(profile);
                    return Response();
                }

                if (account.Credentials.SecretTries > 0)
                {
                    account.Credentials.SecretTries = 0;
                    await Repository.UpdateAsync(profile);
                }

                return Response(new TokenVM
                                    {
                                        IssuedTo = accountId,
                                        Token = GenerateNewJWT(accountId, profile)
                                    });
            }
            catch (OptimisticConcurrencyLightException)
            {
                if (tryNum <= 3)
                    return await AuthenticateBySecret(accountId, secret, ++tryNum);
                else
                    throw;
            }
        }

        public async Task<DomainResponse> AuthenticateByIdToken(string idToken, int? tryNum = 1)
        {
            ClaimsPrincipal userClaims = await Service<AADService>().GetClaimsFromIdTokenAsync(idToken);
            if (userClaims is null)
            {
                Telemetry.TrackEvent("Authenticate by Id_Token", $"accountId: null tryNum:{tryNum}");
                return Response();
            }

            var accountId = userClaims.FindFirstValue(JwtClaimTypes.UserId);
            Telemetry.TrackEvent("Authenticate by Id_Token", $"accountId: {accountId} tryNum:{tryNum}");

            var updating = Repository.Get<Profile>(p => p.Accounts[0].Id == accountId).FirstOrDefault();

            if (updating is null)
                updating = await Service<ProfileService>().CreateProfileForAADClaimsAsync(userClaims);
            else
            {
                var before = updating.CloneComparable();
                updating.UpdateAADAccount(userClaims);
                try
                {
                    updating = await Repository.UpdateAsync(updating);
                    await Service<ProfileService>().NotifySubscribersOfChangesBetween(before, updating);
                }
                catch (OptimisticConcurrencyLightException)
                {
                    if (tryNum <= 3)
                        return await AuthenticateByIdToken(idToken, ++tryNum);
                    else
                        throw;
                }
            }

            if (HasBusinessErrors)
                return Response();

            return Response(new TokenVM
                                {
                                    IssuedTo = accountId,
                                    Token = GenerateNewJWT(accountId, updating)
                                });
        }

        public DomainResponse GetChannelTypes()
        {
            Telemetry.TrackEvent("Get Channel Types");

            return Response(ChannelType.GetAll());
        }

        private static bool ValidatePrescribedJWT(string jwt, out SecurityToken validatedToken)
        {
            try
            {
                tokenHandler.ValidateToken(jwt, validationParameters, out validatedToken);
                return true;
            }
            catch (Exception e1)
            {
                if (e1.Message.Contains("The associated certificate has expired"))
                {
                    TokenValidationParameters relaxedValidationParameters = new()
                                                                            {
                                                                                ValidateIssuerSigningKey = false,
                                                                                ValidateAudience = true,
                                                                                ValidateIssuer = true,
                                                                                ValidateLifetime = false,
                                                                                IssuerSigningKey = new X509SecurityKey(privateKey),
                                                                                ValidAudience = config.JWTSelfIssuedAudience,
                                                                                ValidIssuer = config.JWTSelfIssuer
                                                                            };

                    try
                    {
                        WorkBench.Telemetry.TrackException(new LightException("JWT Authenticator Certificate has expired", e1));

                        tokenHandler.ValidateToken(jwt, relaxedValidationParameters, out validatedToken);
                        return true;
                    }
                    catch { }
                }

                X509Certificate2 oldPrivateKey = new(config.JWTP12CertificateLocationOld, config.JWTCertificatePasswordOld);
                TokenValidationParameters oldValidationParameters = new()
                                                                    {
                                                                        ValidateIssuerSigningKey = true,
                                                                        ValidateAudience = true,
                                                                        ValidateIssuer = true,
                                                                        ValidateLifetime = false,
                                                                        IssuerSigningKey = new X509SecurityKey(oldPrivateKey),
                                                                        ValidAudience = config.JWTSelfIssuedAudience,
                                                                        ValidIssuer = config.JWTSelfIssuer
                                                                    };

                try
                {
                    tokenHandler.ValidateToken(jwt, oldValidationParameters, out validatedToken);
                    return true;
                }
                catch (Exception e2)
                {
                    if (e2.Message.Contains("The associated certificate has expired"))
                    {
                        oldValidationParameters.ValidateIssuerSigningKey = false;

                        try
                        {
                            tokenHandler.ValidateToken(jwt, oldValidationParameters, out validatedToken);
                            return true;
                        }
                        catch { }
                    }

                    WorkBench.Telemetry.TrackTrace($"Invalid token: \n Current config: {e1.Message}, \n Former config: {e2.Message}");
                }
            }

            validatedToken = default;
            return false;
        }

        private static string GenerateNewJWT(string accountId, Profile profile)
        {
            var givenName = profile.Name.Split()[0];
            var surname = string.Join(' ', profile.Name.Split().Skip(1));

            var credentials = new SigningCredentials(new X509SecurityKey(privateKey), SecurityAlgorithms.RsaSha256);

            List<Claim> userClaims = new()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("sub", accountId),
                new Claim("GivenName", givenName),
                new Claim("Surname", surname),
                new Claim("Email", profile.Channels.Email ?? ""),
                new Claim("CellPhone", profile.Channels.Phone ?? "")
            };

            foreach (var role in profile.Accounts.First().Roles)
            {
                userClaims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, role));
            }

            var token = new JwtSecurityToken(issuer: config.JWTSelfIssuer,
                                             audience: config.JWTSelfIssuedAudience,
                                             claims: userClaims,
                                             notBefore: WorkBench.UtcNow,
                                             expires: profile.GetTokenExpiration(),
                                             signingCredentials: credentials);                                            

            //This method only works in Linux, and it required significant effort to figure it out 🙄
            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        private void SendAuthLinkByEmail(string profileId, string newOtp)
        {
            Telemetry.TrackEvent("Send AuthLink By Email", profileId);

            var activationPayload = System.Text.Encoding.UTF8.GetBytes(new
            {
                channel = ChannelType.Email.Code,
                otp = newOtp,
                accountId = profileId
            }.ToJsonString());

            var activationLink = "{MemberAppURL}" + $"/login/allow?otpToken={Convert.ToBase64String(activationPayload)}";

            var emailMSG = FactoryLightMessage<EmailMSG>(EmailCMD.Send);
            emailMSG.UserId = profileId;
            emailMSG.Type = NotificationType.Account.Code;
            emailMSG.Subject = LightLocalizer.Localize("AUTHENTICATION_LINK_EMAIL_SUBJECT");
            emailMSG.Message = LightLocalizer.Localize("AUTHENTICATION_LINK_EMAIL_MESSAGE", activationLink);

            PlatformServices.SendEmail(emailMSG);
        }

        private void SendAuthLinkByText(string profileId, string newOtp)
        {
            Telemetry.TrackEvent("Send AuthLink By Text", profileId);

            var activationPayload = System.Text.Encoding.UTF8.GetBytes(new
            {
                channel = ChannelType.Phone.Code,
                otp = newOtp,
                accountId = profileId
            }.ToJsonString());

            var activationLink = "{MemberAppURL}" + $"/login/allow?otpToken={Convert.ToBase64String(activationPayload)}";

            var shortTextMSG = FactoryLightMessage<ShortTextMSG>(ShortTextCMD.Send);
            shortTextMSG.UserId = profileId;
            shortTextMSG.Type = NotificationType.Account.Code;
            shortTextMSG.Message = LightLocalizer.Localize("AUTHENTICATION_LINK_TEXT_MESSAGE", activationLink);

            PlatformServices.SendText(shortTextMSG);
        }
    }
}
