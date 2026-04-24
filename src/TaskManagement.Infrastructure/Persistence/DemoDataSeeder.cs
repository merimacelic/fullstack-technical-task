using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Tags;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence;

// Populates the database with a deterministic demo dataset so a reviewer opening
// the hosted URL sees something meaningful instead of an empty shell.
//
// Idempotent: keyed off the demo user's email. Running twice is a no-op, so it
// is safe to leave Seeding:DemoData=true on a long-lived deployment — the
// container can restart freely and the data survives.
//
// Goes through the domain factories (Tag.Create / TaskItem.Create) rather than
// inserting raw rows, so every invariant (title length, due-date-in-past guard,
// order-key step) is exercised the same way the production write path would.
public sealed class DemoDataSeeder
{
    public const string DemoEmail = "demo@icon.mt";
    public const string DemoPassword = "Passw0rd!";

    // Stable seed → the same tag list + task distribution on every run, so the
    // reviewer sees the same pre-baked "Shop for groceries" as a screenshot.
    private const int RandomSeed = 20260425;

    private const int TaskCount = 60;

    // Weights for the "how many tags on a task?" distribution: 15% untagged,
    // 45% one tag, 30% two, 10% three. Centralised as fields so CA1861 is happy
    // (no new arrays allocated in the hot path).
    private static readonly int[] TagCountChoices = [0, 1, 2, 3];
    private static readonly float[] TagCountWeights = [0.15f, 0.45f, 0.30f, 0.10f];

