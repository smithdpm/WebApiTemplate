using Cqrs.Operations.Queries;

namespace Application.Cars.GetByMake;
public record GetCarsByMakeQuery(string Make): IQuery<List<CarDto>>;
