namespace TaskManagement.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class TaskManagementCollection : ICollectionFixture<TaskManagementApiFactory>
{
    public const string Name = "TaskManagementApi";
}
