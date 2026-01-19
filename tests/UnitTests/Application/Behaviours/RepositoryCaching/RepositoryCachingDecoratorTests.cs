using Ardalis.Specification;
using Domain.Cars;
using Infrastructure.IdentityGeneration;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using RepositoryCaching.Cache;
using RepositoryCaching.Configuration;
using RepositoryCaching.Database;
using SharedKernel.Database;
using Shouldly;

namespace UnitTests.Application.Behaviours.RepositoryCaching;

public class RepositoryCachingDecoratorTests
{
    private readonly IRepository<Car> _innerRepository;
    private readonly ICacheService _cacheService;
    private readonly IOptions<RepositoryCacheSettings> _options;
    private readonly CarFactory _carFactory;
    private readonly RepositoryCachingDecorator.CachedRepository<Car> _cachedRepository;
    private readonly RepositoryCacheSettings _cacheSettings;

    public RepositoryCachingDecoratorTests()
    {
        _innerRepository = Substitute.For<IRepository<Car>>();
        _carFactory = new CarFactory(new UuidSqlServerFriendlyGenerator());
        _cacheService = Substitute.For<ICacheService>();
        _cacheSettings = new RepositoryCacheSettings
        {
            Enabled = true,
            DefaultExpirationInMinutes = 5,
            PerEntitySettings = new Dictionary<string, EntityCacheSettings>
            {
                ["Car"] = new EntityCacheSettings { Enabled = true, ExpirationInMinutes = 10 }
            }
        };
        _options = Options.Create(_cacheSettings);
        _cachedRepository = new RepositoryCachingDecorator.CachedRepository<Car>(_innerRepository, _cacheService, _options);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldCallInnerRepository()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        // Act
        await _cachedRepository.AddAsync(car);

        // Assert
        await _innerRepository.Received(1).AddAsync(car, Arg.Any<CancellationToken>());
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_ShouldCallInnerRepository()
    {
        // Arrange
        var cars = new List<Car>
        {
            _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m),
            _carFactory.Create("Honda", "Civic", 2021, 5000, 22000m)
        };

        // Act
        await _cachedRepository.AddRangeAsync(cars);

        // Assert
        await _innerRepository.Received(1).AddRangeAsync(cars, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenCachingEnabled_AndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var cachedCar = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);
        var cacheKey = $"Car-{carId}";

        _cacheService.GetAsync<Car>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(cachedCar);

        // Act
        var result = await _cachedRepository.GetByIdAsync(carId);

        // Assert
        result.ShouldBe(cachedCar);
        await _innerRepository.DidNotReceive().GetByIdAsync(carId, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).GetAsync<Car>(cacheKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenCachingEnabled_AndCacheMiss_ShouldCallInnerRepositoryAndCache()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);
        var cacheKey = $"Car-{carId}";

        _cacheService.GetAsync<Car>(cacheKey, Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.GetByIdAsync(carId, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.GetByIdAsync(carId);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).GetByIdAsync(carId, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync(cacheKey, car, TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenCachingDisabled_ShouldCallInnerRepositoryDirectly()
    {
        // Arrange
        _cacheSettings.Enabled = false;
        var carId = Guid.NewGuid();
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _innerRepository.GetByIdAsync(carId, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.GetByIdAsync(carId);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).GetByIdAsync(carId, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<Car>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region FirstOrDefaultAsync with ISpecification Tests

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpecification_WhenCacheEnabled_AndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("test-cache-key");

        var cachedCars = new List<Car> { _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m) };

        _cacheService.GetAsync<List<Car>>("test-cache-key", Arg.Any<CancellationToken>())
            .Returns(cachedCars);

        // Act
        var result = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(cachedCars.First());
        await _innerRepository.DidNotReceive().ListAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpecification_WhenCacheEnabled_AndCacheMiss_ShouldCallInnerAndCache()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("test-cache-key");

        var cars = new List<Car> { _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m) };

        _cacheService.GetAsync<List<Car>>("test-cache-key", Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.ListAsync(spec, Arg.Any<CancellationToken>())
            .Returns(cars);

        // Act
        var result = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(cars.First());
        await _innerRepository.Received(1).ListAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync("test-cache-key", cars, TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpecification_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();
        spec.CacheEnabled.Returns(false);

        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);
        _innerRepository.FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<List<Car>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region FirstOrDefaultAsync with ISingleResultSpecification Tests

    [Fact]
    public async Task FirstOrDefaultAsync_WithSingleResultSpec_WhenCacheEnabled_AndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("single-result-key");

        var cachedCar = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _cacheService.GetAsync<Car>("single-result-key", Arg.Any<CancellationToken>())
            .Returns(cachedCar);

        // Act
        var result = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(cachedCar);
        await _innerRepository.DidNotReceive().FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSingleResultSpec_WhenCacheEnabled_AndCacheMiss_ShouldCallInnerAndCache()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("single-result-key");

        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _cacheService.GetAsync<Car>("single-result-key", Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync("single-result-key", car, TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSingleResultSpec_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car>>();
        spec.CacheEnabled.Returns(false);

        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);
        _innerRepository.FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<Car>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_WithoutSpecification_ShouldCallInnerRepository()
    {
        // Arrange
        var cars = new List<Car>
        {
            _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m),
            _carFactory.Create("Honda", "Civic", 2021, 5000, 22000m)
        };

        _innerRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(cars);

        // Act
        var result = await _cachedRepository.ListAsync();

        // Assert
        result.ShouldBe(cars);
        await _innerRepository.Received(1).ListAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithSpecification_WhenCacheEnabled_AndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("list-cache-key");

        var cachedCars = new List<Car>
        {
            _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m),
            _carFactory.Create("Honda", "Civic", 2021, 5000, 22000m)
        };

        _cacheService.GetAsync<List<Car>>("list-cache-key", Arg.Any<CancellationToken>())
            .Returns(cachedCars);

        // Act
        var result = await _cachedRepository.ListAsync(spec);

        // Assert
        result.ShouldBe(cachedCars);
        await _innerRepository.DidNotReceive().ListAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithSpecification_WhenCacheEnabled_AndCacheMiss_ShouldCallInnerAndCache()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("list-cache-key");

        var cars = new List<Car>
        {
            _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m),
            _carFactory.Create("Honda", "Civic", 2021, 5000, 22000m)
        };

        _cacheService.GetAsync<List<Car>>("list-cache-key", Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.ListAsync(spec, Arg.Any<CancellationToken>())
            .Returns(cars);

        // Act
        var result = await _cachedRepository.ListAsync(spec);

        // Assert
        result.ShouldBe(cars);
        await _innerRepository.Received(1).ListAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync("list-cache-key", cars, TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithSpecification_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();
        spec.CacheEnabled.Returns(false);

        var cars = new List<Car>
        {
            _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m),
            _carFactory.Create("Honda", "Civic", 2021, 5000, 22000m)
        };

        _innerRepository.ListAsync(spec, Arg.Any<CancellationToken>())
            .Returns(cars);

        // Act
        var result = await _cachedRepository.ListAsync(spec);

        // Assert
        result.ShouldBe(cars);
        await _innerRepository.Received(1).ListAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<List<Car>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SingleOrDefaultAsync Tests

    [Fact]
    public async Task SingleOrDefaultAsync_WithSingleResultSpec_WhenCacheEnabled_AndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("single-cache-key");

        var cachedCar = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _cacheService.GetAsync<Car>("single-cache-key", Arg.Any<CancellationToken>())
            .Returns(cachedCar);

        // Act
        var result = await _cachedRepository.SingleOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(cachedCar);
        await _innerRepository.DidNotReceive().SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleOrDefaultAsync_WithSingleResultSpec_WhenCacheEnabled_AndCacheMiss_ShouldCallInnerAndCache()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("single-cache-key");

        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _cacheService.GetAsync<Car>("single-cache-key", Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.SingleOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync("single-cache-key", car, TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleOrDefaultAsync_WithSingleResultSpec_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car>>();
        spec.CacheEnabled.Returns(false);

        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);
        _innerRepository.SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.SingleOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<Car>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Update and Delete Operations Tests

    [Fact]
    public async Task UpdateAsync_ShouldCallInnerRepository()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _innerRepository.UpdateAsync(car, Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _cachedRepository.UpdateAsync(car);

        // Assert
        result.ShouldBe(1);
        await _innerRepository.Received(1).UpdateAsync(car, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRangeAsync_ShouldCallInnerRepository()
    {
        // Arrange
        var cars = new List<Car>
        {
            _carFactory.Create  ("Toyota", "Camry", 2020, 10000, 25000m),
            _carFactory.Create("Honda", "Civic", 2021, 5000, 22000m)
        };

        _innerRepository.UpdateRangeAsync(cars, Arg.Any<CancellationToken>())
            .Returns(2);

        // Act
        var result = await _cachedRepository.UpdateRangeAsync(cars);

        // Assert
        result.ShouldBe(2);
        await _innerRepository.Received(1).UpdateRangeAsync(cars, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallInnerRepository()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _innerRepository.DeleteAsync(car, Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _cachedRepository.DeleteAsync(car);

        // Assert
        result.ShouldBe(1);
        await _innerRepository.Received(1).DeleteAsync(car, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRangeAsync_WithEntities_ShouldCallInnerRepository()
    {
        // Arrange
        var cars = new List<Car>
        {
            _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m),
            _carFactory.Create("Honda", "Civic", 2021, 5000, 22000m)
        };

        _innerRepository.DeleteRangeAsync(cars, Arg.Any<CancellationToken>())
            .Returns(2);

        // Act
        var result = await _cachedRepository.DeleteRangeAsync(cars);

        // Assert
        result.ShouldBe(2);
        await _innerRepository.Received(1).DeleteRangeAsync(cars, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRangeAsync_WithSpecification_ShouldCallInnerRepository()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();

        _innerRepository.DeleteRangeAsync(spec, Arg.Any<CancellationToken>())
            .Returns(3);

        // Act
        var result = await _cachedRepository.DeleteRangeAsync(spec);

        // Assert
        result.ShouldBe(3);
        await _innerRepository.Received(1).DeleteRangeAsync(spec, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Count and Any Operations Tests

    [Fact]
    public async Task CountAsync_WithSpecification_ShouldCallInnerRepository()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();

        _innerRepository.CountAsync(spec, Arg.Any<CancellationToken>())
            .Returns(5);

        // Act
        var result = await _cachedRepository.CountAsync(spec);

        // Assert
        result.ShouldBe(5);
        await _innerRepository.Received(1).CountAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CountAsync_WithoutSpecification_ShouldCallInnerRepository()
    {
        // Arrange
        _innerRepository.CountAsync(Arg.Any<CancellationToken>())
            .Returns(10);

        // Act
        var result = await _cachedRepository.CountAsync();

        // Assert
        result.ShouldBe(10);
        await _innerRepository.Received(1).CountAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_ShouldCallInnerRepository()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();

        _innerRepository.AnyAsync(spec, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _cachedRepository.AnyAsync(spec);

        // Assert
        result.ShouldBeTrue();
        await _innerRepository.Received(1).AnyAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnyAsync_WithoutSpecification_ShouldCallInnerRepository()
    {
        // Arrange
        _innerRepository.AnyAsync(Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _cachedRepository.AnyAsync();

        // Assert
        result.ShouldBeFalse();
        await _innerRepository.Received(1).AnyAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldCallInnerRepository()
    {
        // Arrange
        _innerRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(3);

        // Act
        var result = await _cachedRepository.SaveChangesAsync();

        // Assert
        result.ShouldBe(3);
        await _innerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Caching Configuration Tests

    [Fact]
    public async Task GetByIdAsync_WhenEntityCachingDisabled_ShouldNotCache()
    {
        // Arrange
        _cacheSettings.PerEntitySettings["Car"].Enabled = false;
        var carId = Guid.NewGuid();
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);

        _innerRepository.GetByIdAsync(carId, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.GetByIdAsync(carId);

        // Assert
        result.ShouldBe(car);
        await _innerRepository.Received(1).GetByIdAsync(carId, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<Car>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithDefaultExpiration_WhenEntityExpirationNotSet()
    {
        // Arrange
        _cacheSettings.PerEntitySettings["Car"].ExpirationInMinutes = null;
        _cacheSettings.DefaultExpirationInMinutes = 15;
        
        var carId = Guid.NewGuid();
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);
        var cacheKey = $"Car-{carId}";

        _cacheService.GetAsync<Car>(cacheKey, Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.GetByIdAsync(carId, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await _cachedRepository.GetByIdAsync(carId);

        // Assert
        result.ShouldBe(car);
        await _cacheService.Received(1).SetAsync(cacheKey, car, TimeSpan.FromMinutes(15), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithNoEntitySpecificSettings_ShouldUseDefaults()
    {
        // Arrange - Create a new cached repository instance with no entity-specific settings
        var emptyCacheSettings = new RepositoryCacheSettings
        {
            Enabled = true,
            DefaultExpirationInMinutes = 5,
            PerEntitySettings = new Dictionary<string, EntityCacheSettings>() // No entity-specific settings
        };
        var emptyOptions = Options.Create(emptyCacheSettings);
        var cachedRepoWithoutEntitySettings = new RepositoryCachingDecorator.CachedRepository<Car>(_innerRepository, _cacheService, emptyOptions);
        
        var carId = Guid.NewGuid();
        var car = _carFactory.Create("Toyota", "Camry", 2020, 10000, 25000m);
        var cacheKey = $"Car-{carId}";

        _cacheService.GetAsync<Car>(cacheKey, Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.GetByIdAsync(carId, Arg.Any<CancellationToken>())
            .Returns(car);

        // Act
        var result = await cachedRepoWithoutEntitySettings.GetByIdAsync(carId);

        // Assert
        result.ShouldBe(car);
        await _cacheService.Received(1).SetAsync(cacheKey, car, TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Generic Result Type Tests

    [Fact]
    public async Task FirstOrDefaultAsync_WithGenericResult_WhenCacheEnabled_AndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car, CarDto>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("generic-result-key");

        var cachedResults = new List<CarDto> 
        { 
            new CarDto(Guid.NewGuid(), "Toyota", "Camry", 2020, 10000, 25000m) 
        };

        _cacheService.GetAsync<List<CarDto>>("generic-result-key", Arg.Any<CancellationToken>())
            .Returns(cachedResults);

        // Act
        var result = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(cachedResults.First());
        await _innerRepository.DidNotReceive().FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithGenericResult_WhenCacheEnabled_AndCacheMiss_ShouldCallInnerAndCache()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car, CarDto>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("generic-list-key");

        var results = new List<CarDto>
        {
            new CarDto(Guid.NewGuid(), "Toyota", "Camry", 2020, 10000, 25000m),
            new CarDto(Guid.NewGuid(), "Honda", "Civic", 2021, 5000, 22000m)
        };

        _cacheService.GetAsync<List<CarDto>>("generic-list-key", Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.ListAsync(spec, Arg.Any<CancellationToken>())
            .Returns(results);

        // Act
        var result = await _cachedRepository.ListAsync(spec);

        // Assert
        result.ShouldBe(results);
        await _innerRepository.Received(1).ListAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync("generic-list-key", results, TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleOrDefaultAsync_WithGenericResult_WhenCacheEnabled_AndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car, CarDto>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("single-generic-key");

        var cachedResult = new CarDto(Guid.NewGuid(), "Toyota", "Camry", 2020, 10000, 25000m);

        _cacheService.GetAsync<CarDto>("single-generic-key", Arg.Any<CancellationToken>())
            .Returns(cachedResult);

        // Act
        var result = await _cachedRepository.SingleOrDefaultAsync(spec);

        // Assert
        result.ShouldBe(cachedResult);
        await _innerRepository.DidNotReceive().SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithGenericResult_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car, CarDto>>();
        spec.CacheEnabled.Returns(false);

        var result = new CarDto(Guid.NewGuid(), "Toyota", "Camry", 2020, 10000, 25000m);
        _innerRepository.FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        var actualResult = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        actualResult.ShouldBe(result);
        await _innerRepository.Received(1).FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<List<CarDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithGenericResult_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car, CarDto>>();
        spec.CacheEnabled.Returns(false);

        var results = new List<CarDto>
        {
            new CarDto(Guid.NewGuid(), "Toyota", "Camry", 2020, 10000, 25000m),
            new CarDto(Guid.NewGuid(), "Honda", "Civic", 2021, 5000, 22000m)
        };

        _innerRepository.ListAsync(spec, Arg.Any<CancellationToken>())
            .Returns(results);

        // Act
        var actualResult = await _cachedRepository.ListAsync(spec);

        // Assert
        actualResult.ShouldBe(results);
        await _innerRepository.Received(1).ListAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<List<CarDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleOrDefaultAsync_WithGenericResult_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car, CarDto>>();
        spec.CacheEnabled.Returns(false);

        var result = new CarDto(Guid.NewGuid(), "Toyota", "Camry", 2020, 10000, 25000m);
        _innerRepository.SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        var actualResult = await _cachedRepository.SingleOrDefaultAsync(spec);

        // Assert
        actualResult.ShouldBe(result);
        await _innerRepository.Received(1).SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<CarDto>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSingleResultSpecGenericResult_WhenCacheDisabled_ShouldCallInnerDirectly()
    {
        // Arrange
        var spec = Substitute.For<ISingleResultSpecification<Car, CarDto>>();
        spec.CacheEnabled.Returns(false);

        var result = new CarDto(Guid.NewGuid(), "Toyota", "Camry", 2020, 10000, 25000m);
        _innerRepository.FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        var actualResult = await _cachedRepository.FirstOrDefaultAsync(spec);

        // Assert
        actualResult.ShouldBe(result);
        await _innerRepository.Received(1).FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<CarDto>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GetByIdAsync_WhenEntityIsNull_ShouldNotCache()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var cacheKey = $"Car-{carId}";

        _cacheService.GetAsync<Car>(cacheKey, Arg.Any<CancellationToken>())
            .ReturnsNull();
        _innerRepository.GetByIdAsync(carId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var result = await _cachedRepository.GetByIdAsync(carId);

        // Assert
        result.ShouldBeNull();
        await _cacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<Car>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithEmptyCachedList_ShouldReturnEmptyList()
    {
        // Arrange
        var spec = Substitute.For<ISpecification<Car>>();
        spec.CacheEnabled.Returns(true);
        spec.CacheKey.Returns("empty-list-key");

        var emptyCachedList = new List<Car>();

        _cacheService.GetAsync<List<Car>>("empty-list-key", Arg.Any<CancellationToken>())
            .Returns(emptyCachedList);

        // Act
        var result = await _cachedRepository.ListAsync(spec);

        // Assert
        result.ShouldBe(emptyCachedList);
        result.ShouldBeEmpty();
        await _innerRepository.DidNotReceive().ListAsync(spec, Arg.Any<CancellationToken>());
    }

    #endregion
}

// DTO for testing generic result types
public record CarDto(Guid Id, string Make, string Model, int Year, int Mileage, decimal Price);