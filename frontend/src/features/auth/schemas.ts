// Zod schemas mirroring the backend FluentValidation rules 1:1:
//   RegisterUserCommandValidator — email 1-256, email format; password 8-128
//     + upper + lower + digit.
//   LoginUserCommandValidator — email required+format+<=256; password required+<=128.
// Server is the source of truth; these schemas exist for snappy UX feedback
// before the round-trip.

import { z } from 'zod';

export const loginSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(256, 'Email must be 256 characters or fewer.')
    .email('Enter a valid email address.'),
  password: z
    .string()
    .min(1, 'Password is required.')
    .max(128, 'Password is too long.'),
});
export type LoginForm = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(256, 'Email must be 256 characters or fewer.')
    .email('Enter a valid email address.'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters.')
    .max(128, 'Password must be 128 characters or fewer.')
    .regex(/[A-Z]/, 'Password must contain an uppercase letter.')
    .regex(/[a-z]/, 'Password must contain a lowercase letter.')
    .regex(/[0-9]/, 'Password must contain a digit.'),
});
export type RegisterForm = z.infer<typeof registerSchema>;
