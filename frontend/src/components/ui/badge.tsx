import type { HTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export type BadgeTone = 'neutral' | 'positive' | 'info' | 'warn' | 'danger' | 'accent'

const tones: Record<BadgeTone, string> = {
  neutral: 'bg-neutral-tag-soft text-neutral-tag',
  positive: 'bg-positive-soft text-positive',
  info: 'bg-info-soft text-info',
  warn: 'bg-warn-soft text-warn',
  danger: 'bg-danger-soft text-danger',
  accent: 'bg-accent-soft text-accent',
}

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: BadgeTone
}

export function Badge({ className, tone = 'neutral', ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-xs px-1.5 py-0.5 text-xs font-medium tracking-wide',
        tones[tone],
        className,
      )}
      {...props}
    />
  )
}
