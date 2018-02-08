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
        public MessageStatus()
        {
            // Necessary to avoid: "TableQuery Generic Type must provide a default parameterless constructor."
            // when retrieving record from table storage
        }

        public MessageStatus(string messageId)
        {
            PartitionKey = messageId;
            RowKey = string.Empty;
        }

        public string MessageStage { get; set; }

        public string ErrorMessage { get; set; }
    }
}
