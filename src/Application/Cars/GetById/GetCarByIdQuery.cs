using Cqrs.Operations.Queries;

namespace Application.Cars.GetById;

public record GetCarByIdQuery (Guid CarId) : IQuery<CarDto>;

