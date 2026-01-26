namespace SharedKernel.Abstractions;
public interface IEntityFactory<T, TId> where T : IEntity<TId>;
