using AutoSale.Application.Abstractions.Messaging;

namespace AutoSale.Vehicles.Api.UnitTests.Fakes;

internal sealed class StubCommandHandler<TCommand, TResult>(Func<TCommand, TResult> handle) : ICommandHandler<TCommand, TResult>
{
    public Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(handle(command));
}
