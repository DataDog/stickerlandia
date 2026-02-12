namespace Stickerlandia.PrintService.Core;

public interface IUnitOfWork : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
