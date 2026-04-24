import type { ReactNode } from 'react';
import { ThemeToggle } from '@/shared/layout/ThemeToggle';
import { Separator } from '@/shared/ui/separator';

interface AuthShellProps {
  title: string;
  description: string;
  children: ReactNode;
}

export function AuthShell({ title, description, children }: AuthShellProps) {
  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <header className="flex items-center justify-between px-4 py-4">
        <span className="flex items-center gap-2 font-semibold">
          <span aria-hidden className="text-xl">
            ✓
          </span>
          <span>Task Management</span>
        </span>
        <ThemeToggle />
      </header>
      <main className="flex flex-1 items-center justify-center px-4 py-10">
        <div className="w-full max-w-md rounded-lg border bg-card p-8 shadow-sm">
          <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{description}</p>
          <Separator className="my-6" />
          {children}
        </div>
      </main>
    </div>
  );
}
