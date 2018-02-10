namespace QBLLDemo.MessageProcessor
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using QBLLDemo.Common.Helpers;
    using QBLLDemo.Common.Models;

    public static class ProcessMessage
    {
        [FunctionName("ProcessMessage")]
        public static async Task Run([QueueTrigger("messages", 
            Connection = "AzureWebJobsStorage")]string queueItem, TraceWriter log)
        {
            var leaseId = await StorageHelper.AcquireLease(30);
            if (string.IsNullOrEmpty(leaseId))
            {
                // Lease could not be acquired, so put back on queue with visibility delay.
                var visibilityDelay = TimeSpan.FromSeconds(5);
                await StorageHelper.AddToQueue(queueItem, visibilityDelay);
            }

            try
            {
                // Deserialize message from queue into parts
                Guid messageId;
                string body;
                DeserializeMessage(queueItem, out messageId, out body);

                // Processing of the message
                var result = await PerformProcessMessage(body);

                // Update message status in log on success
                if (result.Status == ProcessMessageResultStatus.Success)
                {
                    await StorageHelper.WriteToLog(messageId, 
                        MessageStageOption.Complete);
                }
                else
                {
                    // Update message status and error message in log on failure
                    await StorageHelper.WriteToLog(messageId, 
                        MessageStageOption.ErrorProcessingMessage, 
                        result.ErrorMessage);

                    // If we can retry, throw exception so message remains on queue
                    if (result.Status == ProcessMessageResultStatus.FailCanRetry)
                    {
                        throw new InvalidOperationException(result.ErrorMessage);
                    }
                }
            }
            finally
            {
                // Release lease
                await StorageHelper.ReleaseLease(leaseId);
            }
        }

        public static void DeserializeMessage(string queueItem, out Guid messageId, out string body)
        {
            var splitterPosition = queueItem.IndexOf("|", StringComparison.Ordinal);
            messageId = Guid.Parse(queueItem.Substring(0, splitterPosition));
            body = queueItem.Substring(splitterPosition + 1);
        }

        private static async Task<ProcessMessageResult> PerformProcessMessage(string message)
        {
            // Simulate a success result
            return await Task.FromResult(new ProcessMessageResult { Status = ProcessMessageResultStatus.Success });
        }
    }
}
