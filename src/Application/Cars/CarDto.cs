namespace Application.Cars;

public record CarDto
    (Guid Id,
    string Make,
    string Model,
    int Year,
    int Mileage,
    decimal Price);