using FluentValidation;
using Liquid.Runtime;

namespace Liquid.OnGoogle
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
{
    /// <summary>
    /// The Configuration for BigQuery
    /// </summary>
    public class BigQueryConfiguration : LightConfig<BigQueryConfiguration>
    {
        public string Base64Key { get; set; }
        public string ProjectId { get; set; }
        public string DatasetId { get; set; }

        /// <summary>
        /// The necessary validation to create an blob block
        /// </summary>
        public override void Validate()
        {
            RuleFor(d => Base64Key).NotEmpty().WithError("Base64Key on BiqQuery settings should not be empty.");

            RuleFor(d => ProjectId).NotEmpty().WithError("ProjectId on BiqQuery settings should not be empty.");

            RuleFor(d => DatasetId).NotEmpty().WithError("DatasetId on BiqQuery settings should not be empty.");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}