using Application.Abstractions.Messaging;

namespace Application.Cars.GetById;

public record GetCarByIdQuery (Guid CarId) : IQuery<CarDto>;

