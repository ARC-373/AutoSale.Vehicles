using System.Data;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Domain.Catalog;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Catalog.PublishPending;

public sealed class PublishPendingCatalogHandler : ICommandHandler<PublishPendingCatalogCommand, Result<CatalogPublishSummary>>
{
    private const int MaximumBatchSize = 100;
    private const int MaximumRetryDelaySeconds = 300;
    private readonly ICatalogOutboxRepository _outboxRepository;
    private readonly ISalesCatalogClient _salesCatalogClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishPendingCatalogHandler(
        ICatalogOutboxRepository outboxRepository,
        ISalesCatalogClient salesCatalogClient,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _outboxRepository = outboxRepository;
        _salesCatalogClient = salesCatalogClient;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CatalogPublishSummary>> HandleAsync(
        PublishPendingCatalogCommand command,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<CatalogPublishSummary>(validation.Error);
        }

        var leasedItems = await AcquireBatchAsync(command, cancellationToken);
        var published = 0;
        var failed = 0;

        foreach (var item in leasedItems)
        {
            var publication = await PublishAsync(item, cancellationToken);

            if (publication.IsSuccess)
            {
                item.MarkProcessed(_clock.UtcNow);
                published++;
            }
            else
            {
                var nextAttemptAt = _clock.UtcNow.AddSeconds(CalculateRetryDelaySeconds(item.Attempts));
                item.MarkFailed(publication.Error.Code, nextAttemptAt);
                failed++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new CatalogPublishSummary(leasedItems.Count, published, failed));
    }

    private async Task<Result> PublishAsync(CatalogOutbox item, CancellationToken cancellationToken)
    {
        try
        {
            return await _salesCatalogClient.UpsertVehicleAsync(
                item.VehicleId,
                item.PayloadJson,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Result.Failure(ApplicationErrors.CatalogPublicationFailed);
        }
    }

    private async Task<IReadOnlyList<CatalogOutbox>> AcquireBatchAsync(
        PublishPendingCatalogCommand command,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var now = _clock.UtcNow;
        var items = await _outboxRepository.GetPendingAsync(now, command.BatchSize, cancellationToken);

        foreach (var item in items)
        {
            var lease = item.AcquireLease(command.WorkerId, now.Add(command.LeaseDuration), now);
            if (lease.IsFailure)
            {
                throw new InvalidOperationException($"Unable to lease catalog item: {lease.Error.Code}");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return items;
    }

    private static Result Validate(PublishPendingCatalogCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.WorkerId))
        {
            return Result.Failure(ApplicationErrors.InvalidWorkerId);
        }

        if (command.BatchSize is < 1 or > MaximumBatchSize)
        {
            return Result.Failure(ApplicationErrors.InvalidBatchSize);
        }

        return command.LeaseDuration <= TimeSpan.Zero
            ? Result.Failure(ApplicationErrors.InvalidLeaseDuration)
            : Result.Success();
    }

    private static int CalculateRetryDelaySeconds(int attempts)
    {
        var exponent = Math.Min(attempts, 8);
        return Math.Min(1 << exponent, MaximumRetryDelaySeconds);
    }
}
