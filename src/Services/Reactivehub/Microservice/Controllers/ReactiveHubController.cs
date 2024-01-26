using Liquid.Activation;
using Liquid.Base;
using Liquid.Platform;
using Microservice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Microservice.Controllers
{
    /// <summary>
    /// API with its endpoints and exchanged datatypes
    /// </summary>
    [Authorize]
    [Route("/")]
    [Produces("application/json")]
    public class ReactiveHubController : LightController
    {
        /// <summary>
        /// Send notification to user
        /// </summary>
        /// <param name="notification">Notification contents</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("notify")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Notify([FromBody] NotificationVM notification)
        {
            ValidateInput(notification);
            var data = await Factory<ReactiveHubService>().Notify(notification);
            return Result(data);
        }
    }

}

