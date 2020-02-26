using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TagRides.Shared.DataStore
{
    public class AzureDataStore : IDataStore
    {
        public AzureDataStore()
        {
            var account = CloudStorageAccount.Parse(storageConnection);
            client = account.CreateCloudBlobClient();
        }

        public async Task GetStreamResource(string resource, Stream output)
        {
            CloudBlockBlob blob = await GetBlob(resource);
            
            await blob.DownloadToStreamAsync(output);

            output.Position = 0;
        }

        public async Task<string> GetStringResource(string resource)
        {
            CloudBlockBlob blob = await GetBlob(resource);

            return await blob.DownloadTextAsync();
        }

        public async Task<byte[]> GetByteResource(string resource)
        {
            CloudBlockBlob blob = await GetBlob(resource);

            await blob.FetchAttributesAsync();
            Byte[] photoBytes = new Byte[blob.Properties.Length];
            await blob.DownloadToByteArrayAsync(photoBytes, 0);
            return photoBytes;
        }

        public async Task PostStreamResource(string resource, Stream data)
        {
            CloudBlockBlob blob = await GetBlob(resource);

            await blob.UploadFromStreamAsync(data);
        }

        public async Task PostStringResource(string resource, string data)
        {
            CloudBlockBlob blob = await GetBlob(resource);

            await blob.UploadTextAsync(data);
        }

        public Task DeleteResource(string resource)
        {
            if (resource.IndexOf('/') == -1)
                return DeleteContainerAsync(resource);
            return DeleteBlobAsync(resource);
        }

        public async Task<bool> ResourceExists(string resource)
        {
            var splitResource = SplitResourceString(resource);

            CloudBlobContainer container = client.GetContainerReference(splitResource.Item1);

            if (!await container.ExistsAsync()) return false;

            CloudBlockBlob blob = container.GetBlockBlobReference(splitResource.Item2);

            return await blob.ExistsAsync();
        }

        #region private functions

        async Task<CloudBlockBlob> GetBlob(string resource)
        {
            var splitResource = SplitResourceString(resource);

            CloudBlobContainer container = client.GetContainerReference(splitResource.Item1);
            await container.CreateIfNotExistsAsync();

            return container.GetBlockBlobReference(splitResource.Item2);
        }

        /// <summary>
        /// Deletes the specified container
        /// </summary>
        /// <param name="resource">The container's name.</param>
        async Task DeleteContainerAsync(string containerName)
        {
            CloudBlobContainer container = client.GetContainerReference(containerName);

            await container.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Deletes the specified blob
        /// </summary>
        /// <param name="resource">Path for container and blob.</param>
        async Task DeleteBlobAsync(string resource)
        {
            CloudBlockBlob blob = await GetBlob(resource);

            await blob.DeleteIfExistsAsync();
        }

        (string, string) SplitResourceString(string resource)
        {
            int firstSlash = resource.IndexOf('/');
            if (firstSlash == -1) throw new Exception("Invalid resource.");

            return (resource.Substring(0, firstSlash), resource.Substring(firstSlash + 1));
        }

        #endregion

        readonly CloudBlobClient client;
        
        const string storageConnection = "DefaultEndpointsProtocol=https;AccountName=tagrides;AccountKey=Q/XpkE5cQqENIX3APbE2BLEK52U8Gl2k4fpV2QHYbu8LEHETTESbPZylS8apU7noDiUCS07XTt0DNNRAslcnEA==;EndpointSuffix=core.windows.net";
    }
}
