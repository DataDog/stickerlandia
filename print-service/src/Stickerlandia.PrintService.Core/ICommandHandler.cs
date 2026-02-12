namespace Stickerlandia.PrintService.Core;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> Handle(TCommand command);
}

public interface ICommandHandler<in TCommand>
{
    Task Handle(TCommand command);
}
