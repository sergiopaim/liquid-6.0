using Microsoft.AspNetCore.Hosting;

namespace Liquid.Runtime.OpenApi
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public abstract class WorkBenchCommand : IWorkBenchCommand
    {
        private string[] args;
        private bool isReactiveHub;
        public IWebHost Host { get; protected set; }

        protected string[] Args()
        {
            return args;
        }

        protected void SetArgs(string[] value)
        {
            args = value;
        }

        protected bool IsReactiveHub()
        {
            return isReactiveHub;
        }

        protected void SetIsReactiveHub(bool value)
        {
            isReactiveHub = value;
        }

        public abstract bool Execute();

        public WorkBenchCommand(IWebHost host, string[] args, bool isReactiveHub = false) 
        {
            Host = host;
            SetArgs(args);
            SetIsReactiveHub(isReactiveHub);

        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
