

using Ardalis.Result;
using Domain.Cars;
using SharedKernel.Database;
using SharedKernel.Messaging;

namespace Application.Cars.GetByMake;
public class GetCarsByMakeHandler(IReadRepository<Car> repository) : IQueryHandler<GetCarsByMakeQuery, List<CarDto>>
{
    public async Task<Result<List<CarDto>>> Handle(GetCarsByMakeQuery query, CancellationToken cancellationToken)
    {
        var spec = new GetCarDtosByMakeSpec(query.Make);
        return await repository.ListAsync(spec, cancellationToken);
    }
}
