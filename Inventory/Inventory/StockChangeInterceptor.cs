using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Inventory;

public class StockChangeInterceptor(ILogger<StockChangeInterceptor> logger, EventProducerBase<InventoryEventDto> producer) : ISaveChangesInterceptor
{
    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Intercepting SaveChangesAsync to check for InventoryEntry stock changes.");
        var ctx = eventData.Context;
        if (ctx == null) return result;
        var entries = ctx.ChangeTracker.Entries<InventoryEntry>().Where(e => e.State == EntityState.Modified);

        foreach (var e in entries)
        {
            var oldQty = (int)(e.OriginalValues["AvailableQuantity"])!;
            var newQty = e.Entity.AvailableQuantity;
            logger.LogInformation("InventoryEntry for ProductId {ProductId} changed from {OldQty} to {NewQty}", e.Entity.ProductId, oldQty, newQty);

            switch (oldQty)
            {
                case <= 0 when newQty > 0:
                    await producer.ProduceAsync(new InventoryEventDto() 
                    { 
                        ProductId = e.Entity.ProductId, 
                        Quantity = newQty,
                        EventType = nameof(Events.InventoryInStock),
                        EventId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow
                    }, cancellationToken);
                    break;
                case > 0 when newQty <= 0:
                    await producer.ProduceAsync(new InventoryEventDto() 
                    { 
                        ProductId = e.Entity.ProductId, 
                        Quantity = 0,
                        EventType = nameof(Events.InventoryOutOfStock),
                        EventId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow
                    }, cancellationToken);
                    break;
            }
        }

        return result;
    }
}