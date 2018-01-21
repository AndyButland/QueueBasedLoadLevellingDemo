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
            var messageId = req.GetQuerystringValue("messageId");
            var messageStatus = await StorageHelper.GetMessageById(messageId);
            if (messageStatus == null)
            {
                return req.CreateErrorResponse(HttpStatusCode.NotFound, "Message not found");
            }
            
            return req.CreateResponse(HttpStatusCode.OK, 
                new
                    {
                        messageId = messageStatus.MessageId,
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
