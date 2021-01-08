// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.EventGrid.Models;

namespace Function
{
    public static class FlowLogDeleted
    {
        private const string BlobCreatedEvent = "Microsoft.Storage.BlobCreated";
        private const string BlobDeletedEvent = "Microsoft.Storage.BlobDeleted";

        [FunctionName(nameof(FlowLogDeleted))]
        public static void Run(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [Table("checkpoints", Connection = "AzureWebJobsStorage")] CloudTable checkpointTable,
            ILogger log)
        {
            if (eventGridEvent == null && string.IsNullOrWhiteSpace(eventGridEvent.EventType))
                throw new ArgumentNullException("Null or Invalid Event Grid Event");

            log.LogInformation($@"New Event Grid Event:
    - Id=[{eventGridEvent.Id}]
    - EventType=[{eventGridEvent.EventType}]
    - EventTime=[{eventGridEvent.EventTime}]
    - Subject=[{eventGridEvent.Subject}]
    - Topic=[{eventGridEvent.Topic}]");

            if (eventGridEvent.EventType.Equals(BlobDeletedEvent))
            {
                // EventGridEvent.Subject contains the value of the deleted file, prefixed with "/blobServices/default/containers/insights-logs-networksecuritygroupflowevent/blobs/resourceId=" (hence the offset)
                // Intantiate the blob details, find it's checkpoint value, if it exists, delete it
                BlobDetails blobDetails = new BlobDetails(eventGridEvent.Subject, 5);
                Checkpoint.GetCheckpoint(blobDetails, checkpointTable, false)?.DeleteCheckpoint(checkpointTable);
            }
        }
    }
}
