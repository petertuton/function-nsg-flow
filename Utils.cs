public static class Utils
{    public static string ExtractResourceGroupFromId(string Id)
    {
        // Format: "/subscriptions/<xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx>/resourceGroups/<resource-group>/providers/Microsoft.Compute/virtualmachines/<vm-name>"
        string resourceName = Id.Substring(Id.LastIndexOf("/")+1);
        string marker1 = "/resourceGroups/";
        string marker2 = "/providers/";
        int startIndex = Id.IndexOf(marker1)+marker1.Length;
        int endIndex = Id.IndexOf(marker2, startIndex);
        return Id.Substring(startIndex, endIndex-startIndex);
    }

    public static string ExtractNameFromId(string Id)
    {
        // Format: "/subscriptions/<xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx>/resourceGroups/<resource-group>/providers/<provider>/<model>/<name>"
        return Id.Substring(Id.LastIndexOf("/")+1);
    }
}