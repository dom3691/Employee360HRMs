using Employee360.Application;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Employee360.UnitTests.Application;

/// <summary>
/// Smoke tests verifying the Application layer composition root wires up
/// MediatR, FluentValidation, and AutoMapper correctly.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterMediator()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        using var provider = services.BuildServiceProvider();

        provider.GetService<IMediator>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ShouldRegisterAutoMapper()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        using var provider = services.BuildServiceProvider();

        provider.GetService<AutoMapper.IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ShouldRegisterValidationPipelineBehaviour()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType != null &&
            d.ImplementationType.Name.Contains("ValidationBehaviour"));
    }
}
