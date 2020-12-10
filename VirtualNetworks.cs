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
    public static class VirtualNetorksFunction
    {
        [FunctionName("VirtualNetworks")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Grab the Azure subscription id from the query string
            string subscriptionId = req.Query["subscriptionId"];
            log.LogInformation($"Virtual Networks for subscription: '{subscriptionId}'");

            // Initialize the response
            Vnets response = new Vnets
            {
                SubscriptionId = subscriptionId,
                VirtualNetworks = new List<VirtualNetwork>()
            };

            // Authenticate to Azure. See here for details on the authn attempt order: https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            var credential = new DefaultAzureCredential(); 

            // Iterate through each virtual network in this subscription
            NetworkManagementClient networkManagementClient = new NetworkManagementClient(subscriptionId, credential);
            await foreach (Azure.ResourceManager.Network.Models.VirtualNetwork virtualNetwork in networkManagementClient.VirtualNetworks.ListAllAsync())
            {
                string vnetName = virtualNetwork.Name;

                // Iterate through the vnet's subnets
                List<Subnet> subnets = new List<Subnet>();
                foreach (var subnet in virtualNetwork.Subnets)
                {
                    subnets.Add(new Subnet{
                        Name = subnet.Name,
                        AddressPrefix = subnet.AddressPrefix,
                        AddressPrefixes = subnet.AddressPrefixes
                    });
                }

                // Add the vnet's details to the response
                response.VirtualNetworks.Add(new VirtualNetwork
                {
                    Name = virtualNetwork.Name,
                    Region = virtualNetwork.Location,
                    AddressPrefixes = virtualNetwork.AddressSpace.AddressPrefixes,
                    Subnets = subnets
                });
            }

            log.LogInformation($"Virtual Networks: {JsonSerializer.Serialize(response)}");
            return new OkObjectResult(response);
        }
    }

    public class Vnets
    {
        public string SubscriptionId { get; set; }
        public IList<VirtualNetwork> VirtualNetworks { get; set; }
    }

    public class VirtualNetwork
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public IList<string> AddressPrefixes { get; set; }
        public IList<Subnet> Subnets { get; set; }
    }

    public class Subnet
    {
        public string Name { get; set; }
        public string AddressPrefix { get; set; }
        public IList<string> AddressPrefixes { get; set; }
    }
}
