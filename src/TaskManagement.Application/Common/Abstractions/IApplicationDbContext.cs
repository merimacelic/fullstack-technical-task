using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Tags;
using TaskManagement.Domain.Users;

namespace TaskManagement.Application.Common.Abstractions;

public interface IApplicationDbContext
{
    DbSet<TaskItem> Tasks { get; }

    DbSet<Tag> Tags { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
