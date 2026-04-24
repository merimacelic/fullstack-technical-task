// Zod schemas mirroring the backend FluentValidation rules 1:1:
//   RegisterUserCommandValidator — email 1-256, email format; password 8-128
//     + upper + lower + digit.
//   LoginUserCommandValidator — email required+format+<=256; password required+<=128.
// Server is the source of truth; these schemas exist for snappy UX feedback
// before the round-trip.
//
// Messages are i18n KEYS, not literal strings — schemas evaluate at module
// load (before i18n is initialised), so storing keys defers translation to
// render time. FormField calls t(message) when displaying the error.

import { z } from 'zod';

export const loginSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'validation.auth.email.required')
    .max(256, 'validation.auth.email.tooLong')
    .email('validation.auth.email.invalid'),
  password: z
    .string()
    .min(1, 'validation.auth.password.required')
    .max(128, 'validation.auth.password.tooLong'),
});
export type LoginForm = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'validation.auth.email.required')
    .max(256, 'validation.auth.email.tooLong')
    .email('validation.auth.email.invalid'),
  password: z
    .string()
    .min(8, 'validation.auth.password.tooShort')
    .max(128, 'validation.auth.password.tooLong')
    .regex(/[A-Z]/, 'validation.auth.password.upper')
    .regex(/[a-z]/, 'validation.auth.password.lower')
    .regex(/[0-9]/, 'validation.auth.password.digit'),
});
export type RegisterForm = z.infer<typeof registerSchema>;