    // Curated tag names — realistic, cover the common task-manager taxonomy, and
    // short enough that the tag picker UI doesn't wrap. 15 entries is enough to
    // stress the tag combobox without flooding the UI.
    private static readonly string[] TagNames =
    [
        "work",
        "urgent",
        "personal",
        "shopping",
        "research",
        "bugs",
        "feature",
        "meetings",
        "reading",
        "fitness",
        "hobby",
        "travel",
        "home",
        "finance",
        "learning",
    ];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByEmailAsync(DemoEmail) is not null)
        {
            _logger.LogInformation(
                "Demo data already seeded — user {Email} exists; skipping.", DemoEmail);
            return;
        }

        // Fresh-install check hardened against a rare half-seeded state: if the
        // user row is gone but tasks still reference a stale OwnerId, those
        // orphans wouldn't cause a constraint violation (no FK to Identity), but
        // they would muddle the demo. A single query confirms the DB has no
        // residue before we commit to creating a new user.
        if (await _db.Tasks.AnyAsync(cancellationToken))
        {
            _logger.LogInformation(
                "Task table is non-empty; skipping demo seed to avoid mixing with existing data.");
            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var created = await _userManager.CreateAsync(user, DemoPassword);
        if (!created.Succeeded)
        {
            var errors = string.Join("; ", created.Errors.Select(e => $"{e.Code}: {e.Description}"));
            _logger.LogError("Failed to create demo user {Email}: {Errors}", DemoEmail, errors);
            return;
        }

        var now = DateTime.UtcNow;
        var faker = new Faker("en") { Random = new Randomizer(RandomSeed) };

        var tags = CreateTags(user.Id, now);
        await _db.Tags.AddRangeAsync(tags, cancellationToken);

        var tasks = CreateTasks(user.Id, tags, faker, now);
        await _db.Tasks.AddRangeAsync(tasks, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded demo user {Email} with {TagCount} tags and {TaskCount} tasks. Password: {Password}",
            DemoEmail,
            tags.Count,
            tasks.Count,
            DemoPassword);
    }

    private static List<Tag> CreateTags(Guid ownerId, DateTime now)
    {
        var tags = new List<Tag>(TagNames.Length);
        foreach (var name in TagNames)
        {
            var result = Tag.Create(ownerId, name, now);
            if (!result.IsError)
            {
                tags.Add(result.Value);
            }
        }

        return tags;
    }

    private static List<TaskItem> CreateTasks(
        Guid ownerId,
        IReadOnlyList<Tag> tags,
        Faker faker,
        DateTime now)
    {
        var priorities = new[]
        {
            TaskPriority.Low,
            TaskPriority.Medium,
            TaskPriority.High,
            TaskPriority.Critical,
        };

        // Curated title pool keeps the dataset legible (Bogus.Hacker.Phrase()
        // produces amusing nonsense like "try to quantify the SSL driver" which
        // is fun in a dev fixture but reads as noise in a portfolio demo).
        // We blend a pool of realistic titles with Bogus descriptions for body copy.
        var titlePool = new[]
        {
            "Draft Q2 roadmap",
            "Review pull request #482",
            "Buy groceries for the week",
            "Renew domain registration",
            "Prepare sprint retrospective",
            "Investigate auth token leak",
            "Update onboarding documentation",
            "Call the plumber",
            "Book flights to Valletta",
            "Refactor TaskRepository",
            "Respond to client feedback",
            "Plan weekend hike",
            "Order new laptop stand",
            "Read 'Designing Data-Intensive Applications' ch. 7",
            "Schedule dentist appointment",
            "Pay electricity bill",
            "Write release notes for v1.3",
            "Sync with design team on new theme",
            "Backup family photos",
            "Fix flaky CI test",
            "Register for conference in Malta",
            "Plant tomatoes in the garden",
            "Submit tax return",
            "Move refresh tokens to httpOnly cookie",
            "Prepare demo for Friday standup",
            "Respond to Oliver's email",
            "Clean up unused feature flags",
            "Merge the tags-filter branch",
            "Cook Sunday dinner",
            "Run a 10k this weekend",
            "Audit third-party dependencies",
            "Write blog post on ErrorOr pattern",
            "Set up new dev machine",
            "Rotate JWT signing key",
            "Fix keyboard shortcut on TaskCard",
            "Add dark-mode screenshots to README",
            "Triage weekly bug reports",
            "Pair with Sarah on new endpoint",
            "Organise team lunch",
            "Review architecture decision records",
            "Migrate feature flags to LaunchDarkly",
            "Write tests for pagination edge cases",
            "Draft performance review",
            "Update Dockerfile to node:24",
            "Reply to tax accountant",
            "Configure Cloudflare Pages",
            "Order birthday gift",
            "Research pgvector for search",
            "Restart home router",
            "Finish reading the design doc",
            "Pick up prescription",
            "Schedule quarterly 1:1s",
            "Rename legacy Order model",
            "Stretch the Zod schemas to match new API",
            "Water the plants",
            "Book car service",
            "Write post-mortem for yesterday's outage",
            "Clean the dishwasher filter",
            "Set up monitoring alerts for 5xx",
            "Ship the i18n branch",
        };

        // Generate orderKeys at the domain-approved spacing so drag-and-drop still
        // operates in a large gap window (no rebalance needed from the first drag).
        var orderKey = TaskItem.OrderKeyStep;
        var tasks = new List<TaskItem>(TaskCount);
        for (var i = 0; i < TaskCount; i++)
        {
            var title = titlePool[i % titlePool.Length];

            // Backdate creation across the last 90 days so the list has history
            // and the "updated recently" filter has something to work with.
            var createdAt = now.AddDays(-faker.Random.Int(0, 90)).AddHours(-faker.Random.Int(0, 23));

            // Roughly 70% of tasks get a description — matches what users actually do.
            var description = faker.Random.Bool(0.7f) ? faker.Lorem.Paragraph() : null;

            var priority = faker.PickRandom(priorities);

            // Due-date distribution:
            //   25% none, 35% in the future, 40% relative to createdAt (mix of overdue + not)
            DateTime? dueDate = faker.Random.Double() switch
            {
                < 0.25 => null,
                < 0.60 => now.AddDays(faker.Random.Int(1, 45)).Date,
                _ => createdAt.AddDays(faker.Random.Int(1, 60)).Date,
            };

            var result = TaskItem.Create(
                ownerId,
                title,
                description,
                priority,
                dueDate,
                createdAt,
                orderKey);

            if (result.IsError)
            {
                // Very unlikely (inputs are all valid by construction) but
                // defensively skip rather than crash the whole seeder.
                continue;
            }

            var task = result.Value;

            // Status distribution: ~35% completed, ~25% in-progress, ~40% pending.
            var roll = faker.Random.Double();
            if (roll < 0.35)
            {
                var completedAt = task.CreatedAtUtc.AddHours(faker.Random.Int(1, 24 * 30));
                if (completedAt > now)
                {
                    completedAt = now;
                }

                task.Complete(completedAt);
            }
            else if (roll < 0.60)
            {
                task.ChangeStatus(TaskItemStatus.InProgress, task.CreatedAtUtc.AddHours(faker.Random.Int(1, 12)));
            }

            // Tag association: 0–3 random tags per task. Weighted so most tasks
            // have at least one tag (demonstrates the feature) but some stay
            // untagged (demonstrates the filter shows "no tags" tasks too).
            var tagCount = faker.Random.WeightedRandom(TagCountChoices, TagCountWeights);

            if (tagCount > 0)
            {
                var picked = faker.Random.Shuffle(tags).Take(tagCount).Select(t => t.Id).ToArray();
                task.ReplaceTags(picked, task.UpdatedAtUtc);
            }

            tasks.Add(task);
            orderKey += TaskItem.OrderKeyStep;
        }

        return tasks;
    }
}
