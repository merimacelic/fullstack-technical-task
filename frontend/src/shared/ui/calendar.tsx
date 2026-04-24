import { DayPicker } from 'react-day-picker';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { buttonVariants } from './button';
import { cn } from '@/shared/lib/cn';

export type CalendarProps = React.ComponentProps<typeof DayPicker>;

/**
 * Thin wrapper around react-day-picker v9. Uses the v9 class-name map
 * (months, month_caption, weekdays, weekday, week, day, day_button, …) —
 * v8 names (caption, head_row, head_cell, row, cell, day_selected, …) no
 * longer exist in v9 and silently break the layout and click targets.
 */
export function Calendar({
  className,
  classNames,
  showOutsideDays = true,
  ...props
}: CalendarProps) {
  return (
    <DayPicker
      showOutsideDays={showOutsideDays}
      className={cn('p-3', className)}
      classNames={{
        months: 'flex flex-col gap-4 sm:flex-row',
        month: 'flex flex-col gap-3',
        month_caption: 'relative flex h-8 items-center justify-center pt-1',
        caption_label: 'text-sm font-medium',
        nav: 'flex items-center',
        button_previous: cn(
          buttonVariants({ variant: 'ghost' }),
          'absolute left-1 top-1 size-7 cursor-pointer p-0 opacity-60 hover:opacity-100',
        ),
        button_next: cn(
          buttonVariants({ variant: 'ghost' }),
          'absolute right-1 top-1 size-7 cursor-pointer p-0 opacity-60 hover:opacity-100',
        ),
        month_grid: 'w-full border-collapse',
        weekdays: 'flex',
        weekday:
          'w-9 flex-1 text-center text-[0.75rem] font-normal text-muted-foreground',
        week: 'mt-1 flex w-full',
        day: 'relative size-9 flex-1 p-0 text-center text-sm',
        day_button: cn(
          buttonVariants({ variant: 'ghost' }),
          'size-9 cursor-pointer p-0 font-normal',
        ),
        selected:
          '[&_button]:bg-primary [&_button]:text-primary-foreground [&_button]:hover:bg-primary [&_button]:hover:text-primary-foreground [&_button]:focus:bg-primary [&_button]:focus:text-primary-foreground',
        today:
          '[&_button]:bg-accent [&_button]:text-accent-foreground [&_button]:font-semibold',
        outside: '[&_button]:text-muted-foreground [&_button]:opacity-50',
        disabled:
          '[&_button]:pointer-events-none [&_button]:text-muted-foreground [&_button]:opacity-40',
        hidden: 'invisible',
        ...classNames,
      }}
      components={{
        Chevron: ({ orientation }) =>
          orientation === 'left' ? (
            <ChevronLeft className="size-4" />
          ) : (
            <ChevronRight className="size-4" />
          ),
      }}
      {...props}
    />
  );
}
Calendar.displayName = 'Calendar';
