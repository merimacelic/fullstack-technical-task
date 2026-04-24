// Mirrors CreateTaskCommandValidator + UpdateTaskCommandValidator on the
// backend. MaxTagsPerTask=50 is enforced server-side too (see CreateTask
// command validator); we cap client-side so users get feedback before submit.
//
// Messages are i18n keys; FormField translates at render time.

import { z } from 'zod';
import { isPastDate } from '@/shared/lib/date';

const MAX_TITLE = 200;
const MAX_DESCRIPTION = 2000;
const MAX_TAGS = 50;

export const taskFormSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, 'validation.task.title.required')
    .max(MAX_TITLE, 'validation.task.title.tooLong'),
  description: z
    .string()
    .max(MAX_DESCRIPTION, 'validation.task.description.tooLong')
    .optional()
    .or(z.literal('')),
  priority: z.enum(['Low', 'Medium', 'High', 'Critical'], {
    errorMap: () => ({ message: 'validation.task.priority.required' }),
  }),
  status: z.enum(['Pending', 'InProgress', 'Completed'], {
    errorMap: () => ({ message: 'validation.task.status.required' }),
  }),
  dueDate: z
    .string()
    .optional()
    .refine((v) => !v || !isPastDate(v), 'validation.task.dueDate.past'),
  tagIds: z
    .array(z.string().uuid())
    .max(MAX_TAGS, 'validation.task.tags.max')
    .optional()
    .default([]),
});

export type TaskFormValues = z.infer<typeof taskFormSchema>;
