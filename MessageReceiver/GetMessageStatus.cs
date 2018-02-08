namespace QBLLDemo.MessageReceiver
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using QBLLDemo.Common.Helpers;

    public static class GetMessageStatus
    {
        [FunctionName("GetMessageStatus")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // Retrieve the message Id from the querystring
            var messageId = req.GetQuerystringValue("messageId");

            // Retrieve the corresponding message from table storage
            var messageStatus = await StorageHelper.GetMessageById(messageId);
            if (messageStatus == null)
            {
                return req.CreateErrorResponse(HttpStatusCode.NotFound, "Message not found");
            }
            
            // Construct and return a response representing the message
            return req.CreateResponse(HttpStatusCode.OK, 
                new
                    {
                        messageId = messageStatus.PartitionKey,
                        messageStage = messageStatus.MessageStage,
                        errorMessage = messageStatus.ErrorMessage
                    });
        }

        private static string GetQuerystringValue(this HttpRequestMessage req, string key)
        {
            return req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, key, StringComparison.OrdinalIgnoreCase) == 0).Value;
        }
    }
}
