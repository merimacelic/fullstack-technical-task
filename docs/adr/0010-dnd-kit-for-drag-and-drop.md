# 0010 — @dnd-kit for drag-and-drop reordering

- **Status:** Accepted
- **Date:** 2026-04-24

## Context

The backend's `PATCH /api/tasks/{id}/reorder` expects a `{ previousTaskId, nextTaskId }` pair (ADR 0006); the SPA needs a drag-and-drop UI that produces those pairs. Options in 2026:

| Library | Status | Notes |
|---|---|---|
| **react-beautiful-dnd** | Deprecated by Atlassian (2024) | Don't. |
| **@dnd-kit** | Actively maintained | Keyboard + screen-reader support built in. Modular sensors, sortable strategy. |
| **Pragmatic drag-and-drop** (Atlassian) | Modern | Designed for Jira-scale (1000+ items). More complex API; overkill here. |
| **react-dnd** | Maintained but ageing | HTML5 drag-and-drop quirks on touch devices; less friendly API. |

## Decision

**@dnd-kit** — `DndContext` + `SortableContext` with `verticalListSortingStrategy`.

- `PointerSensor` with `activationConstraint: { distance: 8 }` — avoids accidental drags when the user just clicks a card button.
- `KeyboardSensor` with `coordinateGetter: sortableKeyboardCoordinates` — enables Space to pick up, arrow keys to move, Space to drop, Escape to cancel.
- Custom `announcements` on the `DndContext` so the ARIA live region reads task titles, not opaque IDs.
- `onDragEnd` derives `previousTaskId` + `nextTaskId` by looking at the array after `arrayMove`, then dispatches the RTK Query `reorder` mutation.
- Optimistic cache patching via `onQueryStarted` + `updateQueryData` so the card snaps to its new position instantly; the server response reconciles `orderKey`.

Drag handle is a dedicated `<button>` with an explicit `aria-label="Drag to reorder {title}"`. Drag mode is only active when `SortBy === 'Order'` — mixing DnD with a non-order sort would mislead the user.

## Consequences

- **Positive:** keyboard + SR users get a first-class experience; desktop mouse users get a responsive UI. The library is small (~20 kB gzipped core + sortable).
- **Negative:** dnd-kit's API is a handful of primitives; there's no "drop-in kanban" pre-built widget. For this app that's a feature — we want control over the list shape.
- **Future work:** multi-column boards (Kanban) would add `DragOverlay` + `useDroppable` per column; current single-list architecture stays the same.
