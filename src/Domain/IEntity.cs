namespace Domain;

public interface IEntity<TId>
{
    TId Id { get;  }
}