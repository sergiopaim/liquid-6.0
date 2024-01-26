using System.Collections.Concurrent;

namespace Liquid.Domain
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class RegisteredJobs
    {
        private static ConcurrentDictionary<string, string> RegisteredJobsStatus { get; } = new();

        public static bool RegisterJob(string jobName, string status) => RegisteredJobsStatus.TryAdd(jobName, status);

        public static string JobStatus(string jobName) => RegisteredJobsStatus[jobName];

        // This should be changed whenever `JobCommandMSG.LightJobStatus` is changed
        public static bool NotAborted(string jobName) => RegisteredJobsStatus[jobName] != "Aborted";

        public static void UpdateJobStatus(string jobName, string status)
        {
            RegisteredJobsStatus[jobName] = status;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
