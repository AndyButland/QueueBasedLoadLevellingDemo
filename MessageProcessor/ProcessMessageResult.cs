namespace QBLLDemo.MessageProcessor
{
    public enum ProcessMessageResultStatus
    {
        Success,
        FailCanRetry,
        FailCannotRetry
    }

    public class ProcessMessageResult
    {
        public ProcessMessageResultStatus Status { get; set; }

        public string ErrorMessage { get; set; }
    }
}
