namespace SharedKernel.Abstractions;

public interface IEntity<TId>
{
    TId Id { get;  }
}