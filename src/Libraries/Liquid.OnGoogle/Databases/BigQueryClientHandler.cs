using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Liquid.Base;
using System;

namespace Liquid.OnGoogle
{
    class BigQueryClientHandler
    {
        private const int MIN_RESET_PERIOD_IN_SECS = 180;
        private string projectId;
        private GoogleCredential credential;
        private DateTime lastReset;

        private BigQueryClient client;
        public BigQueryClient Client { get => client; }

        public void Initialize(string projectId, GoogleCredential credential) 
        {
            if (client is null)
            {
                this.projectId = projectId;
                this.credential = credential;
                lastReset = WorkBench.UtcNow;

                client = BigQueryClient.Create(this.projectId, this.credential);
            }
            else
                throw new LightException("handler cannot be initialized again.");
        }

        public bool Reset() 
        {
            if (lastReset.AddSeconds(MIN_RESET_PERIOD_IN_SECS) <= WorkBench.UtcNow)
            {
                lastReset = WorkBench.UtcNow;
                client = BigQueryClient.Create(this.projectId, this.credential);
                return true;
            }
            else
                return false;
        }
    }
}