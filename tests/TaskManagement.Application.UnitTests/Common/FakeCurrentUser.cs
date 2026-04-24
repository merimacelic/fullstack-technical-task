using TaskManagement.Application.Common.Abstractions;

namespace TaskManagement.Application.UnitTests.Common;

public sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid? userId) => UserId = userId;

    public Guid? UserId { get; set; }

    public bool IsAuthenticated => UserId.HasValue;

    public static FakeCurrentUser WithId(Guid id) => new(id);

    public static FakeCurrentUser Anonymous() => new(null);
}
