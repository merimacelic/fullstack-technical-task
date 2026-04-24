using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.IntegrationTests.Infrastructure;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Api.IntegrationTests.Tasks;

// Concurrency probe for the reorder pipeline.
//
// Reorder computes a new OrderKey as the midpoint of (prev, next) in a separate
// scope from SaveChanges, with no app-level lock per owner, no optimistic
// concurrency token on TaskItem, and no unique index on (OwnerId, OrderKey).
// Two clients dropping different tasks into the same gap therefore both compute
// the same midpoint and both commit it — the user's list ends up with two tasks
// at the same OrderKey, ordering undefined.
//
// The test below documents the expected post-condition (distinct OrderKeys per
// owner after concurrent reorders) and exercises the race in-process against a
// real SQL Server via Testcontainers. If it fails, the reorder pipeline needs
// one of: a unique index on (OwnerId, OrderKey) with retry-on-conflict, an
// owner-scoped advisory lock, optimistic concurrency on TaskItem, or wrapping
// the read+write in SERIALIZABLE.
public class TasksConcurrencyTests : IntegrationTestBase
{
    public TasksConcurrencyTests(TaskManagementApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ConcurrentReorder_IntoSameGap_Should_PreserveDistinctOrderKeys()
    {
        await AuthenticateAsync();

        var a = await CreateTaskAsync("A");
        var b = await CreateTaskAsync("B");
        var c = await CreateTaskAsync("C");
        var d = await CreateTaskAsync("D");

        // Fire both reorders into the (A, B) gap at the same instant. Each request
        // gets its own DbContext scope; the race is between the two SaveChanges
        // calls landing on the same row family.
        var moveC = ReorderAsync(c.Id, previousTaskId: a.Id, nextTaskId: b.Id);
        var moveD = ReorderAsync(d.Id, previousTaskId: a.Id, nextTaskId: b.Id);

        await Task.WhenAll(moveC, moveD);

        (await moveC).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await moveD).StatusCode.ShouldBe(HttpStatusCode.OK);

        var keys = await UseDbAsync(db => db.Tasks
            .OrderBy(t => t.OrderKey)
            .Select(t => t.OrderKey)
            .ToListAsync());

        keys.Count.ShouldBe(4);
        keys.Distinct().Count().ShouldBe(
            4,
            customMessage: "concurrent reorders into the same gap produced duplicate OrderKeys — list ordering is now ambiguous");
    }

    private async Task<TaskResponse> CreateTaskAsync(string title)
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new { title, priority = "Low" });
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync<TaskResponse>(response);
    }

    private Task<HttpResponseMessage> ReorderAsync(Guid id, Guid? previousTaskId, Guid? nextTaskId) =>
        Client.PatchAsJsonAsync($"/api/tasks/{id}/reorder", new { previousTaskId, nextTaskId });
}
