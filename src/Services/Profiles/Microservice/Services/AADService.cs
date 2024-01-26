using Liquid;
using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Runtime;
using Microservice.Configuration;
using Microservice.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microservice.Services
{
    internal class AADService : LightService
    {

        #region Service Operations

        public async Task<ClaimsPrincipal> GetClaimsFromIdTokenAsync(string idToken)
        {
            if (await ValidateIdTokenAsync(idToken))
                try
                {
                    var userClaims = DecodeIdToken(idToken);

                    if (userClaims.FindFirstValue("hasgroups") == "true")
                        userClaims = ReplaceGroupsByRoles(userClaims, LoadGroupsFromMSGraphAsync(userClaims).Result);
                    else
                        userClaims = ReplaceGroupsByRoles(userClaims, userClaims.Claims
                                                                                .Where(c => c.Type == "groups")
                                                                                .Select(g => MapRoleFromGroup(g.Value)));

                    return userClaims;
                }
                //An exception thrown while decoding means an malformed token
                catch (LightException)
                {
                    BadRequest("malformed token");
                }

            return null;
        }

        public async Task<DomainResponse> GetAADUsersByEmailFilter(string tip, List<string> emailFilters, bool guestOnly)
        {
            return Response(await AADRepository.GetUsersByEmailFilterAsync(tip, emailFilters, guestOnly));
        }

        public async Task<DomainResponse> UpdateUserRoles(string id, List<string> roles)
        {
            var newGroups = MapGroupsFromRoles(roles);
            var oldGroups = await AADRepository.GetMemberGroupsByUserAsync(id);

            var toRemove = oldGroups.Where(g => !newGroups.Contains(g)).ToList();
            var toAdd = newGroups.Where(g => !oldGroups.Contains(g)).ToList();

            await AADRepository.RemoveUserGroupsAsync(id, toRemove);
            await AADRepository.AddUserGroupsAsync(id, toAdd);

            var profile = await Service<ProfileService>().UpdateRolesForAADUserAsync(id, roles);

            return Response(ProfileVM.FactoryFrom(profile));
        }

        public async Task<DomainResponse> InviteUser(string name, string email, string role, string redirectUrl)
        {
            name = name.Trim().Replace("  ", " ");
            email = email.ToLower();

            var profileOfSameEmail = Repository.Get<Profile>(p => p.Channels.Email == email || p.Channels.EmailToChange == email).FirstOrDefault();

            if (profileOfSameEmail is not null)
            {
                var accountOfSameEmail = profileOfSameEmail.Accounts.First();
                if (accountOfSameEmail.Source == AccountSource.IM.Code ||
                    accountOfSameEmail.Roles.Contains(AccountRole.Member.Code))
                    return Conflict("CANNOT_INVITE_MEMBERS_AS_ADD", email);
                else
                    return Conflict("USER_OF_SAME_EMAIL", email);
            }

            var (invitation, criticCode) = await AADRepository.InviteUserAsync(name, email, redirectUrl);

            if (invitation is null)
                return BusinessError(criticCode);
            
            if (!string.IsNullOrEmpty(criticCode))
                AddBusinessWarning(criticCode);

            var newGroup = MapGroupFromRole(role);

            var oldGroups = await AADRepository.GetMemberGroupsByUserAsync(invitation.Id);

            await AADRepository.RemoveUserGroupsAsync(invitation.Id, oldGroups.Where(g => g != newGroup).ToList());

            if (!oldGroups.Any(g => g == newGroup))
                await AADRepository.AddUserGroupsAsync(invitation.Id, new List<string> { newGroup });

            await Service<ProfileService>().CreateProfileForAADUserAsync(DirectoryUserSummaryVM.FactoryFrom(invitation), new List<string> { role });

            return Response(invitation);
        }

        internal static async Task<(DirectoryUserSummaryVM User, List<string> Roles)> GetUserByIdAsync(string id)
        {
            var user = await AADRepository.GetUserAsync(id);

            if (user is not null)
                return (user, MapRolesFromGroups(await AADRepository.GetMemberGroupsByUserAsync(id)));

            return (null, null);
        }

        #endregion

        #region Token handling

        private static ClaimsPrincipal DecodeIdToken(string jwt)
        {
            ClaimsIdentity claims = null;

            try
            {
                if (!string.IsNullOrEmpty(jwt))
                {
                    claims = new ClaimsIdentity(new JwtSecurityToken(jwtEncodedString: jwt).Claims, "Custom");

                    var oid = claims.FindFirst("oid");

                    claims.RemoveClaim(claims.FindFirst(Liquid.Runtime.JwtClaimTypes.UserId));
                    claims.AddClaim(new Claim(Liquid.Runtime.JwtClaimTypes.UserId, oid.Value));

                    claims.RemoveClaim(oid);
                }
            }
            catch (Exception e)
            {
                throw new LightException("Error when trying to decode JWT into User.Claims", e);
            }

            if (claims is null)
            {
                return null;
            }
            else
            {
                //Validation PASSED
                return new(claims);
            }

        }

        private async Task<bool> ValidateIdTokenAsync(string jwt)
        {
            bool forceValidate = false;

            if (!forceValidate && (WorkBench.IsDevelopmentEnvironment || WorkBench.IsIntegrationEnvironment || WorkBench.IsQualityEnvironment || WorkBench.IsDemonstrationEnvironment))
                return true;

            // Get Config from AAD according to the configManager's refresh parameter
            var AADConfig = await AADRepository.ConfigManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAudience = AADRepository.Config.AADServicePrincipalId,
                ValidIssuer = $"https://login.microsoftonline.com/{AADRepository.Config.AADTenantId}/v2.0",
                IssuerSigningKeys = AADConfig.SigningKeys
            };

            try
            {
                //Throws an Exception as the token is invalid (expired, invalid-formatted, etc.)
                new JwtSecurityTokenHandler().ValidateToken(jwt, validationParameters, out var validatedToken);
                return validatedToken is not null;
            }
            catch (SecurityTokenException e)
            {
                string invalidationError = $"'{e.Message.Split(".")[0]}'";
                BadRequest($"token is invalid by the motive {invalidationError}");

                WorkBench.Telemetry.TrackException(new LightException($"POSSIBLE THREAT! Attempt to authenticate with invalid token_id: error {invalidationError} from token '{jwt}'"));
                return false;
            }
            catch (ArgumentException e)
            {
                BadRequest($"token is invalid by the motive {e.Message}");

                return false;
            }
        }

        #endregion

        #region Group>Role mapping

        private static async Task<IEnumerable<Claim>> LoadGroupsFromMSGraphAsync(ClaimsPrincipal userClaims)
        {
            var userId = userClaims.FindFirstValue(Liquid.Runtime.JwtClaimTypes.UserId);

            try
            {
                var groups = await AADRepository.GetMemberGroupsByUserAsync(userId);
                return groups.Select(g => MapRoleFromGroup(g)).ToList();
            }
            catch (ServiceException e)
            {
                throw GraphApiExceptionFor(userId, e);
            }
        }

        private static ClaimsPrincipal ReplaceGroupsByRoles(ClaimsPrincipal userClaims, IEnumerable<Claim> roles)
        {
            var identity = new ClaimsIdentity(userClaims.Claims);

            var claimsToRemove = new List<Claim>();
            foreach (var hasGroups in identity.Claims.Where(c => c.Type == "hasgroups"))
                claimsToRemove.Add(hasGroups);

            foreach (var group in identity.Claims.Where(c => c.Type == "groups"))
                claimsToRemove.Add(group);

            foreach (var claim in claimsToRemove)
                identity.RemoveClaim(claim);

            foreach (var role in roles)
                if (role is not null)
                    identity.AddClaim(role);

            return new(identity);
        }

        private static Claim MapRoleFromGroup(string groupId)
        {
            if (AADRepository.Config?.AADGroupsAsRoles is null)
                return null;

            var groupAsRole = AADRepository.Config.AADGroupsAsRoles.Find(x => x.GroupObjectId == groupId);

            if (groupAsRole is not null)
                return new(ClaimsIdentity.DefaultRoleClaimType, groupAsRole.RoleName);
            else
                return null;
        }

        private static List<string> MapRolesFromGroups(List<string> groups)
        {
            var roles = new List<string>();
            foreach (var group in groups)
            {
                var role = MapRoleFromGroup(group);
                if (role is not null)
                    roles.Add(role.Value);
            }

            return roles;
        }

        private static string MapGroupFromRole(string role)
        {
            if (AADRepository.Config?.AADGroupsAsRoles is null)
                return null;

            var groupAsRole = AADRepository.Config.AADGroupsAsRoles.Find(x => x.RoleName == role);

            if (groupAsRole is not null)
                return groupAsRole.GroupObjectId;
            else
                return null;
        }

        private static List<string> MapGroupsFromRoles(List<string> roles)
        {
            var groups = new List<string>();
            foreach (var role in roles)
            {
                var group = MapGroupFromRole(role);
                if (group is not null)
                    groups.Add(group);
            }

            return groups;
        }

        #endregion

        #region Exceptions

        private static Exception GraphApiExceptionFor(string userId, ServiceException e)
        {
            return new LightException(LightLocalizer.Localize("CANNOT_ACCESS_USER_IN_GRAPH_API", userId, AADRepository.Config.AADTenantId), e);
        }

        internal static List<string> CheckRolesToUpdate(Profile profile)
        {
            var account = profile.Accounts.FirstOrDefault();
            if (account.Source == AccountSource.AAD.Code)
            {
                var currentRoles = MapRolesFromGroups(AADRepository.GetMemberGroupsByUserAsync(profile.Id).Result);

                if (account.Roles.Except(currentRoles).Any() ||
                    currentRoles.Except(account.Roles).Any())
                    return currentRoles;
            }

            return null;
        }

        #endregion

    }

    internal class AADRepository
    {
        #region AAD / MS Graph Connection

        public static readonly AuthenticationConfig Config = LightConfigurator.LoadConfig<AuthenticationConfig>("Authentication");

        public static readonly ConfigurationManager<OpenIdConnectConfiguration> ConfigManager = new($"https://login.microsoftonline.com/{Config.AADTenantId}/.well-known/openid-configuration",
                                                                                                                                                      new OpenIdConnectConfigurationRetriever());

        private static readonly IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                                                                                                    .Create(Config.AADServicePrincipalId)
                                                                                                    .WithTenantId(Config.AADTenantId)
                                                                                                    .WithClientSecret(Config.AADServicePrincipalPassword)
                                                                                                    .Build();

        private static readonly GraphServiceClient msGraph = FactoryNewGraphServiceClient();

        private static readonly int GRAPH_SERVICE_TIMEOUT_IN_MS = 50000;

        private static async Task<string> GetAccessTokenAsync()
        {
            // Client credential flow requires permission scopes on the app registration in aad, then scope is just default
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).
            var authResult = await confidentialClientApplication.AcquireTokenForClient(scopes)
                                                                .ExecuteAsync();

            return authResult.AccessToken;
        }

        private static GraphServiceClient FactoryNewGraphServiceClient()
        {

            // Build the Microsoft Graph client. As the authentication provider, set an async lambda
            // which uses the MSAL client to obtain an app-only access token to Microsoft Graph,
            // and inserts this access token in the Authorization header of each API request. 

            var auth = new DelegateAuthenticationProvider(async (requestMessage) =>
                                                                {
                                                                    // Add the access token in the Authorization header of the API request.
                                                                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                                                                                                                                         await GetAccessTokenAsync());
                                                                }
                                                         );

            var client = new GraphServiceClient(auth);

            return client;
        }

        #endregion

        #region Service methods

        internal static async Task<(DirectoryUserInvitationVM Invitation, string CriticCode)> InviteUserAsync(string name, string email, string redirectUrl)
        {
            try
            {
                using var timeoutToken = new CancellationTokenSource();
                var timeoutTask = Task.Delay(GRAPH_SERVICE_TIMEOUT_IN_MS, timeoutToken.Token);

                var invitationTask = msGraph.Invitations
                                            .Request()
                                            .AddAsync(new Invitation()
                                                          {
                                                              InvitedUserDisplayName = name,
                                                              InvitedUserEmailAddress = email,
                                                              InviteRedirectUrl = PlatformServices.ExpandAppUrls(redirectUrl),
                                                              SendInvitationMessage = false
                                                          },
                                                      timeoutToken.Token);

                if (await Task.WhenAny(invitationTask, timeoutTask) == invitationTask)
                {
                    timeoutToken.Cancel();
                    var invitation = await invitationTask;

                    var toUpdate = new User
                                   {
                                       PreferredLanguage = "pt-BR"
                                   };

                    await UpdateUser(invitation.InvitedUser.Id, toUpdate);

                    return (new()
                            {
                                Id = invitation.InvitedUser.Id,
                                Email = invitation.InvitedUserEmailAddress,
                                Name = invitation.InvitedUserDisplayName,
                                InviteRedeemUrl = invitation.InviteRedeemUrl
                            },
                            null);
                }
                else
                    throw new TimeoutException($"MS Graph invite operation took more than {(int)GRAPH_SERVICE_TIMEOUT_IN_MS / 1000}s to respond");
            }
            catch (ServiceException e)
            {
                if (e.StatusCode == HttpStatusCode.BadRequest &&
                    e.Error.Message.Contains("Group email address is not supported"))
                    return (null, "GROUP_EMAIL_ADDRESS_NOT_SUPPORTED");
                else
                    return (null, $"Status: {e.StatusCode} - Error: {e.Error.Message}");
            }
        }

        private static async Task UpdateUser(string userId, User toUpdate, int retry = 0)
        {
            const int RETRY_DELAY_MS = 1000;
            const int MAX_RETRIES = 4;

            try
            {
                using var timeoutToken = new CancellationTokenSource();
                var timeoutTask = Task.Delay(GRAPH_SERVICE_TIMEOUT_IN_MS, timeoutToken.Token);

                var updateTask = msGraph.Users[userId].Request().UpdateAsync(toUpdate, timeoutToken.Token);

                if (await Task.WhenAny(updateTask, timeoutTask) == updateTask)
                {
                    timeoutToken.Cancel();
                    await updateTask;
                }
                else
                    throw new TimeoutException($"MS Graph update operation took more than {(int)GRAPH_SERVICE_TIMEOUT_IN_MS / 1000}s to respond");
            }
            catch (ServiceException)
            {
                if (retry <= MAX_RETRIES)
                {
                    Thread.Sleep(RETRY_DELAY_MS * retry);
                    await UpdateUser(userId, toUpdate, ++retry);
                }
                else
                    throw;
            }
        }

        internal static async Task<List<DirectoryUserSummaryVM>> GetUsersByEmailFilterAsync(string tip, List<string> emailFilters, bool guestOnly)
        {
            string filter = guestOnly ? "userType eq 'guest'" : string.Empty;
            List<string> escapedEmailFilters = emailFilters.Where(f => !string.IsNullOrEmpty(f)).Select(f => f.ToEscapedString()).ToList();

            if (!string.IsNullOrEmpty(tip))
            {
                tip = tip.ToEscapedString();

                if (!string.IsNullOrEmpty(filter))
                    filter += " and ";

                filter += $"(startswith(displayName,'{tip}') or startswith(mail,'{tip}'))";
            }

            if (escapedEmailFilters.Count > 0)
            {
                if (!string.IsNullOrEmpty(filter))
                    filter += " and ";

                filter += "(endswith(mail,'" +
                          string.Join(@$"') or endswith(mail,'", escapedEmailFilters) +
                          "'))";
            }

            /*** The following code should work but the MS Graph SDK and/or its documentation is buggy
            *    https://github.com/microsoftgraph/microsoft-graph-docs/issues/4331
            *   
            
            var request = msGraph.Users.Request().Filter(filter);
            request.Headers.Add(new HeaderOption("ConsistencyLevel", "Eventual"));

            var users = await request.GetAsync();

            return users.ToList();

            *
            *  So we had to make a REST workaround as bellow
            */

            var client = new HttpClient();

            foreach (var header in msGraph.Users.Request().Headers)
                client.DefaultRequestHeaders.Add(header.Name, header.Value);

            client.DefaultRequestHeaders.Add("ConsistencyLevel", "Eventual");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

            string uri = msGraph.Users.RequestUrl + $"?$count=true&$select=displayName,mail,id&$filter={filter}";
            var response = await client.GetAsync(uri);

            JsonDocument jResponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            List<DirectoryUserSummaryVM> users = new();

            foreach (var jObj in jResponse.Property("value").EnumerateArray())
            {
                users.Add(new DirectoryUserSummaryVM()
                {
                    Id = jObj.Property("id").AsString(),
                    Name = jObj.Property("displayName").AsString(),
                    Email = jObj.Property("mail").AsString()?.ToLower(),
                });
            }

            return users;
        }

        internal static async Task<DirectoryUserSummaryVM> GetUserAsync(string userId)
        {
            User user = null;
            try
            {
                using var timeoutToken = new CancellationTokenSource();
                var timeoutTask = Task.Delay(GRAPH_SERVICE_TIMEOUT_IN_MS, timeoutToken.Token);

                var getTask = msGraph.Users[userId].Request().GetAsync();

                if (await Task.WhenAny(getTask, timeoutTask) == getTask)
                {
                    timeoutToken.Cancel();
                    user = await getTask;
                }
                else
                    throw new TimeoutException($"MS Graph get operation took more than {(int)GRAPH_SERVICE_TIMEOUT_IN_MS / 1000}s to respond");
            }
            catch (ServiceException e)
            {
                if (e.StatusCode != HttpStatusCode.NotFound)
                    WorkBench.Telemetry.TrackException(new LightException("Error acessing Microsoft Graph", e));
            }

            if (user is null)
                return null;
            else
                return new()
                {
                    Id = user.Id,
                    Name = user.DisplayName,
                    Email = user.Mail?.ToLower()
                };
        }

        internal static async Task<List<string>> GetMemberGroupsByUserAsync(string userId)
        {
            IDirectoryObjectGetMemberGroupsCollectionPage groups = default;
            try
            {
                using var timeoutToken = new CancellationTokenSource();
                var timeoutTask = Task.Delay(GRAPH_SERVICE_TIMEOUT_IN_MS, timeoutToken.Token);

                var getGroupTask = msGraph.Users[userId].GetMemberGroups(true).Request().PostAsync();

                if (await Task.WhenAny(getGroupTask, timeoutTask) == getGroupTask)
                {
                    timeoutToken.Cancel();
                    groups = await getGroupTask;
                }
                else
                    throw new TimeoutException($"MS Graph get group operation took more than {(int)GRAPH_SERVICE_TIMEOUT_IN_MS / 1000}s to respond");
            }
            catch (ServiceException e)
            {
                if (e.StatusCode != HttpStatusCode.NotFound)
                    WorkBench.Telemetry.TrackException(new LightException("Error acessing Microsoft Graph", e));
            }
            return groups?.Select(g => g).ToList() ?? new List<string>();
        }

        internal static async Task RemoveUserGroupsAsync(string userId, List<string> groups)
        {
            if (groups is null)
                return;

            foreach (var group in groups)
                await msGraph.Groups[group].Members[userId].Reference.Request().DeleteAsync();
        }

        internal static async Task AddUserGroupsAsync(string userId, List<string> groups)
        {
            if (groups is null)
                return;

            foreach (var group in groups)
                await msGraph.Groups[group].Members.References.Request().AddAsync(new DirectoryObject() { Id = userId });
        }

        #endregion
    }
}