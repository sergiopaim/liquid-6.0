using Liquid.Base;
using Liquid.Interfaces;
using Liquid.Repository;
using Liquid.Runtime;
using Microsoft.Azure.Storage; // Namespace for CloudStorageAccount  
using Microsoft.Azure.Storage.Blob; // Namespace for Blob storage types  
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liquid.OnAzure
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
{
    /// <summary>
    /// Cartridge for Azure Blob
    /// </summary>
    public class AzureBlob : ILightMediaStorage
    {
        public MediaStorageConfiguration MediaStorageConfiguration { get; set; }
        public string Connection { get; set; }
        public string Permission { get; set; }
        private CloudBlobContainer ContainerReference { get; set; }
        private string container = string.Empty;
        public string Container
        {
            get { return container; }
            set
            {
                container = value;
                ContainerReference = BlobClient.GetContainerReference(value);
                ContainerReference.CreateIfNotExistsAsync();
                ContainerReference.SetPermissionsAsync(
                    new BlobContainerPermissions
                    {
                        PublicAccess = !string.IsNullOrEmpty(Permission) ?
                        (Permission.Equals("Blob") ? BlobContainerPublicAccessType.Blob :
                        (Permission.Equals("Off") ? BlobContainerPublicAccessType.Off :
                        (Permission.Equals("Container") ? BlobContainerPublicAccessType.Container :
                        (Permission.Equals("Unknown") ? BlobContainerPublicAccessType.Unknown : BlobContainerPublicAccessType.Blob)))) : BlobContainerPublicAccessType.Blob
                    });
            }
        }

        private CloudStorageAccount StorageAAccountConnection
        {
            get { return CloudStorageAccount.Parse(Connection); }
        }

        private CloudBlobClient BlobClient
        {
            get { return StorageAAccountConnection.CreateCloudBlobClient(); }
        }

        public void Initialize()
        {
            //Get the configuration on appsetting. But in this case the features can be accessed outside from the repository
            MediaStorageConfiguration = LightConfigurator.LoadConfig<MediaStorageConfiguration>("MediaStorage");
            Connection = MediaStorageConfiguration.ConnectionString;
            Permission = MediaStorageConfiguration.Permission;
            Container = MediaStorageConfiguration.Container;

            //If the MS has the configuration outside from the repository will be used this context and not inside
            WorkBench.Repository?.SetMediaStorage(this);
        }

        public async Task<ILightAttachment> GetAsync(string resourceId, string id)
        {
            var blob = ContainerReference.GetBlobReference(resourceId + "/" + id);
            
            Stream stream = new MemoryStream();

            await blob.DownloadToStreamAsync(stream, 
                                             new AccessCondition(), 
                                             new BlobRequestOptions() { DisableContentMD5Validation = true },
                                             new OperationContext());
            LightAttachment _blob = new()
            {
                MediaStream = stream,
                Id = id,
                ResourceId = resourceId,
                ContentType = blob.Properties.ContentType,
                Name = blob.Name,
                MediaLink = blob.Uri.AbsoluteUri
            };

            _blob.MediaStream.Position = 0;

            return _blob;
        }

        public async Task<Stream> GetBlockAsync(string resourceId, string id, int blockNumber)
        {
            string blockName = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockNumber.ToString("0000000000")));

            var blockBlob = ContainerReference.GetBlockBlobReference(resourceId + "/" + id);

            long offset = 0;
            long length = 0;
            foreach (var block in blockBlob.DownloadBlockList())
                if (block.Name == blockName)
                {
                    length = block.Length;
                    break;
                }
                else
                    offset += block.Length;

            Stream stream = new MemoryStream();

            await blockBlob.DownloadRangeToStreamAsync(stream, offset, length);

            return stream;

        }

        public async Task InsertUpdateAsync(ILightAttachment attachment)
        {
            var targetFile = attachment.Id;
            var blockBlob = ContainerReference.GetDirectoryReference(attachment.ResourceId).GetBlockBlobReference(targetFile);
            blockBlob.Properties.ContentType = attachment.ContentType;

            await blockBlob.UploadFromStreamAsync(attachment.MediaStream);
        }

        public async Task ReplaceAsync(ILightAttachment attachment)
        {
            var targetFile = attachment.Id;

            var directory = ContainerReference.GetDirectoryReference(attachment.ResourceId);

            var tempBlob = directory.GetBlockBlobReference(targetFile + "-temp");

            tempBlob.Properties.ContentType = attachment.ContentType;

            await tempBlob.UploadFromStreamAsync(attachment.MediaStream);

            var toReplace = directory.GetBlockBlobReference(targetFile);
            toReplace.StartCopy(tempBlob);
            while (toReplace.CopyState.Status == CopyStatus.Pending)
            {
                // Wait for the copy operation to complete
                System.Threading.Thread.Sleep(1000);
                toReplace.FetchAttributes();
            }

            // Delete the temp blob
            tempBlob.Delete();
        }

        public async Task AppendBlockAsync(string resourceId, string id, Stream block, int blockNumber)
        {
            string blockName = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockNumber.ToString("0000000000")));

            var blockBlob = ContainerReference.GetBlockBlobReference(resourceId + "/" + id);

            List<string> blockList;
            try
            {
                blockList = blockBlob.DownloadBlockList()
                                     .Select(b => b.Name)
                                     .ToList();
            }
            catch
            {
                if (!blockBlob.Exists())
                {
                    using MemoryStream ms = new();
                    await blockBlob.UploadFromStreamAsync(ms); //Empty memory stream. Will (re)create an empty blob.
                }

                blockList = blockBlob.DownloadBlockList()
                     .Select(b => b.Name)
                     .ToList();
            }

            using (block)
            {
                await blockBlob.PutBlockAsync(blockName, block);
            }

            if (!blockList.Contains(blockName))
                blockList.Add(blockName);

            await blockBlob.PutBlockListAsync(blockList.OrderBy(b => Encoding.UTF8.GetString(Convert.FromBase64String(b))));
        }

        public Task Remove(ILightAttachment attachment)
        {
            var targetFile = attachment.Id;
            var blockBlob = ContainerReference.GetDirectoryReference(attachment.ResourceId)
                                              .GetBlockBlobReference(targetFile);
            return blockBlob.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Method to run Health Check for AzureBlob Media Storage
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LightHealth.HealthCheckStatus HealthCheck(string serviceKey, string value)
        {
            try
            {
                TimeSpan span = new(0, 0, 15);
                ContainerReference.AcquireLeaseAsync(span);
                ContainerReference.BreakLeaseAsync(span);
                return LightHealth.HealthCheckStatus.Healthy;
            }
            catch
            {
                return LightHealth.HealthCheckStatus.Unhealthy;
            }
        }

        /// <summary>
        /// Removes a directory and its contents
        /// </summary>
        /// <param name="dirName">Directory name</param>
        public async Task RemoveDirectoryAsync(string dirName)
        {
            var rootDirFolder = ContainerReference.GetDirectoryReference(dirName)
                                                  .ListBlobsSegmentedAsync(true, BlobListingDetails.Metadata, null, null, null, null)
                                                  .Result;

            foreach (IListBlobItem blob in rootDirFolder.Results)
                if (blob.GetType() == typeof(CloudBlob) || blob.GetType().BaseType == typeof(CloudBlob))
                    await ((CloudBlob)blob).DeleteIfExistsAsync();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}