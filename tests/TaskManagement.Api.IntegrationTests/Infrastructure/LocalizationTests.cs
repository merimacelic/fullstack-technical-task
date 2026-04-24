using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.Api.IntegrationTests.Infrastructure;

// Confirms Accept-Language is honoured end-to-end: validators and ProblemDetails
// titles both come back in the requested locale, with the stable Error.Code
// preserved so the client can still map the problem to an i18n key of its own.
// Also verifies that dynamic args embedded in domain errors (ids, limits, names)
// survive the resx template substitution in both locales.
public sealed class LocalizationTests : IntegrationTestBase
{
    public LocalizationTests(TaskManagementApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Accept_Language_En_Returns_English_Validation_Detail()
    {
        await AuthenticateAsync();
        Client.DefaultRequestHeaders.AcceptLanguage.Clear();
        Client.DefaultRequestHeaders.AcceptLanguage.Add(new("en"));

        var response = await Client.PostAsJsonAsync("/api/tasks", new
        {
            title = "",
            priority = "Low",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadJsonAsync<ValidationProblemDetails>(response);
        problem.Title.ShouldBe("Validation error");
        problem.Errors["Title"][0].ShouldBe("Title is required.");
    }

    [Fact]
    public async Task Accept_Language_Mt_Returns_Maltese_Validation_Detail()
    {
        await AuthenticateAsync();
        Client.DefaultRequestHeaders.AcceptLanguage.Clear();
        Client.DefaultRequestHeaders.AcceptLanguage.Add(new("mt"));

        var response = await Client.PostAsJsonAsync("/api/tasks", new
        {
            title = "",
            priority = "Low",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadJsonAsync<ValidationProblemDetails>(response);
        // Maltese resource strings — assert on text unique to the .mt.resx file.
        problem.Title.ShouldBe("Żball ta' validazzjoni");
        problem.Errors["Title"][0].ShouldBe("It-titlu huwa meħtieġ.");
    }

    [Fact]
    public async Task Accept_Language_Mt_Returns_Maltese_NotFound_Detail_With_Id()
    {
        await AuthenticateAsync();
        Client.DefaultRequestHeaders.AcceptLanguage.Clear();
        Client.DefaultRequestHeaders.AcceptLanguage.Add(new("mt"));
        var missingId = Guid.NewGuid();

        var response = await Client.GetAsync($"/api/tasks/{missingId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadJsonAsync<ProblemDetails>(response);
        problem.Title.ShouldBe("Riżors ma nstabx");
        // Localised template plus the dynamic id — proves Metadata["args"] is
        // being substituted into the .mt.resx {0} placeholder.
        problem.Detail.ShouldBe($"Il-biċċa xogħol bl-id '{missingId}' ma nstabitx.");
        problem.Type.ShouldBe("Task.NotFound");
    }

    [Fact]
    public async Task Unsupported_Accept_Language_Falls_Back_To_English()
    {
        await AuthenticateAsync();
        Client.DefaultRequestHeaders.AcceptLanguage.Clear();
        // fr-FR is not in SupportedCultures; en;q=0.1 is, so RequestLocalization
        // should pick English. Even without the en backup the default is English.
        Client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr-FR"));
        Client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.1));
        var missingId = Guid.NewGuid();

        var response = await Client.GetAsync($"/api/tasks/{missingId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadJsonAsync<ProblemDetails>(response);
        problem.Title.ShouldBe("Resource not found");
        problem.Detail.ShouldBe($"Task with id '{missingId}' was not found.");
        problem.Type.ShouldBe("Task.NotFound");
    }
}
