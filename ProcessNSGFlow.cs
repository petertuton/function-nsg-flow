using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;

namespace Function
{
    public static class ProcessNSGFlow
    {
        [FunctionName("ProcessNSGFlow")]
        public static async Task Run(
            [BlobTrigger("%blobContainerName%/resourceId=/SUBSCRIPTIONS/{subId}/RESOURCEGROUPS/{resourceGroup}/PROVIDERS/MICROSOFT.NETWORK/NETWORKSECURITYGROUPS/{nsgName}/y={blobYear}/m={blobMonth}/d={blobDay}/h={blobHour}/m={blobMinute}/macAddress={mac}/PT1H.json", Connection = "nsgSourceDataAccount")] string myBlob, 
            [Table("checkpoints", Connection = "AzureWebJobsStorage")] CloudTable checkpointTable,
            string subId, string resourceGroup, string nsgName, string blobYear, string blobMonth, string blobDay, string blobHour, string blobMinute, string mac,
            ILogger log)
        {
            log.LogInformation($"ProcessNSGFlow triggered on blob\n Name:/resourceId=/SUBSCRIPTIONS/{subId}/RESOURCEGROUPS/{resourceGroup}/PROVIDERS/MICROSOFT.NETWORK/NETWORKSECURITYGROUPS/{nsgName}/y={blobYear}/m={blobMonth}/d={blobDay}/h={blobHour}/m={blobMinute}/macAddress={mac}/PT1H.json \n Size: {myBlob.Length} Bytes");

            // Init the blob details
            BlobDetails blobDetails = new BlobDetails(subId, resourceGroup, nsgName, blobYear, blobMonth, blobDay, blobHour, blobMinute, mac);

            // Get the checkpoint for this blob
            Checkpoint checkpoint = Checkpoint.GetCheckpoint(blobDetails, checkpointTable);

            // Calculate the length of data to process
            int index = checkpoint.CheckpointIndex;
            int count = (int)myBlob.Length - index;
            log.LogInformation($"Blob: {blobDetails.ToString()}, index: {index}, number of new bytes: {count}");

            // Grab just the new content
            string newContent = myBlob.Substring(index, count);
            log.LogInformation(newContent);

            // Do something with the updated data... e.g. send it to a downstream processor (it could be another Azure Function!)

            // Update the checkpoint for this blob
            checkpoint.PutCheckpoint(checkpointTable, index+count);
        }
    }
}
