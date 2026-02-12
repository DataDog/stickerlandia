namespace Stickerlandia.PrintService.Core;

public class UnitOfWorkCommandHandler<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await inner.Handle(command, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            unitOfWork.MarkFaulted();
            throw;
        }
    }
}

public class UnitOfWorkCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand>
{
    public async Task Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            await inner.Handle(command, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            unitOfWork.MarkFaulted();
            throw;
        }
    }
}
