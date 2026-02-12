namespace Stickerlandia.PrintService.Core;

public class NoOpUnitOfWork : IUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void MarkFaulted() { }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
