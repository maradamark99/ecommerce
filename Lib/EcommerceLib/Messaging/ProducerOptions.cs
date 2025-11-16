namespace EcommerceLib.Messaging;

public class ProducerOptions
{
    public string BootstrapServers { get; set; }
    public string Topic { get; set; }
    
    public string Acks { get; set; }
    
    public int MessageSendMaxRetries { get; set; }
    
    public bool EnableIdempotence { get; set; }
}