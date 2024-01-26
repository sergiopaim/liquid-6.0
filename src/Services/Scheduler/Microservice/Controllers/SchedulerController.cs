using Liquid.Activation;
using Liquid.Base;
using Microservice.Services;
using Microservice.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microservice.Controllers
{
    /// <summary>
    /// API with its endpoints and exchangeable datatypes
    /// </summary>
    [Authorize(Roles = "sysAdmin, generalAdmin")]
    [Route("/")]
    [Produces("application/json")]
    public class SchedulerController : LightController
    {
        /// <summary>
        /// Lists the status of all jobs
        /// </summary>
        /// <returns></returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(string), 200)]
        [Produces("text/plain")]
        [AllowAnonymous]
        public IActionResult ListAllJobs()
        {
            var data = Factory<SchedulerService>().ListJobs();
            return Ok(data);
        }

        /// <summary>
        /// Gets information of all jobs
        /// </summary>
        /// <returns></returns>
        [HttpGet("all")]
        [ProducesResponseType(typeof(Response<List<ScheduleVM>>), 200)]
        public IActionResult GetAllJobs()
        {
            var data = Factory<SchedulerService>().GetAllJobs();
            return Result(data);
        }

        /// <summary>
        /// Gets information of the jobs run by a microservice
        /// </summary>
        /// <param name="microservice">The name of the microservice</param>
        /// <returns></returns>
        [HttpGet("{microservice}")]
        [ProducesResponseType(typeof(Response<ScheduleVM>), 200)]
        public IActionResult GetByMicroservice(string microservice)
        {
            if (string.IsNullOrEmpty(microservice))
                AddInputError("microservice must not be empty");
            var data = Factory<SchedulerService>().GetByMicroservice(microservice);
            return Result(data);
        }

        /// <summary>
        /// Gets job information
        /// </summary>
        /// <param name="microservice">The name of the microservice that runs the job</param>
        /// <param name="jobName">The name of the job</param>
        /// <returns></returns>
        [HttpGet("{microservice}/{jobName}")]
        [ProducesResponseType(typeof(Response<ScheduleVM>), 200)]
        public IActionResult GetJob(string microservice, string jobName)
        {
            if (string.IsNullOrEmpty(microservice))
                AddInputError("microservice must not be empty");
            if (string.IsNullOrEmpty(jobName))
                AddInputError("jobName must not be empty");
            var data = Factory<SchedulerService>().GetJob(microservice, jobName);
            return Result(data);
        }

        /// <summary>
        /// Recativates a job
        /// </summary>
        /// <param name="microservice">The name of the microservice that runs the job</param>
        /// <param name="jobName">The name of the job</param>
        /// <returns></returns>
        [HttpPut("/{microservice}/{jobName}/reactivate")]
        [ProducesResponseType(typeof(Response<ScheduleVM>), 200)]
        public IActionResult ReactivateJob(string microservice, string jobName)
        {
            if (string.IsNullOrEmpty(microservice))
                AddInputError("microservice must not be empty");
            if (string.IsNullOrEmpty(jobName))
                AddInputError("jobName must not be empty");

            var data = Factory<SchedulerService>().ReactivateJob(microservice, jobName);
            return Result(data);
        }

        /// <summary>
        /// Aborts a job
        /// </summary>
        /// <param name="microservice">The name of the microservice that runs the job</param>
        /// <param name="jobName">The name of the job</param>
        /// <returns></returns>
        [HttpPut("/{microservice}/{jobName}/abort")]
        [ProducesResponseType(typeof(Response<ScheduleVM>), 200)]
        public async Task<IActionResult> AbortJob(string microservice, string jobName)
        {
            if (string.IsNullOrEmpty(microservice))
                AddInputError("microservice must not be empty");
            if (string.IsNullOrEmpty(jobName))
                AddInputError("jobName must not be empty");

            var data = await Factory<SchedulerService>().AbortJob(microservice, jobName);
            return Result(data);
        }
    }
}