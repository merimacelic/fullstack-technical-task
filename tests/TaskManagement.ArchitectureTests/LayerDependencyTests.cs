using System.Reflection;
using NetArchTest.Rules;

namespace TaskManagement.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Tasks.TaskItem).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    private const string ApplicationNamespace = "TaskManagement.Application";
    private const string InfrastructureNamespace = "TaskManagement.Infrastructure";
    private const string ApiNamespace = "TaskManagement.Api";

    // Third-party namespace roots we need to keep out of specific layers. Project
    // rules only stop our own layers depending on each other; these rules stop
    // framework types leaking into layers that must stay framework-agnostic.
    private const string AspNetCoreNamespace = "Microsoft.AspNetCore";
    private const string EntityFrameworkCoreNamespace = "Microsoft.EntityFrameworkCore";

    [Fact]
    public void Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Application_Should_Not_DependOn_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Application_Should_Not_DependOn_AspNetCore()
    {
        // Application should be hostable from a console, a worker, or a Blazor app
        // without dragging ASP.NET. If something in Application starts needing
        // HttpContext, route it through an abstraction (see ICurrentUser).
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(AspNetCoreNamespace)
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Domain_Should_Not_DependOn_AspNetCore_Or_EntityFramework()
    {
        // Domain is pure: no HTTP types, no ORM types. Pragmatic exception for
        // Application (which references EF Core for DbSet<T> on the abstraction).
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(AspNetCoreNamespace, EntityFrameworkCoreNamespace)
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Infrastructure_Should_Not_DependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Handlers_Should_BeSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Validators_Should_BeSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        AssertArchitecture(result);
    }

    private static void AssertArchitecture(TestResult result)
    {
        if (result.IsSuccessful)
        {
            return;
        }

        var offending = string.Join(
            Environment.NewLine,
            result.FailingTypeNames ?? []);
        throw new Xunit.Sdk.XunitException(
            $"Architecture rule violated by the following types:{Environment.NewLine}{offending}");
    }
}
