// A thin wrapper that standardises label / error / description around a field.
// Keeps RHF usage tidy: pass id/description/error from formState — the wrapper
// handles aria-* wiring so the visual layer never forgets it.
//
// Error translation is done here: Zod schemas store translation keys as error
// messages (they evaluate at module load, before i18n is ready), and FormField
// looks them up at render time. If the passed string isn't a known key, it's
// rendered verbatim — so server-side field errors (already localised by the
// backend via Accept-Language) round-trip as-is.

import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { Label } from './label';
import { cn } from '@/shared/lib/cn';

interface FormFieldProps {
  id: string;
  label: string;
  description?: string;
  error?: string;
  required?: boolean;
  className?: string;
  children: (ids: { id: string; describedBy: string | undefined; invalid: boolean }) => ReactNode;
}

export function FormField({
  id,
  label,
  description,
  error,
  required,
  className,
  children,
}: FormFieldProps) {
  const { t } = useTranslation();
  const descriptionId = description ? `${id}-description` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [descriptionId, errorId].filter(Boolean).join(' ') || undefined;
  const invalid = Boolean(error);
  const localisedError = error ? t(error, { defaultValue: error }) : undefined;

  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <Label htmlFor={id}>
        {label}
        {required && <span className="ml-0.5 text-destructive">*</span>}
      </Label>
      {children({ id, describedBy, invalid })}
      {description && !error && (
        <p id={descriptionId} className="text-xs text-muted-foreground">
          {description}
        </p>
      )}
      {localisedError && (
        <p id={errorId} className="text-xs text-destructive">
          {localisedError}
        </p>
      )}
    </div>
  );
}
