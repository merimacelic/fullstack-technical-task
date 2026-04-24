// MSW handlers mirroring the backend HTTP surface. The fixture database is
// in-memory and per-test — call `resetFixtures()` from a beforeEach if you
// need a clean slate.

import { http, HttpResponse, delay } from 'msw';
import type { TagDto } from '@/features/tags/types';
import type { TaskDto } from '@/features/tasks/types';

const API = 'http://localhost:8080';

interface UserRecord {
  id: string;
  email: string;
  password: string;
}

const fixtures: {
  users: UserRecord[];
  sessions: Record<string, string>;
  refreshTokens: Record<string, { userId: string; revoked: boolean }>;
  tasks: TaskDto[];
  tags: TagDto[];
} = {
  users: [],
  sessions: {},
  refreshTokens: {},
  tasks: [],
  tags: [],
};

export function resetFixtures() {
  fixtures.users = [];
  fixtures.sessions = {};
  fixtures.refreshTokens = {};
  fixtures.tasks = [];
  fixtures.tags = [];
}

export const fixtureDb = fixtures;

function issueSession(user: UserRecord) {
  const access = crypto.randomUUID();
  const refresh = crypto.randomUUID();
  fixtures.sessions[access] = user.id;
  fixtures.refreshTokens[refresh] = { userId: user.id, revoked: false };
  return {
    userId: user.id,
    email: user.email,
    accessToken: access,
    accessTokenExpiresUtc: new Date(Date.now() + 15 * 60_000).toISOString(),
    refreshToken: refresh,
    refreshTokenExpiresUtc: new Date(Date.now() + 7 * 24 * 60 * 60_000).toISOString(),
  };
}

function currentUserId(request: Request): string | null {
  const auth = request.headers.get('authorization');
  if (!auth?.startsWith('Bearer ')) return null;
  return fixtures.sessions[auth.slice('Bearer '.length)] ?? null;
}

