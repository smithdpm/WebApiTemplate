
namespace Domain.Abstractions;
public interface IIdGenerator<TId>
{
    public TId NewId();
}
