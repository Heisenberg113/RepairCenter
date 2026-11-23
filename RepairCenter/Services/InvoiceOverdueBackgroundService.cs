using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public sealed class InvoiceOverdueBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<InvoiceOverdueBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateOverdueAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await UpdateOverdueAsync(stoppingToken);
    }

    private async Task UpdateOverdueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
            var updated = await billing.MarkOverdueInvoicesAsync(DateTime.UtcNow);
            if (updated > 0) logger.LogInformation("Marked {InvoiceCount} invoices as overdue.", updated);
            var warrantyWorkflow = scope.ServiceProvider.GetRequiredService<IWarrantyWorkflowService>();
            var expired = await warrantyWorkflow.MarkExpiredWarrantiesAsync(DateTime.UtcNow.Date);
            if (expired > 0) logger.LogInformation("Marked {WarrantyCount} warranties as expired.", expired);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Failed to update overdue invoice statuses.");
        }
    }
}
