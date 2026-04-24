// Mirrors CreateTagCommandValidator / RenameTagCommandValidator — Name 1-50,
// unique per owner (uniqueness enforced server-side — surfaces as a 409).

import { z } from 'zod';

export const tagFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Tag name is required.')
    .max(50, 'Tag name must be 50 characters or fewer.'),
});
export type TagFormValues = z.infer<typeof tagFormSchema>;
