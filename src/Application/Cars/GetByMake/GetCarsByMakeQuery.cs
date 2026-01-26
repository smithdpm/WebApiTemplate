
using Cqrs.Messaging;

namespace Application.Cars.GetByMake;
public record GetCarsByMakeQuery(string Make): IQuery<List<CarDto>>;
