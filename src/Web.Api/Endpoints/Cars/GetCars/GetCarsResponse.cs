namespace Web.Api.Endpoints.Cars.GetCars;

public record GetCarsResponse
(List<Car> Cars);

public record Car
    (Guid Id,
    string Make,
    string Model,
    int Year,
    int Mileage,
    decimal Price
    );