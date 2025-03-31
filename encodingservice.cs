using System;
using System.Collections.Specialized;
using System.IO;
using Microsoft.Extensions.Logging;
using Xenhey.BPM.Core.Net8;
using Xenhey.BPM.Core.Net8.Implementation;
using Microsoft.Azure.Functions.Worker;
using PayPal.Api;

namespace AzureServiceBusToSQL
{
    public class notificationservice
    {
        private readonly ILogger _logger;

        public notificationservice(ILogger<notificationservice> logger)
        {
            _logger = logger;
        }

        [Function("notificationservice")]
        public void Run([ServiceBusTrigger("notification-service", " metadata-processor", Connection = "ServiceBusConnectionString")] string mySbMsg, Int32 deliveryCount, DateTime enqueuedTimeUtc, string messageId)
        {
            string ApiKeyName = "x-api-key";
            _logger.LogInformation("C# blob trigger function processed a request.");
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add(ApiKeyName, "43EFE991E8614CFB9EDECF1B0FDED37C");
            IOrchestrationService orchrestatorService = new ManagedOrchestratorService(nvc);
            var processFiles = orchrestatorService.Run(mySbMsg);
            _logger.LogInformation($"EnqueuedTimeUtc={enqueuedTimeUtc}");
            _logger.LogInformation($"DeliveryCount={deliveryCount}");
            _logger.LogInformation($"MessageId={messageId}");
        }
    }
}
