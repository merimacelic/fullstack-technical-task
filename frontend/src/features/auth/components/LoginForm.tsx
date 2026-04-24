import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { FormField } from '@/shared/ui/form-field';
import { parseProblem } from '@/shared/lib/problemDetails';
import { useLoginMutation } from '../api';
import { loginSchema, type LoginForm as LoginValues } from '../schemas';
import { useAppSelector } from '@/app/hooks';
import { selectIsAuthenticated } from '../slice';

interface LocationState {
  from?: { pathname?: string };
}

export function LoginForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const [login, { isLoading }] = useLoginMutation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  const form = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
    mode: 'onBlur',
  });

  useEffect(() => {
    if (isAuthenticated) {
      const state = location.state as LocationState | null;
      navigate(state?.from?.pathname ?? '/tasks', { replace: true });
    }
  }, [isAuthenticated, location.state, navigate]);

  async function onSubmit(values: LoginValues) {
    try {
      await login(values).unwrap();
    } catch (err) {
      const parsed = parseProblem(err as never);
      if (parsed.fieldErrors) {
        for (const [field, messages] of Object.entries(parsed.fieldErrors)) {
          const msg = messages?.[0];
          if (msg) {
            form.setError(field as keyof LoginValues, { message: msg });
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
        error={form.formState.errors.password?.message}
      >
        {({ id, describedBy, invalid }) => (
          <Input
            id={id}
            type="password"
            autoComplete="current-password"
            aria-invalid={invalid}
            aria-describedby={describedBy}
            {...form.register('password')}
          />
        )}
      </FormField>

      <Button type="submit" disabled={isLoading} aria-busy={isLoading} className="mt-2">
        {isLoading ? t('auth.login.submitting') : t('auth.login.submit')}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        {t('auth.login.noAccount')}{' '}
        <Link to="/register" className="font-medium text-foreground underline-offset-4 hover:underline">
          {t('auth.login.createOne')}
        </Link>
      </p>
    </form>
  );
}
