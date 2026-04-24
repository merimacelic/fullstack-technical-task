using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.IntegrationTests.Infrastructure;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Api.IntegrationTests.Tasks;

public class TasksEndpointsTests : IntegrationTestBase
{
    public TasksEndpointsTests(TaskManagementApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateTask_Should_Return_201_And_Persist()
    {
        await AuthenticateAsync();
        var payload = new
        {
            title = "Integration test task",
            description = "Verifies the create endpoint end-to-end",
            priority = "High",
            dueDateUtc = DateTime.UtcNow.AddDays(5),
        };

        var response = await Client.PostAsJsonAsync("/api/tasks", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadJsonAsync<TaskResponse>(response);
        body.Title.ShouldBe("Integration test task");
        body.Status.ShouldBe("Pending");

        var count = await UseDbAsync(db => db.Tasks.CountAsync());
        count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateTask_WithBlankTitle_Should_Return_400()
    {
        await AuthenticateAsync();
        var response = await Client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "", priority = "Low" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTaskById_Unknown_Should_Return_404()
    {
        await AuthenticateAsync();
        var response = await Client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Full_CRUD_Flow_Should_Work()
    {
        await AuthenticateAsync();

        // Create
        var created = await Client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Flow task",
            priority = "Medium",
        });
        created.EnsureSuccessStatusCode();
        var createdTask = await ReadJsonAsync<TaskResponse>(created);

        // Get by id
        var fetched = await Client.GetAsync($"/api/tasks/{createdTask.Id}");
        fetched.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Update
        var updated = await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new
        {
            title = "Flow task updated",
            description = (string?)null,
            priority = "High",
            dueDateUtc = (DateTime?)null,
        });
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync<TaskResponse>(updated)).Title.ShouldBe("Flow task updated");

        // Complete
        var completed = await Client.PatchAsync($"/api/tasks/{createdTask.Id}/complete", null);
        completed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync<TaskResponse>(completed)).Status.ShouldBe("Completed");

        // Filter by status
        var completedList = await Client.GetAsync("/api/tasks?statuses=Completed");
        completedList.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ReadJsonAsync<PagedResult<TaskResponse>>(completedList);
        page.Items.Count.ShouldBe(1);

        // Reopen
        var reopened = await Client.PatchAsync($"/api/tasks/{createdTask.Id}/reopen", null);
        reopened.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync<TaskResponse>(reopened)).Status.ShouldBe("Pending");

        // Delete
        var deleted = await Client.DeleteAsync($"/api/tasks/{createdTask.Id}");
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 404 after delete
        (await Client.GetAsync($"/api/tasks/{createdTask.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthenticated_TaskRequest_Should_Return_401()
    {
        var anon = CreateUnauthenticatedClient();
        var response = await anon.GetAsync("/api/tasks");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tasks_Are_Scoped_Per_User()
    {
        // User A creates a task.
        await AuthenticateAsync();
        var created = await Client.PostAsJsonAsync("/api/tasks", new { title = "A-only", priority = "Low" });
        created.EnsureSuccessStatusCode();
        var task = await ReadJsonAsync<TaskResponse>(created);

        // User B signs in separately.
        var clientB = CreateUnauthenticatedClient();
        await AuthenticateAsync(clientB);

        // User B sees an empty list.
        var listB = await clientB.GetAsync("/api/tasks");
        listB.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync<PagedResult<TaskResponse>>(listB)).Items.Count.ShouldBe(0);

        // User B gets 404, not 403, on A's task (don't leak existence).
        (await clientB.GetAsync($"/api/tasks/{task.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var updateResp = await clientB.PutAsJsonAsync($"/api/tasks/{task.Id}", new
        {
            title = "hijack", description = (string?)null, priority = "High", dueDateUtc = (DateTime?)null,
        });
        updateResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await clientB.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Task still exists in the DB (was not deleted by user B).
        (await UseDbAsync(db => db.Tasks.CountAsync())).ShouldBe(1);
    }
}
