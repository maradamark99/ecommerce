using Microsoft.EntityFrameworkCore;

namespace Inventory;

public class ExpiredReservationsCleanupService(
    ILogger<ExpiredReservationsCleanupService> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(2);
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;

                while (true)
                {
                    var expiredReservations = await dbContext.StockReservations
                        .Where(r => r.ExpiresAt <= now)
                        .OrderBy(r => r.ExpiresAt)
                        .Take(BatchSize)
                        .ToListAsync(stoppingToken);

                    if (expiredReservations.Count == 0)
                        break;

                    await using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);
                    try
                    {
                        foreach (var reservation in expiredReservations)
                        {
                            await dbContext.InventoryEntries
                                .Where(i => i.ProductId == reservation.ProductId)
                                .ExecuteUpdateAsync(i => i
                                        .SetProperty(inv => inv.AvailableQuantity, inv => inv.AvailableQuantity + reservation.Quantity)
                                        .SetProperty(inv => inv.UpdatedAt, inv => DateTime.UtcNow),
                                    cancellationToken: stoppingToken);
                            await dbContext.StockReservations
                                .Where(sr => sr.OrderId == reservation.OrderId && sr.ProductId == reservation.ProductId)
                                .ExecuteDeleteAsync(stoppingToken);
                        }
                        await dbContext.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    } catch
                    {
                        await transaction.RollbackAsync(stoppingToken);
                    }
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
               logger.LogWarning("Error releasing expired reservations: {ExMessage}", ex.Message);
            }
        }
    }
}