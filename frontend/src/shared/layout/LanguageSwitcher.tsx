import { Check, Languages } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Button } from '@/shared/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu';
import { useLocale } from '@/i18n/useLocale';

// Flag-emoji-prefixed labels keep the active locale obvious inside the menu
// without pulling in an icon set for every language. When a third locale ships
// later, adding one entry here + one translation file + one entry in
// SUPPORTED_LOCALES is the whole change.
const FLAGS: Record<'en' | 'mt', string> = {
  en: '🇬🇧',
  mt: '🇲🇹',
};

export function LanguageSwitcher() {
  const { t } = useTranslation();
  const { locale, setLocale, locales } = useLocale();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label={t('header.language.toggle')}>
          <Languages className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {locales.map((code) => (
          <DropdownMenuItem
            key={code}
            onClick={() => setLocale(code)}
            className="gap-2"
          >
            <span aria-hidden>{FLAGS[code]}</span>
            <span className="flex-1">{t(`header.language.${code}`)}</span>
            {locale === code && <Check className="h-4 w-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
