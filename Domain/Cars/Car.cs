using Application.Abstractions.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Cars
{
    public sealed class Car: IAggregateRoot
    {
        public Guid Id { get; private set; }
        public string Make { get; private set; }
        public string Model { get; private set; }
        public int Year { get; private set; }
        public int Mileage { get; private set; }
        public decimal Price { get; private set; }

        public Car(string make, string model, int year, int mileage, decimal price)
        {
            Id = Guid.NewGuid();
            Make = make;
            Model = model;
            Year = year;
            Mileage = mileage;
            Price = price;
        }


    }
}
