using System;
using Microsoft.Azure.Cosmos.Table;

namespace Function
{
    public class Checkpoint : TableEntity
    {
        public int CheckpointIndex { get; set; }  // index of the last processed block list item

        public Checkpoint()
        {
        }

        public Checkpoint(string partitionKey, string rowKey, int index)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            CheckpointIndex = index;
        }

        public static Checkpoint GetCheckpoint(BlobDetails blobDetails, CloudTable checkpointTable, bool createNew = true)
        {
            TableOperation operation = TableOperation.Retrieve<Checkpoint>(blobDetails.GetPartitionKey(), blobDetails.GetRowKey());
            TableResult result = checkpointTable.ExecuteAsync(operation).Result;

            Checkpoint checkpoint = (Checkpoint)result.Result;
            if (checkpoint == null && createNew)
            {
                // Exiting checkpoint doesn't exist - create a new checkpoint
                checkpoint = new Checkpoint(blobDetails.GetPartitionKey(), blobDetails.GetRowKey(), 0);
            }

            return checkpoint;
        }

        public void PutCheckpoint(CloudTable checkpointTable, int index)
        {
            CheckpointIndex = index;
            checkpointTable.ExecuteAsync(TableOperation.InsertOrReplace(this)).Wait();
        }

        public void DeleteCheckpoint(CloudTable checkpointTable)
        {
            checkpointTable.ExecuteAsync(TableOperation.Delete(this)).Wait();
        }
    }
}
