using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;

namespace Function
{
    public static class FlowLogUpdated
    {
        [FunctionName(nameof(FlowLogUpdated))]
        public static void Run(
            [BlobTrigger("%blobContainerName%/resourceId=/SUBSCRIPTIONS/{subId}/RESOURCEGROUPS/{resourceGroup}/PROVIDERS/MICROSOFT.NETWORK/NETWORKSECURITYGROUPS/{nsgName}/y={blobYear}/m={blobMonth}/d={blobDay}/h={blobHour}/m={blobMinute}/macAddress={mac}/PT1H.json", Connection = "nsgSourceDataAccount")] string myBlob, 
            [Table("checkpoints", Connection = "AzureWebJobsStorage")] CloudTable checkpointTable,
            string subId, string resourceGroup, string nsgName, string blobYear, string blobMonth, string blobDay, string blobHour, string blobMinute, string mac,
            ILogger log)
        {
            log.LogInformation($"{nameof(FlowLogUpdated)} triggered on blob\n\tName:/resourceId=/SUBSCRIPTIONS/{subId}/RESOURCEGROUPS/{resourceGroup}/PROVIDERS/MICROSOFT.NETWORK/NETWORKSECURITYGROUPS/{nsgName}/y={blobYear}/m={blobMonth}/d={blobDay}/h={blobHour}/m={blobMinute}/macAddress={mac}/PT1H.json\n\tSize: {myBlob.Length} Bytes");

            // Get the Flow Log version to process
            float version = float.Parse(System.Environment.GetEnvironmentVariable("FlowLogVersion"));

            // Init the blob details
            BlobDetails blobDetails = new BlobDetails(subId, resourceGroup, nsgName, blobYear, blobMonth, blobDay, blobHour, blobMinute, mac);

            // Get the checkpoint for this blob
            Checkpoint checkpoint = Checkpoint.GetCheckpoint(blobDetails, checkpointTable);

            // Calculate the length of data to process
            int index = checkpoint.CheckpointIndex;
            int count = (int)myBlob.Length - index;
            log.LogInformation($"Blob: {blobDetails.ToString()}, index: {index}, number of new bytes: {count}");

            // Grab the new content
            string newContent = myBlob.Substring(index, count);

            // If the index isn't 0, it's appended data, so prepend the records property
            if (index != 0)
                newContent = newContent.Insert(0, "{\"records\":[");

            // Deserialize the new records
            var serializerOptions = new JsonSerializerOptions { Converters = { new FlowLogTupleConverter(version)} };
            FlowLogRecords flowLogRecords = JsonSerializer.Deserialize<FlowLogRecords>(newContent, serializerOptions); 

            // Do something with the data... e.g. send it to a downstream processor (it could be another Azure Function!)

            // Update the checkpoint for this blob
            checkpoint.PutCheckpoint(checkpointTable, index+count-1);
        }
    }

    public class FlowLogRecords
    {
        public FlowLogRecord[] records { get; set; }
    }

    public class FlowLogRecord
    {
        public string time { get; set; }
        public string systemId { get; set; }
        public string macAddress { get; set; }
        public string category { get; set; }
        public string resourceId { get; set; }
        public string operationName { get; set; }
        public FlowLogProperties properties { get; set; }
    }

    public class FlowLogProperties
    {
        public float Version { get; set; }
        public FlowLogFlowsOuter[] flows { get; set; }
    }

    public class FlowLogFlowsOuter
    {
        public string rule { get; set; }
        public FlowLogFlowsInner[] flows { get; set; }
    }

    public class FlowLogFlowsInner
    {
        public string mac { get; set; }
        public FlowLogTuple[] flowTuples { get; set; }
    }

    public class FlowLogTuple
    {
        public float schemaVersion { get; set; }
        public string startTime { get; set; }
        public string sourceAddress { get; set; }
        public string destinationAddress { get; set; }
        public string sourcePort { get; set; }
        public string destinationPort { get; set; }
        public string transportProtocol { get; set; }
        public string deviceDirection { get; set; }
        public string deviceAction { get; set; }

        // version 2 tuple properties
        public string flowState { get; set; }
        public string packetsStoD { get; set; }
        public string bytesStoD { get; set; }
        public string packetsDtoS { get; set; }
        public string bytesDtoS { get; set; }

        public FlowLogTuple(string tuple, float version)
        {
            schemaVersion = version;

            string[] parts = tuple.Split(new char[] { ',' });            
            startTime = parts[0];
            sourceAddress = parts[1];
            destinationAddress = parts[2];
            sourcePort = parts[3];
            destinationPort = parts[4];
            transportProtocol = parts[5];
            deviceDirection = parts[6];
            deviceAction = parts[7];

            if (version >= 2.0)
            {
                flowState = parts[8];
                if (flowState != "B")
                {
                    packetsStoD = (parts[9] == "" ? "0" : parts[9]);
                    bytesStoD = (parts[10] == "" ? "0" : parts[10]);
                    packetsDtoS = (parts[11] == "" ? "0" : parts[11]);
                    bytesDtoS = (parts[12] == "" ? "0" : parts[12]);
                }
            }
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append("rt=").Append((Convert.ToUInt64(startTime) * 1000).ToString());
            result.Append(" src=").Append(sourceAddress);
            result.Append(" dst=").Append(destinationAddress);
            result.Append(" spt=").Append(sourcePort);
            result.Append(" dpt=").Append(destinationPort);
            result.Append(" proto=").Append((transportProtocol == "U" ? "UDP" : "TCP"));
            result.Append(" deviceDirection=").Append((deviceDirection == "I" ? "0" : "1"));
            result.Append(" act=").Append(deviceAction);

            if (schemaVersion >= 2.0)
            {
                // add fields from version 2 schema
                result.Append(" cs2=").Append(flowState);
                result.Append(" cs2Label=FlowState");

                if (flowState != "B")
                {
                    result.Append(" cn1=").Append(packetsStoD);
                    result.Append(" cn1Label=PacketsStoD");
                    result.Append(" cn2=").Append(packetsDtoS);
                    result.Append(" cn2Label=PacketsDtoS");

                    if (deviceDirection == "I")
                    {
                        result.Append(" bytesIn=").Append(bytesStoD);
                        result.Append(" bytesOut=").Append(bytesDtoS);
                    }
                    else
                    {
                        result.Append(" bytesIn=").Append(bytesDtoS);
                        result.Append(" bytesOut=").Append(bytesStoD);
                    }
                }
            }

            return result.ToString();
        }
    }

    public class FlowLogTupleConverter : JsonConverter<FlowLogTuple>
    {
        float version { get; set; }

        public FlowLogTupleConverter(float version = 2) => this.version = version;
        
        public override FlowLogTuple Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new FlowLogTuple(reader.GetString(), this.version);
        }

        public override void Write(Utf8JsonWriter writer, FlowLogTuple flowLogTuple, JsonSerializerOptions options)
        {
            writer.WriteStringValue(flowLogTuple.ToString());
        }
    }
}
