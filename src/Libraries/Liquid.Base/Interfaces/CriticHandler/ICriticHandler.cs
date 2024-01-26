using System.Collections.Generic;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Interface delegates to the handler that creates the Critics lists
    /// </summary> 
    public interface ICriticHandler
    {
        public bool HasNoContentError { get; }
        public bool HasConflictError { get; }
        public bool HasNotGenericReturn { get; }
        public bool HasBadRequestError { get; }

        bool HasBusinessErrors { get; }
        bool HasBusinessWarnings { get; }
        bool HasBusinessInfo { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="args"></param>
        void AddBusinessError(string errorCode, params object[] args);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="warningCode"></param>
        /// <param name="args"></param>
        void AddBusinessWarning(string warningCode, params object[] args);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="infoCode"></param>
        /// <param name="args"></param>
        void AddBusinessInfo(string infoCode, params object[] args);

        List<ICritic> Critics { get; }
        StatusCode StatusCode { get; set; }

        void ResetNoContentError();

        void ResetConflictError();
        bool HasCriticalErrors();
        Dictionary<string, object[]> GetCriticalErrors();
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}