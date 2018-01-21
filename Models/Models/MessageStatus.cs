namespace QBLLDemo.Common.Models
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public enum MessageStageOption
    {
        ValidationFailed,
        OnQueue,
        ErrorProcessingMessage,
        Complete
    }

    public class MessageStatus : TableEntity
    {
        public MessageStatus(string messageId)
        {
            PartitionKey = messageId;
            RowKey = string.Empty;
        }

        public Guid MessageId { get; set; }

        public MessageStageOption MessageStage { get; set; }

        public string ErrorMessage { get; set; }
    }
}
