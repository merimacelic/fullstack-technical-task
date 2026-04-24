// Mirrors CreateTagCommandValidator / RenameTagCommandValidator — Name 1-50,
// unique per owner (uniqueness enforced server-side — surfaces as a 409).
//
// Messages are i18n keys; FormField translates at render time.

import { z } from 'zod';

export const tagFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'validation.tag.name.required')
    .max(50, 'validation.tag.name.tooLong'),
});
export type TagFormValues = z.infer<typeof tagFormSchema>;
