using Liquid;
using Liquid.Activation;
using Liquid.Base;
using Liquid.Platform;
using Microservice.Services;
using Microservice.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microservice.Controllers
{
    /// <summary>
    /// API with its endpoints and exchangeable datatypes
    /// </summary>
    [Authorize]
    [Route("/")]
    [Produces("application/json")]
    public class NotificationsController : LightController
    {
        /// <summary>
        /// Migrate
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "generalAdmin")]
        [HttpPost("migrate")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(Response<List<string>>), 200)]
        public async Task<IActionResult> Migrate()
        {
            var data = await Factory<NotificationService>().Migrate();

            return Result(data);
        }

        /// <summary>
        /// Gets all notifications of a given user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>List of notifications</returns>
        [Authorize(Roles = "generalAdmin, clientManager, clientSetup, projectManager, fieldManager, scheduler, fieldAnalyst, quality, methodology, analytics, validator, finance")]
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(Response<List<HistoryVM>>), 200)]
        public IActionResult GetAllByUser(string userId)
        {
            var data = Factory<NotificationService>().GetAllByUser(userId);
            return Result(data);
        }

        /// <summary>
        /// Gets all notifications of the current user
        /// </summary>
        /// <returns>List of notifications</returns>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(Response<List<NotificationVM>>), 200)]
        public IActionResult GetAllMine()
        {
            var data = Factory<NotificationService>().GetAllMine();
            return Result(data);
        }

        /// <summary>
        /// Gets a given notification of the current user
        /// </summary>
        /// <param name="id">Id of the notification</param>
        /// <returns>The user notification</returns>
        [HttpGet("mine/{id}")]
        [ProducesResponseType(typeof(Response<NotificationVM>), 200)]
        public async Task<IActionResult> GetMineById(string id)
        {
            var data = await Factory<NotificationService>().GetMineById(id);
            return Result(data);
        }

        /// <summary>
        /// Marks as viewed all notifications of the current user
        /// </summary>
        /// <returns>List of marked notifications</returns>
        [HttpPut("mine")]
        [ProducesResponseType(typeof(Response<List<NotificationVM>>), 200)]
        public async Task<IActionResult> MarkAllMineAsViewed()
        {
            var data = await Factory<NotificationService>().MarkAllMineAsViewed();
            return Result(data);
        }

        /// <summary>
        /// Marks as viewed a given notification of the current user
        /// </summary>
        /// <param name="id">Id of the notification</param>
        /// <returns>The marked notification</returns>
        [HttpPut("mine/{id}")]
        [ProducesResponseType(typeof(Response<NotificationVM>), 200)]
        public async Task<IActionResult> MarkMineAsViewedById(string id)
        {
            var data = await Factory<NotificationService>().MarkMineAsViewedById(id);
            return Result(data);
        }

        /// <summary>
        /// Clears all viewed all notifications of the current user
        /// </summary>
        /// <returns>List of cleared notifications</returns>
        [HttpDelete("mine")]
        [ProducesResponseType(typeof(Response<List<NotificationVM>>), 200)]
        public async Task<IActionResult> ClearAllMineViewed()
        {
            var data = await Factory<NotificationService>().ClearAllMineViewed();
            return Result(data);
        }

        /// <summary>
        /// Registers a webpush notification subscription endpoint for the current user
        /// </summary>
        /// <param name="subscriptionEndpoint">The webpush notification endpoint subscription data to register</param>
        /// <returns>The subscription registered</returns>
        [HttpPost("mine/web/devices")]
        [ProducesResponseType(typeof(Response<WebPushEndpointVM>), 200)]
        public async Task<IActionResult> WebPushSubscribeAsync([FromBody] WebPushEndpointVM subscriptionEndpoint)
        {
            ValidateInput(subscriptionEndpoint);
            var data = await Factory<WebPushService>().SubscribeAsync(subscriptionEndpoint);
            return Result(data);
        }

        /// <summary>
        /// Unregisters a webpush notification subscription endpoint for the current user
        /// </summary>
        /// <param name="deviceId">Id of the device registered for webpush notification</param>
        /// <returns>The subscription unregistered</returns>
        [HttpDelete("mine/web/devices/{deviceId}")]
        [ProducesResponseType(typeof(Response<WebPushEndpointVM>), 200)]
        public async Task<IActionResult> WebPushUnsubscribeAsync(string deviceId)
        {
            var data = await Factory<WebPushService>().UnsubscribeAsync(deviceId);
            return Result(data);
        }

        /// <summary>
        /// Gets basic user info of the notification
        /// </summary>
        /// <param name="id">Id of the notification</param>
        /// <returns>User basic info</returns>
        [AllowAnonymous]
        [HttpGet("{id}/userBasicInfo")]
        [ProducesResponseType(typeof(Response<BasicUserInfoVM>), 200)]
        public async Task<IActionResult> GetUserIdById(string id)
        {
            var data = await Factory<NotificationService>().GetUserOfNotification(id);
            return Result(data);
        }

        /// <summary>
        /// Gets notification types
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("types")]
        [ProducesResponseType(typeof(Response<List<NotificationType>>), 200)]
        public IActionResult GetStatusTypes()
        {
            var data = Factory<NotificationService>().GetTypes();
            return Result(data);
        }

        /// <summary>
        /// Test method to send webpushs
        /// </summary>
        /// <param name="notifVM">Notification content</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("test/webpush/send")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(Response<int>), 200)]
        public async Task<IActionResult> TestWebPushSend([FromBody] NotificationVM notifVM)
        {
            ValidateInput(notifVM);
            var data = await Factory<WebPushService>().SendAsync(notifVM);
            return Result(data);
        }

        /// <summary>
        /// Test method to send emails
        /// </summary>
        /// <param name="emailMessage">Email content</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("test/email/send")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(Response<JsonDocument>), 200)]
        public async Task<IActionResult> TestEmailSend([FromBody] EmailMSG emailMessage)
        {
            ValidateInput(emailMessage);
            var data = await Factory<EmailService>().SendAsync(emailMessage);
            return Result(data);
        }

        /// <summary>
        /// Test method to send emails
        /// </summary>
        /// <param name="notifVM">Notification content</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("test/notif/send")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(Response<NotificationVM>), 200)]
        public async Task<IActionResult> TestSendNotification([FromBody] NotificationVM notifVM)
        {
            ValidateInput(notifVM);

            //Forces SentAt & Id because this test endpoint is a shortcut of the notification workflow
            notifVM.Id = Guid.NewGuid().ToString();
            notifVM.SentAt = WorkBench.UtcNow;

            var data = await Factory<NotificationService>().SendNotificationAsync(notifVM);
            return Result(data);
        }
    }
}