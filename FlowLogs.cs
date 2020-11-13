using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Azure.Identity;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;

namespace Function
{
    public static class FlowLogsFunction
    {
        [FunctionName("FlowLogs")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Grab the Azure subscription id from the query string
            string subscriptionId = req.Query["subscriptionId"];
            string resourceGroup = req.Query["resourceGroup"];
            string networkWatcher = req.Query["networkWatcher"];
            log.LogInformation($"Flow Logs for subscription: '{subscriptionId}', in resource group: {resourceGroup}, for network watcher: {networkWatcher}");

            // Initialize the response
            List<Azure.ResourceManager.Network.Models.FlowLog> response = new List<Azure.ResourceManager.Network.Models.FlowLog> {};

            // Authenticate to Azure. See here for details on the authn attempt order: https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            var credential = new DefaultAzureCredential(); 

            // List the Flow Logs
            NetworkManagementClient networkManagementClient = new NetworkManagementClient(subscriptionId, credential);
            await foreach (var flowLog in networkManagementClient.FlowLogs.ListAsync(resourceGroup, networkWatcher))
            {
                response.Add(flowLog);
            }

            log.LogInformation($"Flow Logs: {JsonSerializer.Serialize(response)}");
            return new OkObjectResult(response);
        }
    }
}
