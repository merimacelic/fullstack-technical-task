import { LogOut, User } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

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
import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/ui/tooltip';
import { LanguageSwitcher } from './LanguageSwitcher';
import { ThemeToggle } from './ThemeToggle';

export function AppHeader() {
  const { t } = useTranslation();
  const user = useAppSelector(selectCurrentUser);
  const [revoke, { isLoading }] = useRevokeMutation();

  return (
    <header className="sticky top-0 z-40 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4">
        <Link to="/tasks" className="flex items-center gap-2 font-semibold">
          <span aria-hidden className="text-xl">✓</span>
          <span>{t('common.appName')}</span>
        </Link>
        <nav className="flex items-center gap-2">
          <Tooltip>
            <TooltipTrigger asChild>
              <span>
                <LanguageSwitcher />
              </span>
            </TooltipTrigger>
            <TooltipContent side="bottom">{t('header.tooltip.language')}</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <span>
                <ThemeToggle />
              </span>
            </TooltipTrigger>
            <TooltipContent side="bottom">{t('header.tooltip.theme')}</TooltipContent>
          </Tooltip>
          <DropdownMenu>
            <Tooltip>
              <TooltipTrigger asChild>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" aria-label={t('header.account.menu')}>
                    <User className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
              </TooltipTrigger>
              <TooltipContent side="bottom">{t('header.tooltip.account')}</TooltipContent>
            </Tooltip>
            <DropdownMenuContent align="end" className="w-56">
              <DropdownMenuLabel className="flex flex-col gap-0.5">
                <span className="text-xs text-muted-foreground">{t('header.account.signedInAs')}</span>
                <span className="truncate text-sm font-normal">
                  {user?.email ?? t('header.account.unknown')}
                </span>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                disabled={isLoading}
                onClick={() => {
                  void revoke();
                }}
              >
                <LogOut className="mr-2 h-4 w-4" />
                {t('header.account.signOut')}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </nav>
      </div>
    </header>
  );
}
