import { Badge } from '@/shared/ui/badge';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/ui/tooltip';
import type { TagDto } from '@/features/tags/types';

interface TagOverflowProps {
  tags: readonly TagDto[];
  max?: number;
}

/**
 * Shows up to `max` tag badges inline and collapses the rest into a
 * `+N` pill. Keeps the badge row a single line regardless of how many
 * tags are attached. Long tag names truncate with an ellipsis.
 */
export function TagOverflow({ tags, max = 2 }: TagOverflowProps) {
  if (tags.length === 0) return null;
  const visible = tags.slice(0, max);
  const hidden = tags.slice(max);

  return (
    <>
      {visible.map((tag) => (
        <Badge
          key={tag.id}
          variant="outline"
          className="max-w-[120px] shrink-0 truncate text-xs"
          title={tag.name}
        >
          #{tag.name}
        </Badge>
      ))}
      {hidden.length > 0 && (
        <Tooltip>
          <TooltipTrigger asChild>
            <Badge
              variant="outline"
              className="shrink-0 cursor-default text-xs text-muted-foreground"
            >
              +{hidden.length}
            </Badge>
          </TooltipTrigger>
          <TooltipContent side="top" className="max-w-[260px]">
            {hidden.map((t) => `#${t.name}`).join(', ')}
          </TooltipContent>
        </Tooltip>
      )}
    </>
  );
}
