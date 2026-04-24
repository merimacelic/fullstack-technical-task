import { describe, expect, it } from 'vitest';
import { taskFormSchema } from '@/features/tasks/schemas';

describe('taskFormSchema', () => {
  it('accepts a valid task', () => {
    const result = taskFormSchema.safeParse({
      title: 'Ship it',
      description: 'Hooray',
      priority: 'High',
      dueDate: '2099-01-01',
      tagIds: [],
    });
    expect(result.success).toBe(true);
  });

  it('rejects an empty title', () => {
    const result = taskFormSchema.safeParse({
      title: '',
      priority: 'Low',
    });
    expect(result.success).toBe(false);
  });

  it('rejects an unknown priority', () => {
    const result = taskFormSchema.safeParse({
      title: 'x',
      priority: 'Nope',
    });
    expect(result.success).toBe(false);
  });

  it('rejects a past due date', () => {
    const result = taskFormSchema.safeParse({
      title: 'x',
      priority: 'Low',
      dueDate: '1999-01-01',
    });
    expect(result.success).toBe(false);
  });
});
