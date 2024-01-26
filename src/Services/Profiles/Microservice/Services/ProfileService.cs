using Liquid;
using Liquid.Base;
using Liquid.Domain;
using Liquid.OnAzure;
using Liquid.Platform;
using Microservice.Models;
using Microservice.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microservice.Services
{
    internal class ProfileService : LightService
    {
        private const string DEFAULT_TIMEZONE = "America/Sao_Paulo";
        private const string DEV_DOMAIN = "@your-dev-domain.onmicrosoft.com";
        private const string PRD_DOMAIN = "@your-domain.com";

        static readonly MessageBus<ServiceBus> userProfileBus = new("TRANSACTIONAL", "user/profiles");

        public async Task<DomainResponse> GetOfCurrentAccountAsync()
        {
            Telemetry.TrackEvent("Get Current Profile", CurrentUserId);

            var profile = await Repository.GetByIdAsync<Profile>(CurrentUserId);

            if (profile is null)
                return NoContent();

            return Response(profile.FactoryVM());
        }

        public async Task<DomainResponse> GetPendingChangesOfCurrentAccountAsync()
        {
            Telemetry.TrackEvent("Get Current Profile With Pending Changes", CurrentUserId);

            var profile = await Repository.GetByIdAsync<Profile>(CurrentUserId);

            if (profile is null)
                return NoContent();

            return Response(profile.FactoryWithPendingChangesVM());
        }

        public async Task<DomainResponse> UpdateOfCurrentAccountAsync(EditProfileVM edit, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Update Current Profile", $"id: {CurrentUserId} tryNum:{tryNum}");

            Account current = Account.FactoryFromAADClaims(Context.User);

            var toUpdate = await Repository.GetByIdAsync<Profile>(current.Id);

            if (toUpdate is null)
                if (current.Source == AccountSource.AAD.Code)
                    toUpdate = await CreateProfileForAADClaimsAsync(Context.User);
                else
                    return NoContent();

            if (current.Source == AccountSource.AAD.Code &&
                toUpdate.Channels.WillChangeFrom(edit))
                return BadRequest("Its not allowed to update channels (phone and email) of AAD users");

            if (toUpdate.Channels.WillRemoveAnyFrom(edit))
                return BadRequest("Its not allowed to remove an already defined channel (phone or email)");

            CheckAlternateKeys(toUpdate.Id,
                               toUpdate.Channels.Email,
                               toUpdate.Channels.Phone,
                               edit.Email,
                               edit.Phone);

            if (HasBusinessErrors)
                return Response();
            else
            {
                var before = toUpdate.CloneComparable();
                toUpdate.MapFromEditVM(edit);

                if (HasBusinessErrors)
                    return Response();
                else
                {
                    Service<ChannelService>().ControlChannelUpdates(before, toUpdate);

                    Profile updated;
                    try
                    {
                        updated = await Repository.UpdateAsync(toUpdate);

                        Service<ChannelService>().RequestValidationOfUpdatedChannels(before, updated);

                        await NotifySubscribersOfChangesBetween(before, updated);
                    }
                    catch (OptimisticConcurrencyLightException)
                    {
                        if (tryNum <= 3)
                            return await UpdateOfCurrentAccountAsync(edit, ++tryNum);
                        else
                            throw;
                    }

                    return Response(updated?.FactoryVM());
                }
            }
        }

        public async Task<DomainResponse> RevertChannelByIdAsync(JsonDocument token, int? tryNum = 1)
        {
            var toUpdate = await Repository.GetByIdAsync<Profile>(token.Property("id").AsString());
            if (toUpdate is null)
            {
                Telemetry.TrackEvent("Revert Profile Channel Update", $"id:null tryNum:{tryNum}");
                return NoContent();
            }

            Telemetry.TrackEvent("Revert Profile Channel Update", $"id:{toUpdate.Id} tryNum:{tryNum}");

            var before = toUpdate.CloneComparable();

            Service<ChannelService>().RevertUpdate(toUpdate, token.Property("otp").AsString());

            CheckAlternateKeys(toUpdate.Id,
                               before.Channels.Email,
                               before.Channels.Phone,
                               toUpdate.Channels.Email,
                               toUpdate.Channels.Phone);

            if (HasBusinessErrors)
                return BusinessWarning("UNABLE_TO_REVERT_UPDATE");
            else
            {
                try
                {
                    var updated = await Repository.UpdateAsync(toUpdate);

                    await NotifySubscribersOfChangesBetween(before, updated);

                    return Response(updated.FactoryVM());
                }
                catch (OptimisticConcurrencyLightException)
                {
                    if (tryNum <= 3)
                        return await RevertChannelByIdAsync(token, ++tryNum);
                    else
                        throw;
                }
            }
        }

        public async Task<DomainResponse> ValidateMyChannelAsync(string channelType, string validationOTP, int? tryNum = 1)
        {
            Telemetry.TrackEvent($"Validate Current Profile Channel {channelType}", $"id: {CurrentUserId} tryNum:{tryNum}");

            var toUpdate = await Repository.GetByIdAsync<Profile>(CurrentUserId);
            if (toUpdate is null)
                return NoContent();

            var before = toUpdate.CloneComparable();

            Service<ChannelService>().ValidateRequest(toUpdate, channelType, validationOTP);

            if (HasBusinessErrors)
                return Response();
            else
            {
                Profile updated;
                try
                {
                    updated = await Repository.UpdateAsync(toUpdate);

                    await NotifySubscribersOfChangesBetween(before, updated);
                }
                catch (OptimisticConcurrencyLightException)
                {
                    if (tryNum <= 3)
                        return await ValidateMyChannelAsync(channelType, validationOTP, ++tryNum);
                    else
                        throw;
                }
                return Response(updated.FactoryVM());
            }
        }

        public async Task<DomainResponse> UpdateById(string id, EditProfileVM edit, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Update Profile By Id", $"id: {id} tryNum:{tryNum}");

            var toUpdate = await Repository.GetByIdAsync<Profile>(id);

            if (toUpdate is null)
                return NoContent();

            if (toUpdate.Accounts.First().Source == AccountSource.AAD.Code &&
                toUpdate.Channels.WillChangeFrom(edit))
                return BadRequest("Its not allowed to update channels (phone and email) of AAD users");

            if (toUpdate.Channels.WillRemoveAnyFrom(edit))
                return BadRequest("Its not allowed to remove an already defined channel (phone or email)");

            CheckAlternateKeys(toUpdate.Id,
                               toUpdate.Channels.Email,
                               toUpdate.Channels.Phone,
                               edit.Email,
                               edit.Phone);

            if (HasBusinessErrors)
                return Response();
            else
            {
                Profile updated;
                try
                {
                    var before = toUpdate.CloneComparable();
                    toUpdate.MapFromEditVM(edit);

                    updated = await Repository.UpdateAsync(toUpdate);

                    await NotifySubscribersOfChangesBetween(before, updated);

                    return Response(updated.FactoryVM());
                }
                catch (OptimisticConcurrencyLightException)
                {
                    if (tryNum <= 3)
                        return await UpdateById(id, edit, ++tryNum);
                    else
                        throw;
                }
            }
        }

        public async Task<DomainResponse> GetById(string id, bool onlyIM)
        {
            Telemetry.TrackEvent("Get Profile By Id", id);

            var profile = await Get(id, onlyIM);

            if (profile is null)
                return NoContent();

            return Response(profile.FactoryVM());
        }

        public async Task<DomainResponse> GetPendingChangesById(string id)
        {
            Telemetry.TrackEvent("Get Profile With Pending Changes By Id", id);

            var profile = await Get(id);

            if (profile is null)
                return NoContent();

            return Response(profile.FactoryWithPendingChangesVM());
        }

        public DomainResponse GetByEmail(string email)
        {
            Telemetry.TrackEvent("Get User by email", email);

            email ??= "";

            var profile = Repository.Get<Profile>(p => p.Channels.Email == email.ToLower())
                                    .AsEnumerable()
                                    .FirstOrDefault();
            if (profile is null)
                return NoContent();

            return Response(profile.FactoryVM());
        }

        internal DomainResponse GetIdByChannel(string channel)
        {
            Telemetry.TrackEvent("Get Profile Id By Channel", channel);

            channel ??= "";

            var profile = Repository.Get<Profile>(p => p.Channels.Email == channel.ToLower() || 
                                                       p.Channels.Phone == channel)
                                    .AsEnumerable()
                                    .FirstOrDefault();

            if (profile is null || 
                profile.Accounts.First().Source == AccountSource.AAD.Code)
                return NoContent();

            return Response(profile.FactoryBasicVM());
        }

        public async Task<DomainResponse> GetByIdsAsync(List<string> ids)
        {
            Telemetry.TrackEvent("Get users by ids", $"ids: {string.Join(",", ids)}");
            List<ProfileBasicVM> vms = new();

            if (ids is null || ids.Count == 0 || ids.Any(i => string.IsNullOrEmpty(i)))
                return Response(vms);

            var profiles = Repository.Get<Profile>(p => ids.Contains(p.Id), orderBy: p => p.Name);

            foreach (var profile in profiles)
                vms.Add(profile.FactoryBasicVM());

            //Checks if not found users haven't been created (invited) on AAD directly and, if so, creates them
            foreach (var id in ids.Where(i => !vms.Any(p => p.Id == i)))
            {
                var (user, roles) = await AADService.GetUserByIdAsync(id);

                if (user is not null)
                {
                    var profile = await CreateProfileForAADUserAsync(user, roles);

                    if (profile is not null)
                        vms.Add(profile.FactoryBasicVM());
                }
            }

            return Response(vms);
        }

        public DomainResponse GetByRole(string role, bool all)
        {
            Telemetry.TrackEvent("Get users by role", role);

            var profiles = Repository.Get<Profile>(p => p.Accounts.Any(a => a.Roles.Any(r => r == role)) &&
                                                        (all || p.Status == ProfileStatus.Active.Code),
                                                   p => p.Name);

            List<ProfileVM> vms = new();
            foreach (var profile in profiles)
                vms.Add(profile.FactoryVM());

            return Response(vms);
        }

        public DomainResponse GetByRoles(List<string> roles, bool all)
        {
            Telemetry.TrackEvent("Get users by roles", string.Join(", ", roles));

            return Response(GetUsersByRoles(roles, all));
        }

        private static List<ProfileVM> GetUsersByRoles(List<string> roles, bool all = false)
        {
            var profiles = Repository.Get<Profile>(p => p.Accounts.Any(a => a.Roles.Any(r => roles.Contains(r))) &&
                                                        (all || p.Status == ProfileStatus.Active.Code),
                                                   p => p.Name);

            List<ProfileVM> vms = new();
            foreach (var profile in profiles)
                vms.Add(profile.FactoryVM());

            return vms;
        }

        public async Task<DomainResponse> GetCurrentAccountAsync()
        {
            Telemetry.TrackEvent("Get Current Account", CurrentUserId);

            var profile = await Repository.GetByIdAsync<Profile>(CurrentUserId);

            if (profile is null)
            {
                Account current = Account.FactoryFromAADClaims(Context.User);
                if (current?.Source == AccountSource.AAD.Code)
                {
                    profile = await CreateProfileForAADClaimsAsync(Context.User);

                    if (profile is null)
                        return NoContent();
                }
                else
                    return NoContent();
            }

            return Response(AccountVM.FactoryFrom(profile.Accounts.First()));
        }

        public async Task<DomainResponse> DeleteMe(string feedback)
        {
            Telemetry.TrackEvent("Delete Profile of Current Account", CurrentUserId);

            var profile = await Repository.GetByIdAsync<Profile>(CurrentUserId);
            if (profile is null)
                return NoContent();

            await Repository.DeleteAsync<Profile>(profile.Id);

            SendLogoutDomainEvent(profile);

            await NotifySubscribersOfDeletedAsync(profile);
            NotifyBackOfficeManagersOfOptOut(profile, feedback);

            return Response(profile.FactoryVM());
        }

        public async Task<DomainResponse> CreateOrUpdateWithOTP(ProfileVM toCreate, int? tryNum = 1)
        {
            bool updating = false;

            Telemetry.TrackEvent("Create User With OTP", $"email: {toCreate.Email} tryNum:{tryNum}");

            string eTag = null;

            var existing = await Repository.GetByIdAsync<Profile>(toCreate.Id);
            if (existing is not null)
            {
                //Updates the newProfile so to get existing data so the method could continue as if it was a newProfile
                var toMerge = existing.FactoryVM();
                eTag = existing.ETag;
                toMerge.MapFrom(toCreate);
                toCreate = toMerge;

                updating = true;
            }
            else
            {
                //The new user language is currently based on the inviter's
                toCreate.Language = CultureInfo.CurrentUICulture.Name;

                //If not informed, the timeZone is the default one
                if (string.IsNullOrEmpty(toCreate.TimeZone))
                    toCreate.TimeZone = DEFAULT_TIMEZONE;
            }

            CheckAlternateKeys(existing?.Id,
                               existing?.Channels.Email,
                               existing?.Channels.Phone,
                               toCreate.Email,
                               toCreate.Phone);

            if (HasBusinessErrors)
                return Response();
            else
            {
                var toSave = Profile.FactoryMFrom(toCreate);

                var account = new Account
                {
                    Id = toCreate.Id,
                    Source = AccountSource.IM.Code,
                    Roles = new() { AccountRole.Member.Code }
                };

                account.Credentials.GenerateNewOTP();
                toSave.Accounts.Add(account);

                Profile saved;
                if (updating)
                {
                    try
                    {
                        toSave.ETag = eTag;
                        saved = await Repository.UpdateAsync(toSave);

                        AddBusinessInfo("USER_PROFILE_UPDATED");

                        await NotifySubscribersOfChangesBetween(existing.CloneComparable(), saved);
                    }
                    catch (OptimisticConcurrencyLightException)
                    {
                        if (tryNum <= 3)
                            return await CreateOrUpdateWithOTP(toCreate, ++tryNum);
                        else
                            throw;
                    }
                }
                else
                {
                    saved = await Repository.AddAsync(toSave);

                    AddBusinessInfo("USER_PROFILE_CREATED");

                    await NotifySubscribersOfNew(saved);
                }

                return Response(saved.FactoryWithOTPVM());
            }
        }

        public async Task<DomainResponse> CreateServiceAccount(string id, string name, string email)
        {
            Telemetry.TrackEvent("Create Service Account User", id);

            var existing = await Repository.GetByIdAsync<Profile>(id);
            if (existing is not null)
                return Conflict("SERVICE_ACCOUNT_ALREADY_EXISTS");

            string unencriptedSecret = Credentials.RandomizeSecret();

            var toSave = new Profile()
            {
                Id = id,
                Name = name.Replace(" (Service Account)", "")
                           .Replace("(", "")
                           .Replace(")", "") + " (Service Account)",
                Channels = new()
                {
                    Email = email?.ToLower(),
                    EmailIsValid = true
                },
                Accounts = new()
                                        { new()
                                                {
                                                    Id = id,
                                                    Source = AccountSource.IM.Code,
                                                    Roles = new() { AccountRole.ServiceAccount.Code },
                                                    Credentials = new()
                                                    {
                                                        Secret = Credentials.OneWayEncript(unencriptedSecret)
                                                    }

                                                }
                                        }
            };

            Profile saved = await Repository.AddAsync(toSave);

            AddBusinessInfo("SERVICE_ACCOUNT_CREATED");

            await NotifySubscribersOfNew(saved);

            return Response(new ServiceAccountVM() { Id = id, Name = saved.Name, Email = saved.Channels.Email, Secret = unencriptedSecret });
        }

        public async Task<DomainResponse> UpdateServiceAccount(string id, string name, string email, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Update Service Account User", $"id: {id} tryNum:{tryNum}");

            var toSave = await Repository.GetByIdAsync<Profile>(id);
            //Ignores not found to make it easier for other MSs to keep SP name and email updated,
            //so they do not need to control if the SP is created or not - just triggering SP update if the original data is updated
            if (toSave is null)
                return Response();

            var before = toSave.CloneComparable();
            toSave.Name = name.Replace(" (Service Account)", "")
                              .Replace("(", "")
                              .Replace(")", "") + " (Service Account)";

            if (!string.IsNullOrEmpty(email))
                toSave.Channels.Email = email;

            Profile saved;
            try
            {
                saved = await Repository.UpdateAsync(toSave);

                AddBusinessInfo("SERVICE_ACCOUNT_UPDATED");

                await NotifySubscribersOfChangesBetween(before, saved);
            }
            catch (OptimisticConcurrencyLightException)
            {
                if (tryNum <= 3)
                    return await UpdateServiceAccount(id, name, email, ++tryNum);
                else
                    throw;
            }

            return Response(new ServiceAccountVM() { Id = id, Name = saved.Name, Email = saved.Channels.Email });
        }

        public async Task<DomainResponse> DeleteServiceAccount(string id)
        {
            Telemetry.TrackEvent("Delete Service Account User", id);

            var toDelete = await Repository.GetByIdAsync<Profile>(id);

            if (toDelete is null)
                return Response();
            
            var deleted = await Repository.DeleteAsync<Profile>(id);

            return Response(new ServiceAccountVM() { Id = id, Name = deleted?.Name, Email = deleted?.Channels?.Email });
        }

        public async Task<DomainResponse> GenerateServiceAccountSecret(string id, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Generate Service Account Secret", $"id: {id} tryNum:{tryNum}");

            var toSave = await Repository.GetByIdAsync<Profile>(id);

            if (toSave is null)
                return NoContent();
            else
            {
                string unencriptedSecret = Credentials.RandomizeSecret();

                toSave.Accounts.First().Credentials.Secret = Credentials.OneWayEncript(unencriptedSecret);

                Profile saved;
                try
                {
                    saved = await Repository.UpdateAsync(toSave);

                    AddBusinessInfo("SERVICE_ACCOUNT_SECRET_CREATED");
                }
                catch (OptimisticConcurrencyLightException)
                {
                    if (tryNum <= 3)
                        return await GenerateServiceAccountSecret(id, ++tryNum);
                    else
                        throw;
                }


                return Response(new ServiceAccountVM() { Id = id, Name = saved.Name, Email = saved.Channels.Email, Secret = unencriptedSecret });
            }
        }

        internal async Task<DomainResponse> SyncFromAADUsersAsync()
        {
            Telemetry.TrackEvent("Sync Profiles from AAD Users");

            string sql = $@"SELECT *
                            FROM <{nameof(Profile)}> c
                            WHERE c.accounts[0].source = '{AccountSource.AAD.Code}'
                              AND (   c.channels.email LIKE '%{PRD_DOMAIN}'
                                   OR c.channels.email LIKE '%{DEV_DOMAIN}')";

            int count = 0;
            foreach (var profile in Repository.Query<Profile>(sql))
                if (await SyncFromAADAsync(profile))
                    count++;

            return Response(count);
        }

        internal async Task<DomainResponse> MigrateAsync()
        {
            Telemetry.TrackEvent("Migrate Profiles");

            Console.WriteLine();
            Console.WriteLine("******** MIGRATING PROFILES *******");

            string sql = @$"SELECT c
                            FROM <{nameof(Profile)}> c
                            JOIN a IN c.accounts
                            JOIN r IN a.roles
                            WHERE a.source IN ('iM')
                              AND ARRAY_CONTAINS(a.roles , 'client')";

            int count = 0;
            foreach (var doc in Repository.Query<JsonDocument>(sql))
            {
                var profile = doc.Property("c").ToObject<Profile>();
                profile.Accounts.First().Source = AccountSource.AAD.Code;
                await Repository.UpdateAsync(profile);
                var msg = $"{++count} Migrated -> ({profile.Id}) {profile.Channels.Email}/{profile.Status}";
                Console.WriteLine(msg);
            }

            return Response();
        }

        private async Task<bool> SyncFromAADAsync(Profile profile)
        {
            var (user, roles) = await AADService.GetUserByIdAsync(profile.Accounts.First().Id);

            if (user is null)
            {
                await Repository.DeleteAsync<Profile>(profile.Id);
                await NotifySubscribersOfDeletedAsync(profile);

                return true;
            }

            var before = profile.CloneComparable();
            bool synced = false;

            if (profile.Status == ProfileStatus.Inactive.Code)
            {
                profile.Status = ProfileStatus.Active.Code;
                synced = true;
            }

            if (!profile.Accounts.First().Roles.OrderBy(x => x).SequenceEqual(roles.OrderBy(x => x)))
            {
                profile.Accounts.First().Roles = roles;
                synced = true;
            }

            if (profile.Channels.Email != user.Email && 
                !string.IsNullOrEmpty(user.Email))
            {
                profile.Channels.Email = user.Email;
                synced = true;
            }

            if (profile.Name != user.Name && 
                !string.IsNullOrEmpty(user.Name))
            {
                profile.Name = user.Name;
                synced = true;
            }

            if (synced)
            {
                var updated = await Repository.UpdateAsync(profile);
                await NotifySubscribersOfChangesBetween(before, updated);
            }

            return synced;
        }

        internal async Task<Profile> Get(string id, bool onlyIM = false)
        {
            Telemetry.TrackEvent("Get Profile", id);

            var profile = await Repository.GetByIdAsync<Profile>(id);

            //Checks if not found user hasn't been created (invited) on AAD directly and, if so, creates it
            if (profile is null && !onlyIM)
            {
                var (user, roles) = await AADService.GetUserByIdAsync(id);

                if (user is not null)
                    profile = await CreateProfileForAADUserAsync(user, roles);
            }

            return profile;
        }

        internal async Task<Profile> Put(Profile profile)
        {
            Telemetry.TrackEvent("Update Profile", profile.Id);

            return await Repository.UpdateAsync(profile);
        }

        internal async Task<Profile> CreateProfileForAADClaimsAsync(ClaimsPrincipal user)
        {
            try
            {
                var toSave = Profile.FactoryFromAADClaims(user);

                var saved = await Repository.AddAsync(toSave);

                await NotifySubscribersOfNew(saved);

                return saved;
            }
            catch 
            {
                //Ignore errors caused by DB is reset in non-production environments
                if (!WorkBench.IsProductionEnvironment)
                    return null;
                else
                    throw;
            }
        }

        internal async Task<Profile> CreateProfileForAADUserAsync(DirectoryUserSummaryVM user, List<string> roles, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Create Profile for AAD User", $"id: {user?.Id} tryNum:{tryNum}");

            var fromAAD = Profile.FactoryFromAADUser(user, roles);

            if (fromAAD is null)
                return null;

            Profile existing = await Repository.GetByIdAsync<Profile>(user.Id);

            Profile saved;
            if (existing is null)
            {
                saved = await Repository.AddAsync(fromAAD);
                await NotifySubscribersOfNew(saved);
            }
            else
            {
                try
                {
                    var before = existing.CloneComparable();

                    //Maps only key AAD data
                    existing.Name = fromAAD.Name;
                    existing.Accounts.First().Roles = fromAAD.Accounts.First().Roles;
                    if (!string.IsNullOrEmpty(fromAAD.Channels.Email))
                        existing.Channels.Email = fromAAD.Channels.Email?.ToLower();

                    saved = await Repository.UpdateAsync(existing);

                    await NotifySubscribersOfChangesBetween(before, saved);
                }
                catch (OptimisticConcurrencyLightException)
                {
                    if (tryNum <= 3)
                        return await CreateProfileForAADUserAsync(user, roles, ++tryNum);
                    else
                        throw;
                }
            }

            return saved;
        }

        internal async Task<Profile> UpdateRolesForAADUserAsync(Profile toSave, List<string> roles)
        {
            Telemetry.TrackEvent("Update Roles for AAD User", $"id: {toSave.Id} tryNum:1");

            if (roles is null)
                return toSave;

            var before = toSave.CloneComparable();
            toSave.Accounts.First().Roles = roles;

            Profile saved;
            try
            {
                saved = await Repository.UpdateAsync(toSave);
                await NotifySubscribersOfChangesBetween(before, saved);
            }
            catch (OptimisticConcurrencyLightException)
            {
                return await UpdateRolesForAADUserAsync(toSave.Id, roles, 2);
            }

            return saved;
        }
        
        internal async Task<Profile> UpdateRolesForAADUserAsync(string id, List<string> roles, int? tryNum = 1)
        {
            Telemetry.TrackEvent("Update Roles for AAD User", $"id: {id} tryNum:{tryNum}");

            var toSave = await Get(id);
            var before = toSave.CloneComparable();
            toSave.Accounts.First().Roles = roles;

            Profile saved;
            try
            {
                saved = await Repository.UpdateAsync(toSave);
                await NotifySubscribersOfChangesBetween(before, saved);
            }
            catch (OptimisticConcurrencyLightException)
            {
                if (tryNum <= 3)
                    return await UpdateRolesForAADUserAsync(id, roles, ++tryNum);
                else
                    throw;
            }

            return saved;
        }

        internal async Task NotifySubscribersOfNew(Profile profile)
        {
            var msg = FactoryLightMessage<ProfileMSG>(ProfileCMD.Create);
            profile.MapToMSG(msg);

            await userProfileBus.SendToTopicAsync(msg);
        }

        private void SendLogoutDomainEvent(Profile profile)
        {
            DomainEventMSG logout = FactoryLightMessage<DomainEventMSG>(DomainEventCMD.Notify);
            logout.Name = "logout";
            logout.ShortMessage = LightLocalizer.Localize("YOU_HAVE_BEEN_DISCONNECTED");
            logout.UserIds.Add(profile.Id);

            PlatformServices.SendDomainEvent(logout);
        }

        private async Task NotifySubscribersOfDeletedAsync(Profile profile)
        {
            var msg = FactoryLightMessage<ProfileMSG>(ProfileCMD.Delete);
            profile.MapToMSG(msg);

            await userProfileBus.SendToTopicAsync(msg);
        }

        private void NotifyBackOfficeManagersOfOptOut(Profile profile, string feedback)
        {
            var msg = FactoryLightMessage<EmailMSG>(EmailCMD.Send);
            msg.Type = NotificationType.Tasks.Code;
            msg.Subject = LightLocalizer.Localize("MEMBER_OPTED_OUT_SHORT_SUBJECT", profile.Name);
            msg.Message = LightLocalizer.Localize("MEMBER_OPTED_OUT_SHORT_MESSAGE", profile.Name, feedback, "{EmployeeAppURL}" + $"/members/{profile.Id}");

            foreach (var manager in GetUsersByRoles(new() { "generalAdmin", "fieldManager" }))
            {
                msg.UserId = manager.Id;
                PlatformServices.SendEmail(msg);
            }
        }

        internal async Task NotifySubscribersOfChangesBetween(ComparableProfileVM toCompare, Profile updated)
        {
            var before = FactoryLightMessage<ProfileMSG>(ProfileCMD.Update);
            toCompare.MapToMSG(before);

            var toSend = FactoryLightMessage<ProfileMSG>(ProfileCMD.Update);
            updated.MapToMSG(toSend);

            //Notify subscribing microservices about subscribable changes in user's profile
            if (before.ToJsonString() != toSend.ToJsonString())
                await userProfileBus.SendToTopicAsync(toSend);
        }

        private void CheckAlternateKeys(string profileId, string oldEmail, string oldPhone, string toSaveEmail, string toSavePhone)
        {
            toSaveEmail ??= "";

            if (oldEmail != toSaveEmail || (oldPhone != toSavePhone && toSavePhone is not null))
            {
                var existingEmailOrPhone = Repository.Get<Profile>(p => p.Id != profileId &&
                                                                        (p.Channels.Email == toSaveEmail.ToLower() ||
                                                                         p.Channels.Phone == toSavePhone ||
                                                                         p.Channels.EmailToChange == toSaveEmail.ToLower() ||
                                                                         p.Channels.PhoneToChange == toSavePhone));
                if (existingEmailOrPhone.AsEnumerable().Any())
                    BusinessError("PHONE_OR_EMAIL_ALREADY_REGISTERED");
            }
        }
    }
}