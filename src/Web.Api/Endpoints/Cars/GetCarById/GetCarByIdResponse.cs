namespace Web.Api.Endpoints.Cars.GetCarById;

public record GetCarByIdResponse
    (Guid Id,
    string Make,
    string Model,
    int Year,
    int Mileage,
    decimal Price);