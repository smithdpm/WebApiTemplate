using Application.Abstractions.Messaging;
using System.Globalization;
using System.Xml.Schema;

namespace Application.Cars.Create;

public sealed record CreateCarCommand 
    (
        string Model,
        string Make,
        int Year,
        int Mileage,
        decimal Price
    ): ICommand<Guid>;
