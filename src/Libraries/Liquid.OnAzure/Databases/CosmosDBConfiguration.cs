using FluentValidation;
using Liquid.Repository;
using Liquid.Runtime;

namespace Liquid.OnAzure
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
{
    /// <summary>
    /// The Configuration for CosmosDB
    /// </summary>
    public class CosmosDBConfiguration : LightConfig<CosmosDBConfiguration>
    {
        public string Endpoint { get; set; }
        public string AuthKey { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerPrefix { get; set; }
        public bool CreateIfNotExists { get; set; }
        public int DatabaseRUs { get; set; }
        public string ConnectionMode { get; set; } = "Direct";
        public string ConnectionProtocol { get; set; } = "Tcp";

        /// <summary>
        /// Configuration to create an Azure blob block in containers
        /// </summary>
        public MediaStorageConfiguration MediaStorage { get; set; }

        /// <summary>
        /// The necessary validation to create an blob block
        /// </summary>
        public override void Validate()
        {
            RuleFor(d => Endpoint).NotEmpty().WithError("Endpoint on CosmosDB settings should not be empty.");

            RuleFor(d => AuthKey).NotEmpty().WithError("AuthKey on CosmosDB settings should not be empty.");

            RuleFor(d => DatabaseId).NotEmpty().WithError("DatabaseId on CosmosDB settings should not be empty.");

            RuleFor(d => CreateIfNotExists).NotNull().WithError("CreateIfNotExists on CosmosDB settings should not be empty.");

            RuleFor(d => DatabaseRUs).NotEmpty().WithError("DatabaseRUs on CosmosDB settings should not be empty.");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}