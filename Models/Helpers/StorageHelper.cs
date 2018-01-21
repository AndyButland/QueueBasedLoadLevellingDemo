namespace QBLLDemo.Common.Helpers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using QBLLDemo.Common.Models;

    public static class StorageHelper
    {
        private const string LogTableName = "MessageLog";
        private const string QueueName = "messages";

        private static readonly string StorageConnestionString = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");

        public static async Task WriteToLog(Guid messageId, MessageStageOption messageStage, string errorMessage = "")
        {
            var table = await GetOrCreateTable(LogTableName);

            var record = new MessageStatus(messageId.ToString())
                {
                    MessageStage = messageStage,
                    ErrorMessage = errorMessage,
                };

            var operation = TableOperation.InsertOrReplace(record);
            await table.ExecuteAsync(operation);
        }

        public static async Task<MessageStatus> GetMessageById(string messageId)
        {
            var table = await GetOrCreateTable(LogTableName);
            var operation = TableOperation.Retrieve<MessageStatus>(messageId, string.Empty);
            var result = await table.ExecuteAsync(operation);

            return result?.Result as MessageStatus;
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
            return CloudStorageAccount.Parse(StorageConnestionString);
        }

        public static async Task AddToQueue(string messageContent)
        {
            var queue = await GetOrCreateQueue(QueueName);

            var message = new CloudQueueMessage(messageContent);
            queue.AddMessage(message);
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
    }
}
