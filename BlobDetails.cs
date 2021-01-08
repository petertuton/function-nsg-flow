using System;
using System.Collections.Generic;
using System.Text;

namespace Function
{
    public class BlobDetails
    {
        //const string macPath = "insights-logs-networksecuritygroupflowevent/resourceId=   0, 1
        //SUBSCRIPTIONS/{subId} 2, 3
        //RESOURCEGROUPS/{resourceGroup} 4, 5
        //PROVIDERS/MICROSOFT.NETWORK 6, 7
        //NETWORKSECURITYGROUPS/{nsgName}  8, 9
        //y={blobYear}  10
        //m={blobMonth} 11
        //d={blobDay} 12
        //h={blobHour} 13
        //m={blobMinute} 14
        //macAddress={mac} 15
        //PT1H.json";
        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public string NsgName { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public string Day { get; set; }
        public string Hour { get; set; }
        public string Minute { get; set; }
        public string Mac { get; set; }

        public BlobDetails(string path, int offset = 0)
        {
            var parts = path.Split('/');

            SubscriptionId = parts[3+offset];
            ResourceGroupName = parts[5+offset];
            NsgName = parts[9+offset];
            Year = parts[10+offset].Split('=')[1];
            Month = parts[11+offset].Split('=')[1];
            Day = parts[12+offset].Split('=')[1];
            Hour = parts[13+offset].Split('=')[1];
            Minute = parts[14+offset].Split('=')[1];
            Mac = parts[15+offset].Split('=')[1];
        }

        public BlobDetails(string subscriptionId, string resourceGroupName, string nsgName, string year, string month, string day, string hour, string minute, string mac)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            NsgName = nsgName;
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Mac = mac;
        }

        public BlobDetails(string subscriptionId, string resourceGroupName, string nsgName, string year, string month, string day, string hour, string minute)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            NsgName = nsgName;
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Mac = "none";
        }

        public string GetPartitionKey()
        {
            return string.Format("{0}_{1}_{2}_{3}", SubscriptionId.Replace("-", "_"), ResourceGroupName, NsgName, Mac);
        }

        public string GetRowKey()
        {
            return string.Format("{0}_{1}_{2}_{3}_{4}", Year, Month, Day, Hour, Minute);
        }

        public override string ToString()
        {
            return string.Format("{0}_{1}_{2}_{3}", ResourceGroupName, NsgName, Day, Hour);
        }
    }
}