using System.Collections.Generic;

namespace Liquid.Base
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Class with all results for active cartriges and a general status
    /// </summary>
    public class LightHealthResult
    {
        public string Status { get; set; }
        /// <summary>
        /// Receive cartridges names and Healthy status
        /// </summary>
        public List<LightHealthCartridgeResult> CartridgesStatus = new();
    }

    public class LightHealthCartridgeResult
    {
        public string Name { get; set; }
        public string Status { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