export const handlers = [
  // ---------- Auth ----------
  http.post(`${API}/api/auth/register`, async ({ request }) => {
    const body = (await request.json()) as { email: string; password: string };
    if (fixtures.users.some((u) => u.email.toLowerCase() === body.email.toLowerCase())) {
      return HttpResponse.json(
        {
          type: 'User.RegistrationFailed',
          title: 'Validation error',
          status: 400,
          detail: 'Registration failed. Check the email and password, then try again.',
        },
        { status: 400 },
      );
    }
    const user: UserRecord = { id: crypto.randomUUID(), email: body.email, password: body.password };
    fixtures.users.push(user);
    return HttpResponse.json(issueSession(user));
  }),

  http.post(`${API}/api/auth/login`, async ({ request }) => {
    const body = (await request.json()) as { email: string; password: string };
    const user = fixtures.users.find(
      (u) => u.email.toLowerCase() === body.email.toLowerCase() && u.password === body.password,
    );
    if (!user) {
      return HttpResponse.json(
        {
          type: 'User.InvalidCredentials',
          title: 'Unauthorised',
          status: 401,
          detail: 'Email or password is incorrect.',
        },
        { status: 401 },
      );
    }
    return HttpResponse.json(issueSession(user));
  }),

  http.post(`${API}/api/auth/refresh`, async ({ request }) => {
    const body = (await request.json()) as { refreshToken: string };
    const record = fixtures.refreshTokens[body.refreshToken];
    if (!record || record.revoked) {
      return HttpResponse.json(
        {
          type: 'User.InvalidRefreshToken',
          title: 'Unauthorised',
          status: 401,
          detail: 'Refresh token is invalid, expired, or revoked.',
        },
        { status: 401 },
      );
    }
    record.revoked = true;
    const user = fixtures.users.find((u) => u.id === record.userId);
    if (!user) return HttpResponse.json(null, { status: 401 });
    return HttpResponse.json(issueSession(user));
  }),

  http.post(`${API}/api/auth/revoke`, async () => {
    await delay(10);
    return new HttpResponse(null, { status: 204 });
  }),

  // ---------- Tasks ----------
  http.get(`${API}/api/tasks`, ({ request }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const url = new URL(request.url);
    const status = url.searchParams.get('Status');
    const priority = url.searchParams.get('Priority');
    const search = url.searchParams.get('Search')?.toLowerCase();
    const sortBy = url.searchParams.get('SortBy') ?? 'CreatedAt';
    const sortDir = url.searchParams.get('SortDirection') ?? 'Descending';
    const page = Number(url.searchParams.get('Page') ?? 1);
    const pageSize = Number(url.searchParams.get('PageSize') ?? 20);

    let items = [...fixtures.tasks];
    if (status) items = items.filter((t) => t.status === status);
    if (priority) items = items.filter((t) => t.priority === priority);
    if (search) {
      items = items.filter(
        (t) =>
          t.title.toLowerCase().includes(search) ||
          (t.description ?? '').toLowerCase().includes(search),
      );
    }
    items.sort((a, b) => {
      const pick = (t: TaskDto) => {
        switch (sortBy) {
          case 'Title':
            return t.title;
          case 'Order':
            return t.orderKey;
          case 'Priority':
            return ['Low', 'Medium', 'High', 'Critical'].indexOf(t.priority);
          case 'DueDate':
            return t.dueDateUtc ?? '';
          case 'UpdatedAt':
            return t.updatedAtUtc;
          default:
            return t.createdAtUtc;
        }
      };
      const av = pick(a);
      const bv = pick(b);
      const cmp = av < bv ? -1 : av > bv ? 1 : 0;
      return sortDir === 'Descending' ? -cmp : cmp;
    });

    const totalCount = items.length;
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const paged = items.slice((page - 1) * pageSize, page * pageSize);
    return HttpResponse.json({
      items: paged,
      page,
      pageSize,
      totalCount,
      totalPages,
      hasNextPage: page < totalPages,
      hasPreviousPage: page > 1,
    });
  }),

  http.get(`${API}/api/tasks/:id`, ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const task = fixtures.tasks.find((t) => t.id === params.id);
    return task
      ? HttpResponse.json(task)
      : HttpResponse.json(
          { type: 'Task.NotFound', title: 'Not found', status: 404, detail: 'Task not found.' },
          { status: 404 },
        );
  }),

  http.post(`${API}/api/tasks`, async ({ request }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const body = (await request.json()) as Partial<TaskDto>;
    const now = new Date().toISOString();
    const task: TaskDto = {
      id: crypto.randomUUID(),
      title: body.title!,
      description: body.description ?? null,
      status: 'Pending',
      priority: (body.priority ?? 'Medium') as TaskDto['priority'],
      dueDateUtc: body.dueDateUtc ?? null,
      createdAtUtc: now,
      updatedAtUtc: now,
      completedAtUtc: null,
      orderKey: fixtures.tasks.length * 1000,
      tagIds: body.tagIds ?? [],
    };
    fixtures.tasks.unshift(task);
    return HttpResponse.json(task, { status: 201 });
  }),

  http.put(`${API}/api/tasks/:id`, async ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const idx = fixtures.tasks.findIndex((t) => t.id === params.id);
    if (idx < 0) return HttpResponse.json(null, { status: 404 });
    const body = (await request.json()) as Partial<TaskDto>;
    const current = fixtures.tasks[idx]!;
    const updated: TaskDto = {
      ...current,
      title: body.title ?? current.title,
      description: body.description ?? current.description,
      priority: (body.priority ?? current.priority) as TaskDto['priority'],
      dueDateUtc: body.dueDateUtc ?? current.dueDateUtc,
      tagIds: body.tagIds === null ? current.tagIds : (body.tagIds ?? current.tagIds),
      updatedAtUtc: new Date().toISOString(),
    };
    fixtures.tasks[idx] = updated;
    return HttpResponse.json(updated);
  }),

  http.delete(`${API}/api/tasks/:id`, ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    fixtures.tasks = fixtures.tasks.filter((t) => t.id !== params.id);
    return new HttpResponse(null, { status: 204 });
  }),

  http.patch(`${API}/api/tasks/:id/complete`, ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const t = fixtures.tasks.find((x) => x.id === params.id);
    if (!t) return HttpResponse.json(null, { status: 404 });
    t.status = 'Completed';
    t.completedAtUtc = new Date().toISOString();
    t.updatedAtUtc = t.completedAtUtc;
    return HttpResponse.json(t);
  }),

  http.patch(`${API}/api/tasks/:id/reopen`, ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const t = fixtures.tasks.find((x) => x.id === params.id);
    if (!t) return HttpResponse.json(null, { status: 404 });
    t.status = 'Pending';
    t.completedAtUtc = null;
    t.updatedAtUtc = new Date().toISOString();
    return HttpResponse.json(t);
  }),

  http.patch(`${API}/api/tasks/:id/reorder`, async ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const t = fixtures.tasks.find((x) => x.id === params.id);
    if (!t) return HttpResponse.json(null, { status: 404 });
    const body = (await request.json()) as { previousTaskId: string | null; nextTaskId: string | null };
    // Simple implementation: put it between its neighbours using their orderKeys.
    const prev = fixtures.tasks.find((x) => x.id === body.previousTaskId);
    const next = fixtures.tasks.find((x) => x.id === body.nextTaskId);
    if (prev && next) t.orderKey = (prev.orderKey + next.orderKey) / 2;
    else if (prev) t.orderKey = prev.orderKey + 1000;
    else if (next) t.orderKey = next.orderKey - 1000;
    else t.orderKey = 0;
    t.updatedAtUtc = new Date().toISOString();
    return HttpResponse.json(t);
  }),

  // ---------- Tags ----------
  http.get(`${API}/api/tags`, ({ request }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const withCounts = fixtures.tags.map((tag) => ({
      ...tag,
      taskCount: fixtures.tasks.filter((t) => t.tagIds.includes(tag.id)).length,
    }));
    return HttpResponse.json(withCounts);
  }),

  http.post(`${API}/api/tags`, async ({ request }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const body = (await request.json()) as { name: string };
    if (fixtures.tags.some((t) => t.name.toLowerCase() === body.name.toLowerCase())) {
      return HttpResponse.json(
        {
          type: 'Tag.AlreadyExists',
          title: 'Conflict',
          status: 409,
          detail: `A tag named '${body.name}' already exists for this user.`,
        },
        { status: 409 },
      );
    }
    const tag: TagDto = {
      id: crypto.randomUUID(),
      name: body.name,
      createdAtUtc: new Date().toISOString(),
      taskCount: 0,
    };
    fixtures.tags.push(tag);
    return HttpResponse.json(tag, { status: 201 });
  }),

  http.put(`${API}/api/tags/:id`, async ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    const body = (await request.json()) as { name: string };
    const tag = fixtures.tags.find((t) => t.id === params.id);
    if (!tag) return HttpResponse.json(null, { status: 404 });
    tag.name = body.name;
    return HttpResponse.json(tag);
  }),

  http.delete(`${API}/api/tags/:id`, ({ request, params }) => {
    const uid = currentUserId(request);
    if (!uid) return HttpResponse.json(null, { status: 401 });
    fixtures.tags = fixtures.tags.filter((t) => t.id !== params.id);
    fixtures.tasks.forEach((task) => {
      task.tagIds = task.tagIds.filter((id) => id !== params.id);
    });
    return new HttpResponse(null, { status: 204 });
  }),
];
