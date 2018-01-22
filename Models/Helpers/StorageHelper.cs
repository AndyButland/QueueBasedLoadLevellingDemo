namespace QBLLDemo.Common.Helpers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using QBLLDemo.Common.Models;

    public static class StorageHelper
    {
        private const string LogTableName = "MessageLog";
        private const string QueueName = "messages";
        private const string LeaseContainer = "messages-lease";
        private const string LeaseBlob = "lock.txt";

        private static readonly string StorageConnectionString = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");

        public static async Task WriteToLog(Guid messageId, 
                                            MessageStageOption messageStage, 
                                            string errorMessage = "")
        {
            // Get a reference to the log table
            var table = await GetOrCreateTable(LogTableName);

            // Create a log record instance using the message id as the key
            var record = new MessageStatus(messageId.ToString())
                {
                    MessageStage = messageStage,
                    ErrorMessage = errorMessage,
                };

            // Add or update the log record in table storage
            var operation = TableOperation.InsertOrReplace(record);
            await table.ExecuteAsync(operation);
        }

        public static async Task<MessageStatus> GetMessageById(string messageId)
        {
            // Get a reference to the log table
            var table = await GetOrCreateTable(LogTableName);

            // Retrieve the table record for the requested Id
            var operation = TableOperation.Retrieve<MessageStatus>(messageId, string.Empty);
            var result = await table.ExecuteAsync(operation);
            return result?.Result as MessageStatus;
        }

        public static async Task AddToQueue(string messageContent, TimeSpan visibilityDelay)
        {
            // Get a reference to the queue
            var queue = await GetOrCreateQueue(QueueName);

            // Add the message to the queue
            var message = new CloudQueueMessage(messageContent);
            await queue.AddMessageAsync(message, null, visibilityDelay, null, null);
        }

        public static async Task<string> AcquireLease(int leaseForSeconds)
        {
            var container = await GetOrCreateContainer(LeaseContainer);
            var blob = container?.GetBlockBlobReference(LeaseBlob);
            if (blob == null)
            {
                throw new InvalidOperationException($"Could not locate or create blob {LeaseBlob} in container {LeaseContainer} in order to acquire lease.");
            }

            if (!await blob.ExistsAsync())
            {
                await blob.UploadTextAsync("---");
            }

            var leaseTime = TimeSpan.FromSeconds(leaseForSeconds);
            try
            {
                return await blob.AcquireLeaseAsync(leaseTime);
            }
            catch (StorageException)
            {
                // Lease could not be acquired
                return string.Empty;
            }
        }

        public static async Task ReleaseLease(string leaseId)
        {
            var blob = GetBlob(LeaseContainer, LeaseBlob);
            if (blob == null)
            {
                throw new InvalidOperationException($"Could not locate blob {LeaseBlob} in container {LeaseContainer} in order to release lease.");
            }

            var acc = new AccessCondition { LeaseId = leaseId };
            await blob.ReleaseLeaseAsync(acc);
        }

        private static async Task<CloudTable> GetOrCreateTable(string tableName)
        {
            var account = GetStorageAccount();

            var client = account.CreateCloudTableClient();

            var table = client.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            return table;
        }

        private static CloudStorageAccount GetStorageAccount()
        {
            return CloudStorageAccount.Parse(StorageConnectionString);
        }

        private static async Task<CloudQueue> GetOrCreateQueue(string queueName)
        {
            var queue = GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync();
            return queue;
        }

        private static CloudQueue GetQueueReference(string queueName)
        {
            var account = GetStorageAccount();
            var client = account.CreateCloudQueueClient();
            return client.GetQueueReference(queueName);
        }

        private static async Task<CloudBlobContainer> GetOrCreateContainer(string containerName)
        {
            var container = GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        private static CloudBlobContainer GetContainerReference(string containerName)
        {
            var account = CloudStorageAccount.Parse(StorageConnectionString);
            var client = account.CreateCloudBlobClient();
            return client.GetContainerReference(containerName);
        }

        private static CloudBlockBlob GetBlob(string containerName, string fileName)
        {
            var container = GetContainerReference(containerName);
            return container?.GetBlockBlobReference(fileName);
        }
    }
}
