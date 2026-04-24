// Mirrors CreateTaskCommandValidator + UpdateTaskCommandValidator on the
// backend. MaxTagsPerTask=50 is enforced server-side too (see CreateTask
// command validator); we cap client-side so users get feedback before submit.

import { z } from 'zod';
import { isPastDate } from '@/shared/lib/date';

const MAX_TITLE = 200;
const MAX_DESCRIPTION = 2000;
const MAX_TAGS = 50;

export const taskFormSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, 'Title is required.')
    .max(MAX_TITLE, `Title must be ${MAX_TITLE} characters or fewer.`),
  description: z
    .string()
    .max(MAX_DESCRIPTION, `Description must be ${MAX_DESCRIPTION} characters or fewer.`)
    .optional()
    .or(z.literal('')),
  priority: z.enum(['Low', 'Medium', 'High', 'Critical'], {
    errorMap: () => ({ message: 'Select a priority.' }),
  }),
  dueDate: z
    .string()
    .optional()
    .refine((v) => !v || !isPastDate(v), 'Due date cannot be in the past.'),
  tagIds: z
    .array(z.string().uuid())
    .max(MAX_TAGS, `A task can have at most ${MAX_TAGS} tags.`)
    .optional()
    .default([]),
});

export type TaskFormValues = z.infer<typeof taskFormSchema>;
