using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Azure.Identity;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;

namespace Function
{
    public static class AzureOperations
    {
        [FunctionName("GetDetailsfromMACandIP")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Grab the mac address for which we want to return the VM details
            string macAddress = req.Query["macaddress"];
            string ipAddress = req.Query["ipaddress"];

            log.LogInformation($"Looking for MAC: '{macAddress}' and IP: '{ipAddress}'...");

            // Grab the Azure subscription and resource group. Both these values really should be passed to the function... 
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string resourceGroup = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP");

            // Initialize the response
            bool bFound = false;
            Response response = new Response
            {
                IpAddress = ipAddress,
                MacAddress = macAddress,
                ResourceGroup = resourceGroup,
                SubscriptionId = subscriptionId
            };

            // First, lookup the cache
            // TODO: Use Azure Table Storage to store previously found details for the mac and ip combination. If found, return here

            // Authenticate to Azure. See here for details on the authn attempt order: https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            var credential = new DefaultAzureCredential(); 

            // Enumerate through each of the network interfaces for this subscription until we find NIC with the MAC and IP of interest
            var networkManagementClient = new NetworkManagementClient(subscriptionId, credential);
            var networkInterfaces = networkManagementClient.NetworkInterfaces;
            IEnumerator<NetworkInterface> networkInterfacesEnumerator = networkInterfaces.List(resourceGroup).GetEnumerator();
            while (!bFound && networkInterfacesEnumerator.MoveNext())
            {
                NetworkInterface networkInterface = networkInterfacesEnumerator.Current;
                if (networkInterface.MacAddress.Replace("-", String.Empty).Equals(macAddress))
                {
                    foreach (NetworkInterfaceIPConfiguration ipConfiguration in networkInterface.IpConfigurations)
                    {
                        if (ipConfiguration.PrivateIPAddress.Equals(ipAddress))
                        {
                            bFound = true; 

                            // Set the location (i.e. region)
                            response.Location = networkInterface.Location;

                            // Set the VM-related details
                            {
                                string vmId = networkInterface.VirtualMachine.Id;
                                string vmName = vmId.Substring(vmId.LastIndexOf("/")+1);
                                var vmManagementClient = new ComputeManagementClient(subscriptionId, credential);
                                var virtualMachines = vmManagementClient.VirtualMachines;
                                VirtualMachine virtualMachine = await virtualMachines.GetAsync(resourceGroup, vmName);
                                response.VirtualMachine = virtualMachine.Name;
                                response.AvailabilityZones = virtualMachine.Zones;
                            }

                            // Set the VNET and Subnet details, using the subnet id
                            {
                                // Format: "/subscriptions/<xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx>/resourceGroups/<resource-group>/providers/Microsoft.Network/virtualNetworks/<vnet>/subnets/<subnet>"
                                string subnetId = ipConfiguration.Subnet.Id;
                                response.Subnet = subnetId.Substring(subnetId.LastIndexOf("/")+1);
                                string marker1 = "/virtualNetworks/";
                                string marker2 = "/subnets/";
                                int startIndex = subnetId.IndexOf(marker1)+marker1.Length;
                                int endIndex = subnetId.IndexOf(marker2, startIndex);
                                response.VirtualNetwork = subnetId.Substring(startIndex, endIndex-startIndex);
                            }

                            break;
                        }
                    }
                }
            }

            if (bFound) 
            {
                log.LogInformation($"Found: {response.ToJSON()}");
                return new OkObjectResult(response); 
            }
            else
            {
                log.LogInformation($"Not found: {response.ToJSON()}");
                return new NotFoundResult();
            }
        }
    }
}
