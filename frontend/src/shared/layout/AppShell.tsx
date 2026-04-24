import { Outlet } from 'react-router-dom';
import { TooltipProvider } from '@/shared/ui/tooltip';
import { AppHeader } from './AppHeader';

export function AppShell() {
  return (
    <TooltipProvider delayDuration={200}>
      <a href="#main-content" className="skip-link">
        Skip to main content
      </a>
      <div className="flex min-h-screen flex-col bg-background text-foreground">
        <AppHeader />
        <main id="main-content" className="flex-1">
          <div className="mx-auto w-full max-w-6xl px-4 py-6">
            <Outlet />
          </div>
        </main>
      </div>
    </TooltipProvider>
  );
}
