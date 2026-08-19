import { Badge, type BadgeTone } from '@/components/ui/badge'

const statusTones: Record<string, BadgeTone> = {
  Draft: 'neutral',
  Active: 'positive',
  Completed: 'info',
  Cancelled: 'warn',
  Defaulted: 'danger',
  Inactive: 'warn',
}

export function StatusBadge({ status }: { status: string }) {
  return <Badge tone={statusTones[status] ?? 'neutral'}>{status}</Badge>
}
