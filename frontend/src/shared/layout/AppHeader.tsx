import { LogOut, Tags, User } from 'lucide-react';
import { Link } from 'react-router-dom';

import { useAppSelector } from '@/app/hooks';
import { selectCurrentUser } from '@/features/auth/slice';
import { useRevokeMutation } from '@/features/auth/api';
import { Button } from '@/shared/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu';
import { ThemeToggle } from './ThemeToggle';
import { TagManager } from '@/features/tags/components/TagManager';
import { useState } from 'react';

export function AppHeader() {
  const user = useAppSelector(selectCurrentUser);
  const [revoke, { isLoading }] = useRevokeMutation();
  const [tagsOpen, setTagsOpen] = useState(false);

  return (
    <header className="sticky top-0 z-40 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4">
        <Link to="/tasks" className="flex items-center gap-2 font-semibold">
          <span aria-hidden className="text-xl">✓</span>
          <span>Task Management</span>
        </Link>
        <nav className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setTagsOpen(true)}
            aria-label="Manage tags"
          >
            <Tags className="mr-2 h-4 w-4" />
            <span className="hidden sm:inline">Tags</span>
          </Button>
          <ThemeToggle />
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" aria-label="Open account menu">
                <User className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <DropdownMenuLabel className="flex flex-col gap-0.5">
                <span className="text-xs text-muted-foreground">Signed in as</span>
                <span className="truncate text-sm font-normal">{user?.email ?? 'unknown'}</span>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                disabled={isLoading}
                onClick={() => {
                  void revoke();
                }}
              >
                <LogOut className="mr-2 h-4 w-4" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </nav>
      </div>
      <TagManager open={tagsOpen} onOpenChange={setTagsOpen} />
    </header>
  );
}
