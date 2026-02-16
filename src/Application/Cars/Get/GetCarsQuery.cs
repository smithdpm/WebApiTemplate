using Cqrs.Operations.Queries;

namespace Application.Cars.Get;

public sealed record GetCarsQuery(int? Skip, int? Take) : IQuery<List<CarDto>>;
