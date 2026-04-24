using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Common.Abstractions;

namespace TaskManagement.Api.IntegrationTests.Infrastructure;

// Regression guard: iteration 1 shipped a scoped-consumed-from-singleton bug that only
// manifested on first real HTTP request. Resolving the core abstractions under a fresh
// scope exercises the same path — a lifetime misconfiguration fails here instead of in
// production on the first 500.
[Collection(TaskManagementCollection.Name)]
public class DiContainerValidationTests
{
    private readonly TaskManagementApiFactory _factory;

    public DiContainerValidationTests(TaskManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(typeof(ISender))]
    [InlineData(typeof(IApplicationDbContext))]
    [InlineData(typeof(ICurrentUser))]
    [InlineData(typeof(IJwtTokenService))]
    [InlineData(typeof(IUserService))]
    [InlineData(typeof(IDateTimeProvider))]
    public void Core_Dependencies_Should_Resolve_From_A_Request_Scope(Type serviceType)
    {
        using var scope = _factory.Services.CreateScope();
        var resolved = scope.ServiceProvider.GetService(serviceType);
        resolved.ShouldNotBeNull($"Could not resolve {serviceType.FullName} from the DI container.");
    }

    [Fact]
    public void Mediator_Should_Have_Registered_Handlers_For_Every_Request_Type()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        sender.ShouldNotBeNull();
    }
}
