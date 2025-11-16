namespace Ecommerce.Common.Messaging;

public interface IEvent<T>
{
    public T Payload { get; set; }
}