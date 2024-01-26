using Liquid.Base;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Platform;
using Microservice.Configuration;
using Microservice.Models;
using Microservice.ViewModels;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using WebPush;

namespace Microservice.Services
{
    internal class WebPushService : LightService
    {
        internal async Task<DomainResponse> SubscribeAsync(WebPushEndpointVM webPushEndpoint)
        {
            Telemetry.TrackEvent("Add WebPush Subscription", CurrentUserId);

            Config config = await Repository.GetByIdAsync<Config>(CurrentUserId);

            if (config is null)
                return NoContent();

            WebPushEndpoint endpointToSave = WebPushEndpoint.FactoryFrom(webPushEndpoint);

            //Removes if already exists a subscription for the device, so updating any other property
            var index = config.WebPushChannel.Endpoints.FindIndex(e => e.DeviceId == endpointToSave.DeviceId ||
                                                                       e.PushEndpoint == endpointToSave.PushEndpoint ||
                                                                       e.PushP256DH == endpointToSave.PushP256DH ||
                                                                       e.PushAuth == endpointToSave.PushAuth);
            if (index >= 0)
                config.WebPushChannel.Endpoints.RemoveAt(index);

            config.WebPushChannel.Endpoints.Add(endpointToSave);

            var updatedChannel = await Repository.UpdateAsync(config);

            return Response(WebPushEndpointVM.FactoryFrom(endpointToSave));
        }

        internal async Task<DomainResponse> UnsubscribeAsync(string deviceId)
        {
            Telemetry.TrackEvent("Delete WebPush Subscription", CurrentUserId);

            Config configToUpdate = await Repository.GetByIdAsync<Config>(CurrentUserId);

            if (configToUpdate is null)
                return NoContent();

            var endpointToDelete = configToUpdate.WebPushChannel.Endpoints.Find(e => e.DeviceId == deviceId);

            if (endpointToDelete is null)
                return NoContent();

            configToUpdate.WebPushChannel.Endpoints.Remove(endpointToDelete);

            var updatedChannel = await Repository.UpdateAsync(configToUpdate);

            return Response(WebPushEndpointVM.FactoryFrom(endpointToDelete));
        }

        internal async Task<DomainResponse> SendAsync(NotificationVM notification)
        {
            Telemetry.TrackEvent("Send WebPush", notification.Id);

            var userConfig = await Service<ConfigService>().GetConfigById(notification.UserId);

            if (userConfig is null)
                return NoContent();

            var sucessCount = await SendToAllEndPointsAsync(userConfig, notification, notification.Type);

            return Response(sucessCount);
        }

        internal async Task<int> SendToAllEndPointsAsync(Config userConfig, ILightViewModel notification, string notificationType = null)
        {
            int successCount = 0;

            if (userConfig is null)
                return successCount;

            if (notificationType is not null && !userConfig.WebPushChannel.IsValidNotificationType(notificationType))
            {
                AddBusinessError("NOTIFICATION_TYPE_NOT_OPTED_IN");
                return successCount;
            }

            if (userConfig?.WebPushChannel?.HasAvailableEndpoints != true)
            {
                return successCount;
            }

            var vapidDetails = new VapidDetails(NotificationConfig.vapidSubject, NotificationConfig.vapidPublicKey, NotificationConfig.vapidPrivateKey);
            var webPushClient = new WebPushClient();
            var invalidEndpoints = new List<WebPushEndpoint>();

            foreach (var endpoint in userConfig.WebPushChannel.Endpoints)
            {
                var pushSubscription = new PushSubscription(endpoint.PushEndpoint, endpoint.PushP256DH, endpoint.PushAuth);

                Telemetry.TrackTrace(userConfig.Id + endpoint.PushEndpoint);
                try
                {
                    webPushClient.SendNotification(pushSubscription, notification.ToJsonString(), vapidDetails);
                    successCount++;
                }
                catch (TaskCanceledException ex)
                {
                    Telemetry.TrackException(new LightException($"Exception caught while sending WebPush to endpoint '{endpoint.PushEndpoint}':", ex));
                }
                catch (WebPushException wpEx)
                {
                    // Recovers marking the endpoint as invalid
                    if (wpEx.StatusCode == HttpStatusCode.Gone)
                        invalidEndpoints.Add(endpoint);
                    else if (wpEx.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        invalidEndpoints.Add(endpoint);
                        Telemetry.TrackException(new LightException($"UNAUHTORIZED error while sending WebPush: endpoint jwt & Vapid Keys don't match. Check user config data & configs", wpEx));
                    }
                    else
                        Telemetry.TrackException(new LightException($"UNKNOWN error while sending WebPush: See inner exception", wpEx));
                }
            }

            // Removes invalid endpoints and saves the user config
            foreach (var invalid in invalidEndpoints)
                userConfig.WebPushChannel.Endpoints.Remove(invalid);

            if (invalidEndpoints.Count > 0)
                await Repository.UpdateAsync(userConfig);

            return successCount;
        }
    }
}