using Liquid.Activation;
using Microservice.Services;
using Liquid.OnAzure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microservice.Events;

namespace Microservice.ReactiveHub
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [AllowAnonymous]
    [ReactiveHub(hubEndpoint: "/events")]
    public class GeneralHub : SignalRHub
    {
        #region Connection handling

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await Factory<ReactiveHubService>().RegisterConnection(Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
            await Factory<ReactiveHubService>().RemoveConnection(Context.ConnectionId);
        }

        #endregion 

        #region Server sent, client subscribed events
        [Authorize]
        public async Task NotificationSent(string connectionId, NotificationEV ev)
        {
            await Clients.Client(connectionId).SendAsync(nameof(NotificationSent), ev);
        }

        [Authorize]
        public async Task UserSessionEvent(string connectionId, UserSessionEV ev)
        {
            await Clients.Client(connectionId).SendAsync(nameof(UserSessionEvent), ev);
        }

        public async Task DomainEvent(string connectionId, DomainEV ev)
        {
            await Clients.Client(connectionId).SendAsync(nameof(DomainEvent), ev);
        }

        #endregion

        #region Client sent, server subscribed events
        [Authorize]
        public async Task SendUserSessionEvent(UserSessionEV userSessionEvent)
        {
            await Factory<ReactiveHubService>().SendToUserSessions(Context.ConnectionId, userSessionEvent);
        }

        #endregion
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
