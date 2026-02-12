namespace Stickerlandia.PrintService.Core;

public class UnitOfWorkCommandHandler<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> Handle(TCommand command)
    {
        var result = await inner.Handle(command);
        await unitOfWork.CommitAsync();
        return result;
    }
}

public class UnitOfWorkCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand>
{
    public async Task Handle(TCommand command)
    {
        await inner.Handle(command);
        await unitOfWork.CommitAsync();
    }
}
