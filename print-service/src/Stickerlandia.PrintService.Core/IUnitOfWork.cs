namespace Stickerlandia.PrintService.Core;

public interface IUnitOfWork : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the unit of work as faulted due to a handler exception.
    /// This distinguishes expected failures (handler threw) from unexpected ones
    /// (CommitAsync never called) when logging during disposal.
    /// </summary>
    void MarkFaulted();
}
