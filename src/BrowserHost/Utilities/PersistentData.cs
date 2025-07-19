namespace BrowserHost.Utilities;

public class PersistentData
{
    public int Version { get; set; }
}

public sealed class PersistentData<TData> : PersistentData
{
    public required TData Data { get; set; }
}
