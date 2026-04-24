using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Reqnroll;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Application.Users.Responses;

namespace TaskManagement.Api.IntegrationTests.Acceptance;

[Binding]
public sealed class TaskLifecycleSteps
{
    // One HttpClient per scenario instance — Reqnroll constructs a fresh
    // binding class for each scenario, so auth state cannot leak across
    // scenarios through this field.
    private readonly HttpClient _client = AcceptanceTestHooks.Factory.CreateClient();
    private HttpResponseMessage? _lastResponse;

    [Given("I am a registered and authenticated user")]
    public async Task RegisterAndAuthenticate()
    {
        var email = $"user-{Guid.NewGuid():N}@icon.test";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Passw0rd!",
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Register response was empty.");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    [Given("I discard my authentication")]
    public void DiscardAuthentication()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [When("I create a task titled {string}")]
    public async Task CreateTask(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title,
            priority = "Medium",
        });
        response.EnsureSuccessStatusCode();
    }

    [When("I mark the task titled {string} as complete")]
    public async Task MarkTaskComplete(string title)
    {
        var page = await FetchTasksAsync();
        var target = page.Items.FirstOrDefault(t =>
            string.Equals(t.Title, title, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"No task titled '{title}' found.");

        var response = await _client.PatchAsync($"/api/tasks/{target.Id}/complete", content: null);
        response.EnsureSuccessStatusCode();
    }

    [When("I request the task list")]
    public async Task RequestTaskList()
    {
        _lastResponse = await _client.GetAsync("/api/tasks");
    }

    [Then("the task list contains a task titled {string} with status {string}")]
    public async Task AssertTaskWithStatus(string title, string expectedStatus)
    {
        var page = await FetchTasksAsync();
        var match = page.Items.FirstOrDefault(t =>
            string.Equals(t.Title, title, StringComparison.Ordinal));

        if (match is null)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected a task titled '{title}' but the list contained: " +
                string.Join(", ", page.Items.Select(t => $"'{t.Title}'")));
        }

        if (!string.Equals(match.Status, expectedStatus, StringComparison.Ordinal))
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected '{title}' to be in status '{expectedStatus}' but was '{match.Status}'.");
        }
    }

    [Then("the response status code is {int}")]
    public void AssertResponseStatus(int expected)
    {
        if (_lastResponse is null)
        {
            throw new InvalidOperationException("No HTTP response was captured.");
        }

        if ((int)_lastResponse.StatusCode != expected)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected status {expected} but got {(int)_lastResponse.StatusCode} ({_lastResponse.StatusCode}).");
        }

        // Sanity check for the 401 path specifically — a misconfigured suite
        // could return 401 for unrelated reasons (bad URL, bad base URL).
        if (expected == (int)HttpStatusCode.Unauthorized)
        {
            _lastResponse.Headers.WwwAuthenticate.ShouldNotBeEmpty();
        }
    }

    private async Task<PagedResult<TaskResponse>> FetchTasksAsync()
    {
        var response = await _client.GetAsync("/api/tasks");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<TaskResponse>>()
            ?? throw new InvalidOperationException("Task list response was empty.");
    }
}
