using System.Text.Json;
using System.Collections.Generic;

namespace Function
{
    public class Response
    {
        public string MacAddress { get; set; }
        public string IpAddress { get; set; }
        public string SubscriptionId { get; set; }
        public string VirtualMachine { get; set; }
        public string VirtualNetwork { get; set; }
        public string Subnet { get; set; }
        public string ResourceGroup { get; set; }
        public string Location { get; set; }
        public IList<string> AvailabilityZones { get; set; }

        public string ToJSON()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}