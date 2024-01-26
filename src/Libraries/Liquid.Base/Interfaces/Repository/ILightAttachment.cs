using System.Collections.Generic;
using System.IO;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ILightAttachment
    {
        string Id { get; set; }
        string Name { get; set; }
        string ContentType { get; set; }
        string MediaLink { get; set; }
        Stream MediaStream { get; set; }
        string ResourceId { get; set; }
        public string ETag { get; set; }
        public List<string> Attachments { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
