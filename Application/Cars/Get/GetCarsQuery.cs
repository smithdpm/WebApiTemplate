﻿using Application.Abstractions.Messaging;

namespace Application.Cars.Get;

public sealed record GetCarsQuery(int? Skip, int? Take) : IQuery<List<CarDto>>;
