using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.IntegrationTests.Infrastructure;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Application.Tasks.Tags.Responses;

namespace TaskManagement.Api.IntegrationTests.Tags;

public class TagEndpointsTests : IntegrationTestBase
{
    public TagEndpointsTests(TaskManagementApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateAndList_Should_Roundtrip_Tag_ScopedToUser()
    {
        await AuthenticateAsync();

        var create = await Client.PostAsJsonAsync("/api/tags", new { name = "urgent" });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await Client.GetAsync("/api/tags");
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tags = await ReadJsonAsync<List<TagResponse>>(list);
        tags.Count.ShouldBe(1);
        tags[0].Name.ShouldBe("urgent");
        tags[0].TaskCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateTask_Should_Accept_TagIds_And_Filter_By_Tag()
    {
        await AuthenticateAsync();

        var tag = await CreateTag("sprint-1");

        var createTask = await Client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Tagged task",
            priority = "Medium",
            tagIds = new[] { tag.Id },
        });
        createTask.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadJsonAsync<TaskResponse>(createTask);
        body.TagIds.ShouldContain(tag.Id);

        var filtered = await Client.GetAsync($"/api/tasks?tagIds={tag.Id}");
        filtered.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ReadJsonAsync<PagedResult<TaskResponse>>(filtered);
        page.Items.Count.ShouldBe(1);
        page.Items[0].Id.ShouldBe(body.Id);
    }

    [Fact]
    public async Task Tags_Should_Be_Scoped_Per_User()
    {
        await AuthenticateAsync();
        await CreateTag("a-only");

        var clientB = CreateUnauthenticatedClient();
        await AuthenticateAsync(clientB);

        var list = await clientB.GetAsync("/api/tags");
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tags = await ReadJsonAsync<List<TagResponse>>(list);
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteTag_Should_RemoveAssociation_From_Tasks()
    {
        await AuthenticateAsync();
        var tag = await CreateTag("to-delete");

        var createTask = await Client.PostAsJsonAsync("/api/tasks", new
        {
            title = "tagged",
            priority = "Low",
            tagIds = new[] { tag.Id },
        });
        createTask.StatusCode.ShouldBe(HttpStatusCode.Created);
        var task = await ReadJsonAsync<TaskResponse>(createTask);

        var delete = await Client.DeleteAsync($"/api/tags/{tag.Id}");
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var fetched = await Client.GetAsync($"/api/tasks/{task.Id}");
        fetched.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshed = await ReadJsonAsync<TaskResponse>(fetched);
        refreshed.TagIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReorderTask_Should_Move_Task_Between_Neighbours()
    {
        await AuthenticateAsync();

        var a = await CreateTask("a");
        var b = await CreateTask("b");
        var c = await CreateTask("c");

        // Move 'c' between 'a' and 'b' (drag-and-drop).
        var reorder = await Client.PatchAsJsonAsync($"/api/tasks/{c.Id}/reorder", new
        {
            previousTaskId = a.Id,
            nextTaskId = b.Id,
        });
        reorder.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Fetch the list sorted by Order and assert the new sequence.
        var list = await Client.GetAsync("/api/tasks?sortBy=Order&sortDirection=Ascending");
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ReadJsonAsync<PagedResult<TaskResponse>>(list);
        page.Items.Select(t => t.Id).ShouldBe([a.Id, c.Id, b.Id]);
    }

    private async Task<TagResponse> CreateTag(string name)
    {
        var response = await Client.PostAsJsonAsync("/api/tags", new { name });
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync<TagResponse>(response);
    }

    private async Task<TaskResponse> CreateTask(string title)
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new { title, priority = "Low" });
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync<TaskResponse>(response);
    }
}
