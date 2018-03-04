namespace QBLLDemo.MessageReceiver
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using Newtonsoft.Json;
    using QBLLDemo.Common.Helpers;
    using QBLLDemo.Common.Models;

    public static class AddMessage
    {
        [FunctionName("AddMessage")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, 
            "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // Retrieve the message content POSTed in the request
            var messageContent = await req.Content.ReadAsStringAsync();

            // Create an Id for the message
            var messageId = Guid.NewGuid();

            // Validate the message - if not valid, write to the log and return a BadRequest response
            if (IsMessageValid(messageContent) == false)
            {
                var errorMessage = "Could not deserialize message";
                await StorageHelper.WriteToLog(messageId, MessageStageOption.ValidationFailed, 
                    errorMessage);
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, errorMessage);
            }

            // Construct the queue message and add it to the queue
            var queueMessage = messageId + "|" + messageContent;
            var randomSeconds = new Random().Next(10);
            await StorageHelper.AddToQueue(queueMessage, TimeSpan.FromSeconds(randomSeconds));

            // Write the message status to the log
            await StorageHelper.WriteToLog(messageId, MessageStageOption.OnQueue);

            // Return a Created response with a Location header from where message status can be checked
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Location = 
                new Uri($"{req.RequestUri.AbsoluteUri.Replace("AddMessage", "GetMessageStatus")}" + 
                    $"?messageId={messageId}");
            return response;
        }

        private static bool IsMessageValid(string body)
        {
            // Check message is valid by deserializing the JSON response into an instance of the 
            // expected class
            try
            {
                JsonConvert.DeserializeObject<Message>(body);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
