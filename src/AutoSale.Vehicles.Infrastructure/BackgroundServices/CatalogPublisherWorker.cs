using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Catalog.PublishPending;
using AutoSale.SharedKernel.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoSale.Infrastructure.BackgroundServices;

public sealed class CatalogPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CatalogPublisherOptions _options;
    private readonly ILogger<CatalogPublisherWorker> _logger;
    private readonly string _workerId;

    public CatalogPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CatalogPublisherOptions> options,
        ILogger<CatalogPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _workerId = string.IsNullOrWhiteSpace(_options.WorkerId)
            ? $"{Environment.MachineName}-{Environment.ProcessId}"
            : _options.WorkerId.Trim();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Catalog publisher is disabled");
            return;
        }

        ValidateOptions();
        var pollInterval = TimeSpan.FromSeconds(_options.PollIntervalSeconds);
        var leaseDuration = TimeSpan.FromSeconds(_options.LeaseDurationSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<
                    ICommandHandler<PublishPendingCatalogCommand, Result<CatalogPublishSummary>>>();
                var result = await handler.HandleAsync(
                    new PublishPendingCatalogCommand(_workerId, _options.BatchSize, leaseDuration),
                    stoppingToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning("Catalog publisher cycle failed with code {ErrorCode}", result.Error.Code);
                }
                else if (result.Value!.Claimed > 0)
                {
                    _logger.LogInformation(
                        "Catalog publisher processed {Claimed} items: {Published} published and {Failed} deferred",
                        result.Value.Claimed,
                        result.Value.Published,
                        result.Value.Failed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Catalog publisher cycle failed unexpectedly");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private void ValidateOptions()
    {
        if (_options.BatchSize is < 1 or > 100 ||
            _options.PollIntervalSeconds <= 0 ||
            _options.LeaseDurationSeconds <= 0)
        {
            throw new InvalidOperationException("CatalogPublisher configuration contains invalid intervals or batch size.");
        }
    }
}
