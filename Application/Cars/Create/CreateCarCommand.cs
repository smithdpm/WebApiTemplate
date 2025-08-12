using Application.Abstractions.Messaging;
using System.Xml.Schema;

namespace Application.Cars.Create;

public sealed class CreateCarCommand : ICommand<Guid>
{
    public required string Model { get; set; }
    public required string Make { get; set; }
    public int Year { get; set; }
    public int Mileage { get; set; }
    public decimal Price { get; set; }

}
