namespace EcommerceLib.Messaging;

public class ConsumerOptions
{
    public string BootstrapServers { get; set; }
    public string Topic { get; set; }
    public string GroupId { get; set; }
    public string AutoOffsetReset { get; set; } = "Earliest";
    public bool EnableAutoCommit { get; set; } = false;
    public bool AllowAutoCreateTopics { get; set; } = true;
    public int? SessionTimeoutMs { get; set; }
    public int? MaxPollIntervalMs { get; set; }
    public string? SecurityProtocol { get; set; }
}