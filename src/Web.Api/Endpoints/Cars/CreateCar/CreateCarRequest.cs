namespace Web.Api.Endpoints.Cars.CreateCar3;

public record CreateCarRequest
    (string Model,
    string Make,
    int Year,
    int Mileage,
    decimal Price);