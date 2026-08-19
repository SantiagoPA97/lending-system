import { Badge, type BadgeTone } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { SkeletonRows } from '@/components/ui/skeleton'
import { formatDateTime } from '@/lib/format'
import { useAuditLog } from '@/lib/queries'
import type { AuditLogResponse } from '@/types/api'

const actionTones: Record<string, BadgeTone> = {
  Created: 'positive',
  Updated: 'info',
  Deleted: 'danger',
}

const hiddenKeys = new Set(['xmin', 'createdatutc', 'updatedatutc'])

interface FieldChange {
  key: string
  from?: string
  to: string
}

function toRecord(value: unknown): Record<string, unknown> | null {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    return value as Record<string, unknown>
  }
  return null
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '—'
  const text = typeof value === 'string' ? value : JSON.stringify(value)
  return text.length > 64 ? `${text.slice(0, 64)}…` : text
}

function changedFields(entry: AuditLogResponse): FieldChange[] {
  const oldValues = toRecord(entry.oldValues)
  const newValues = toRecord(entry.newValues)
  const source = newValues ?? oldValues
  if (!source) return []

  const changes: FieldChange[] = []
  for (const key of Object.keys(source)) {
    if (hiddenKeys.has(key.toLowerCase())) continue
    const before = oldValues?.[key]
    const after = newValues?.[key]
    if (oldValues && newValues && JSON.stringify(before) === JSON.stringify(after)) continue
    changes.push({
      key,
      from: oldValues && newValues ? formatValue(before) : undefined,
      to: formatValue(newValues ? after : before),
    })
  }
  return changes
}

export function AuditTab({ facilityId }: { facilityId: string }) {
  const { data, isLoading } = useAuditLog({
    entityType: 'Facility',
    entityId: facilityId,
    pageSize: 100,
  })

  if (isLoading) {
    return (
      <Card>
        <SkeletonRows rows={5} />
      </Card>
    )
  }

  if (!data || data.items.length === 0) {
    return (
      <Card>
        <p className="px-6 py-10 text-center text-sm text-muted">
          No audit entries for this facility yet.
        </p>
      </Card>
    )
  }

  const entries = [...data.items].sort((a, b) => b.timestampUtc.localeCompare(a.timestampUtc))

  return (
    <Card>
      <ol className="space-y-5 border-l border-line py-5 pl-5 ml-5 mr-5">
        {entries.map((entry) => {
          const changes = changedFields(entry)
          const shown = changes.slice(0, 8)
          return (
            <li key={entry.id} className="relative pl-4">
              <span className="absolute -left-[25px] top-1.5 size-2 rounded-full bg-accent ring-4 ring-surface" />
              <div className="flex flex-wrap items-center gap-2">
                <Badge tone={actionTones[entry.action] ?? 'neutral'}>{entry.action}</Badge>
                <span className="text-[13px] text-muted">{formatDateTime(entry.timestampUtc)}</span>
                <span className="text-[13px] text-faint">·</span>
                <span className="text-[13px] text-body">{entry.changedBy ?? 'system'}</span>
              </div>
              {shown.length > 0 && (
                <div className="mt-1.5 space-y-0.5">
                  {shown.map((change) => (
                    <p key={change.key} className="text-xs text-muted">
                      <span className="font-medium text-body">{change.key}</span>{' '}
                      {change.from !== undefined && (
                        <>
                          <span className="font-mono line-through decoration-line-strong">
                            {change.from}
                          </span>
                          <span className="mx-1 text-faint">→</span>
                        </>
                      )}
                      <span className="font-mono text-ink">{change.to}</span>
                    </p>
                  ))}
                  {changes.length > shown.length && (
                    <p className="text-xs text-faint">
                      +{changes.length - shown.length} more fields
                    </p>
                  )}
                </div>
              )}
            </li>
          )
        })}
      </ol>
    </Card>
  )
}
