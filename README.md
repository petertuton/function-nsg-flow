# function-nsg-flow

Requires environment variables for the following: <br>
```
"AzureWebJobsStorage"
- default variable used for Azure Functions - the code uses this storage account to save the checkpoints
```
```
"nsgSourceDataAccount"
- connection string for blob container with NSG Flow logs
```
```
"blobContainerName"
- name of blob container with NSG Flow logs
```
```
"FlowLogVersion"
- version of flow logs
```