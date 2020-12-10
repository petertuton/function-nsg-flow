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
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;

namespace Function
{
    public static class VirtualMachinesFunction
    {
        [FunctionName("VirtualMachines")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Grab the Azure subscription id from the query string
            string subscriptionId = req.Query["subscriptionId"];
            log.LogInformation($"Virtual Machines for subscription: '{subscriptionId}'");

            // Initialize the response
            VMs response = new VMs
            {
                SubscriptionId = subscriptionId,
                VirtualMachines = new List<VirtualMachine>()
            };

            // Authenticate to Azure. See here for details on the authn attempt order: https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            var credential = new DefaultAzureCredential(); 

            // Iterate through each virtual machine in this subscription
            ComputeManagementClient vmManagementClient = new ComputeManagementClient(subscriptionId, credential);
            NetworkManagementClient networkManagementClient = new NetworkManagementClient(subscriptionId, credential);
            await foreach (var virtualMachine in vmManagementClient.VirtualMachines.ListAllAsync())
            {
                // Extract the VM's resource group name, used to lookup the NICs for the VM
                string resourceGroup = Utils.ExtractResourceGroupFromId(virtualMachine.Id);

                // Interate through the VM's network interfaces. NOTE: the value stored in the VM is just a _reference_ to the NIC
                List<NetworkInterface> networkInterfaces = new List<NetworkInterface>();
                foreach (var networkInterfaceReference in virtualMachine.NetworkProfile.NetworkInterfaces)
                {
                    // Get the actual Network Interface from the NIC reference
                    Azure.ResourceManager.Network.Models.NetworkInterface networkInterface = await networkManagementClient.NetworkInterfaces.GetAsync(resourceGroup, Utils.ExtractNameFromId(networkInterfaceReference.Id));

                    // Iterate through the NIC's IP configurations
                    IList<NetworkInterfaceIPConfiguration> ipConfigurations = new List<NetworkInterfaceIPConfiguration>();
                    foreach (var ipConfiguration in networkInterface.IpConfigurations)
                    {
                        // Get the assigned public IP address. NOTE: the value stored in the ipConfig is just a _reference_ to the public IP
                        Azure.ResourceManager.Network.Models.PublicIPAddress publicIpAddress = null;
                        if (ipConfiguration.PublicIPAddress != null)
                        {
                            publicIpAddress = await networkManagementClient.PublicIPAddresses.GetAsync(Utils.ExtractResourceGroupFromId(ipConfiguration.PublicIPAddress.Id), Utils.ExtractNameFromId(ipConfiguration.PublicIPAddress.Id));
                        }

                        // Get the ipaddresses
                        ipConfigurations.Add(new NetworkInterfaceIPConfiguration
                        {
                            PrivateIPAddress = ipConfiguration.PrivateIPAddress,
                            PublicIPAddress = publicIpAddress?.IpAddress
                        });
                        
                    }
                    networkInterfaces.Add(new NetworkInterface
                    {
                        Name = networkInterface.Name,
                        MacAddress = networkInterface.MacAddress,
                        IPConfigurations = ipConfigurations
                    });
                }

                // Iterate through the zones, adding the location name as a prefix
                IList<string> zones = new List<string>();
                foreach (var zone in virtualMachine.Zones)
                    zones.Add($"{virtualMachine.Location}-{zone}");

                // Add the VM's details to the response
                response.VirtualMachines.Add(new VirtualMachine
                {
                    Name = virtualMachine.Name,
                    Region = virtualMachine.Location,
                    NetworkInterfaces = networkInterfaces,
                    AvailabilityZones = zones
                });
            }

            log.LogInformation($"Virtual Machines: {JsonSerializer.Serialize(response)}");
            return new OkObjectResult(response);
        }
    }

    public class VMs
    {
        public string SubscriptionId { get; set; }
        public IList<VirtualMachine> VirtualMachines { get; set; }
    }

    public class VirtualMachine
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public IList<string> AvailabilityZones { get; set; }
        public IList<NetworkInterface> NetworkInterfaces { get; set; }
    }

    public class NetworkInterface
    {
        public string Name { get; set; }
        public string MacAddress { get; set; }
        public IList<NetworkInterfaceIPConfiguration> IPConfigurations { get; set; }
    }

    public class NetworkInterfaceIPConfiguration
    {
        public string PrivateIPAddress { get; set; }
        public string PublicIPAddress { get; set; }
    }
}
