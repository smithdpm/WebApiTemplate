using Ardalis.Result;
using Domain.Cars.Events;
using SharedKernel.Database;

namespace Domain.Cars
{
    public sealed class Car: Entity<Guid>, IAggregateRoot
    {
        public string Make { get; private set; }
        public string Model { get; private set; }
        public int Year { get; private set; }
        public int Mileage { get; private set; }
        public decimal Price { get; private set; }

        public bool IsSold { get; private set; } = false;
        public DateTime? SoldAt { get; private set; }
        public decimal? SoldPrice { get; private set; }

        public Car(string make, string model, int year, int mileage, decimal price)
            : base(Guid.NewGuid())
        {
            Make = make;
            Model = model;
            Year = year;
            Mileage = mileage;
            Price = price;
        }

        public Result SellCar(decimal soldPrice)
        {
            if (IsSold)
                return Result.Error($"Car has been sold already.");
            
            IsSold = true;
            SoldAt = DateTime.UtcNow;
            SoldPrice = soldPrice;
            
            AddDomainEvent(new CarSoldEvent(Id, SoldAt.Value, soldPrice));

            return Result.Success();
        }
    }
}
