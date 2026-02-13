

using Ardalis.Result;
using Cqrs.Messaging;
using Domain.Cars;
using SharedKernel.Database;

namespace Application.Cars.GetByMake;
public class GetCarsByMakeHandler(IReadRepository<Car> repository) : IQueryHandler<GetCarsByMakeQuery, List<CarDto>>
{
    public async Task<Result<List<CarDto>>> HandleAsync(GetCarsByMakeQuery query, CancellationToken cancellationToken)
    {
        var spec = new GetCarDtosByMakeSpec(query.Make);
        return await repository.ListAsync(spec, cancellationToken);
    }
}
