import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { FormField } from '@/shared/ui/form-field';
import { parseProblem } from '@/shared/lib/problemDetails';
import { useRegisterMutation } from '../api';
import { registerSchema, type RegisterForm as RegisterValues } from '../schemas';
import { useAppSelector } from '@/app/hooks';
import { selectIsAuthenticated } from '../slice';

export function RegisterForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [registerUser, { isLoading }] = useRegisterMutation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  const form = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: '', password: '' },
    mode: 'onBlur',
  });

  useEffect(() => {
    if (isAuthenticated) navigate('/tasks', { replace: true });
  }, [isAuthenticated, navigate]);

  async function onSubmit(values: RegisterValues) {
    try {
      await registerUser(values).unwrap();
    } catch (err) {
      const parsed = parseProblem(err as never);
      if (parsed.fieldErrors) {
        for (const [field, messages] of Object.entries(parsed.fieldErrors)) {
          const msg = messages?.[0];
          if (msg) {
            form.setError(field as keyof RegisterValues, { message: msg });
          }
        }
        return;
      }
      toast.error(t(parsed.titleKey), { description: t(parsed.detailKey) });
    }
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
      <FormField
        id="email"
        label={t('auth.fields.email')}
        required
        error={form.formState.errors.email?.message}
      >
        {({ id, describedBy, invalid }) => (
          <Input
            id={id}
            type="email"
            autoComplete="email"
            aria-invalid={invalid}
            aria-describedby={describedBy}
            {...form.register('email')}
          />
        )}
      </FormField>

      <FormField
        id="password"
        label={t('auth.fields.password')}
        required
        description={t('auth.register.passwordHelp')}
        error={form.formState.errors.password?.message}
      >
        {({ id, describedBy, invalid }) => (
          <Input
            id={id}
            type="password"
            autoComplete="new-password"
            aria-invalid={invalid}
            aria-describedby={describedBy}
            {...form.register('password')}
          />
        )}
      </FormField>

      <Button type="submit" disabled={isLoading} aria-busy={isLoading} className="mt-2">
        {isLoading ? t('auth.register.submitting') : t('auth.register.submit')}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        {t('auth.register.haveAccount')}{' '}
        <Link to="/login" className="font-medium text-foreground underline-offset-4 hover:underline">
          {t('auth.register.signIn')}
        </Link>
      </p>
    </form>
  );
}
