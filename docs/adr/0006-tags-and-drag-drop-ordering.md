# 0006 — Tags (second aggregate) and drag-and-drop ordering

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

Two features were added to cover the trial's bonus list:
- **Task prioritisation + drag-and-drop sorting** (explicitly named in the bonus list).
- **Advanced functionality** — the brief is vague here. We interpret it as "something beyond plain CRUD." Tags are the most natural extension for a task app.

### Drag-and-drop needs a server-side ordering model

Manual ordering cannot live only in the frontend; if the UI controls the sequence, the server must persist it. Two common approaches:

1. **Integer position with renumber-on-move** — every reorder rewrites the positions of every sibling. O(n) per drag.
2. **Decimal/fraction position with midpoint insert** — dragging between two neighbours computes the midpoint of their keys; only the moved row is touched. Rebalance only when the gap collapses below a threshold.

### Tags need a second aggregate

A string-array inline on the task is simpler but weaker as a DDD demonstration — no uniqueness, no rename, no per-user tag palette, no FK semantics. A proper `Tag` aggregate with its own CRUD endpoints and a many-to-many association to tasks reads better and tests more scenarios.

## Decision

### Ordering

- Add `OrderKey` (decimal, precision 38, 18) to `TaskItem`. Populated on create (`max + 1000`).
- `TaskItem.MoveTo(orderKey, nowUtc)` is the only write path.
- `PATCH /api/tasks/{id}/reorder { previousTaskId?, nextTaskId? }` — both optional so the target can be dropped at the start or the end.
- `OrderKeyService` (Application) computes the midpoint. If the gap between neighbours falls below `OrderKeyRebalanceThreshold` (0.001), the whole user's task list is renumbered with step 1000.
- Sorting: `GetTasks` gains `sortBy=order`. Default sort stays `createdAt desc` so behaviour is unchanged for existing clients.

### Tags

- `Tag` aggregate in `Domain/Tasks/Tags/` with `TagId`, `OwnerId`, `Name`, `CreatedAtUtc`. Methods: `Create`, `Rename`. Unique per owner (enforced by a composite index).
- Storage trade-off considered:
  - **Proper join table** — cleaner FK semantics, ugly EF config because `TaskItem` exposes `HashSet<TagId>` (no entity) and EF's skip navigation wants a navigation on both sides.
  - **JSON primitive collection** (EF 8 `PrimitiveCollection` on `List<Guid>` stored in a `TagIds` column) — no FK, but clean domain, works with `Contains` queries via `OPENJSON`.
- **Chose the JSON primitive collection.** FK integrity is handled explicitly: `DeleteTagCommandHandler` sweeps every task referencing the tag and calls `RemoveTag(tagId)` before deleting the aggregate. This is the **only** mutation path that can leave dangling references, and it's covered by an integration test.
- Endpoints: `GET /api/tags`, `POST /api/tags`, `PUT /api/tags/{id}`, `DELETE /api/tags/{id}`.
- Tasks integrate at the command boundary: `CreateTaskCommand` and `UpdateTaskCommand` accept optional `TagIds` which the handler validates (owned-by-caller) before calling `TaskItem.ReplaceTags(...)`. `GetTasksQuery` gains a `tagId` filter translated via `EF.Property<List<Guid>>(t, "_tagIds").Contains(tagId)`.

## Consequences

- **Positive:** the bonus "drag-and-drop" item is now functionally complete on the backend (the React iteration becomes a wiring exercise); the "advanced functionality" bonus is concretely demonstrated through a second aggregate with its own CRUD, many-to-many semantics, and cascading cleanup.
- **Negative:** JSON storage for tag associations means we don't get SQL-level FK integrity; if someone inserts a task row directly (bypassing the handler) they can reference a non-existent tag id. That's acceptable for this scope — the only writer is the API.
- **Mitigation:** an integration test asserts that `DELETE /api/tags/{id}` clears the tag from every task that referenced it (`DeleteTag_Should_RemoveAssociation_From_Tasks`). If we ever need real FK integrity, swap the primitive collection for a proper `TaskTag` join entity — contained to Infrastructure.
