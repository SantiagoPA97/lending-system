import { useState } from 'react'
import { ArrowRight, ChevronLeft, ChevronRight, History } from 'lucide-react'
import { Card, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge, type BadgeTone } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { SkeletonRows } from '@/components/ui/skeleton'
import { formatDateTime } from '@/lib/format'
import { useAuditLog } from '@/lib/queries'
import type { AuditLogResponse } from '@/types/api'

const actionTones: Record<string, BadgeTone> = {
  Created: 'positive',
  Updated: 'info',
  Deleted: 'danger',
}

type FieldChange = { field: string; from: string | null; to: string | null }

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'string') return value
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return JSON.stringify(value)
}

function diffValues(entry: AuditLogResponse): FieldChange[] {
  const oldObj =
    entry.oldValues && typeof entry.oldValues === 'object'
      ? (entry.oldValues as Record<string, unknown>)
      : null
  const newObj =
    entry.newValues && typeof entry.newValues === 'object'
      ? (entry.newValues as Record<string, unknown>)
      : null
  if (!newObj && !oldObj) return []
  if (!oldObj) {
    return Object.entries(newObj ?? {})
      .filter(([, v]) => v !== null && v !== undefined && v !== '')
      .slice(0, 6)
      .map(([field, v]) => ({ field, from: null, to: formatValue(v) }))
  }
  const keys = new Set([...Object.keys(oldObj), ...(newObj ? Object.keys(newObj) : [])])
  const changes: FieldChange[] = []
  for (const key of keys) {
    const from = oldObj[key]
    const to = newObj?.[key]
    if (JSON.stringify(from) !== JSON.stringify(to)) {
      changes.push({ field: key, from: formatValue(from), to: formatValue(to) })
    }
  }
  return changes
}

export function AuditTrailCard({
  entityType,
  entityId,
}: {
  entityType: string
  entityId: string
}) {
  const [page, setPage] = useState(1)
  const pageSize = 8
  const { data, isLoading } = useAuditLog({ entityType, entityId, page, pageSize })
  const pageCount = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1

  return (
    <Card>
      <CardHeader>
        <div className="flex items-baseline gap-2">
          <CardTitle>Audit trail</CardTitle>
          {data && <span className="tabular text-[13px] text-muted">{data.total}</span>}
        </div>
      </CardHeader>
      {isLoading ? (
        <SkeletonRows rows={4} />
      ) : !data || data.items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 px-6 py-10 text-center">
          <History className="size-6 text-faint" />
          <p className="text-sm text-muted">No recorded changes yet.</p>
        </div>
      ) : (
        <>
          <ul className="divide-y divide-line">
            {data.items.map((entry) => {
              const changes = diffValues(entry)
              return (
                <li key={entry.id} className="px-5 py-3">
                  <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                    <Badge tone={actionTones[entry.action] ?? 'neutral'}>{entry.action}</Badge>
                    <span className="text-[13px] text-muted">
                      {formatDateTime(entry.timestampUtc)}
                    </span>
                    <span className="text-[13px] text-faint">
                      by {entry.changedBy ?? 'system'}
                    </span>
                  </div>
                  {changes.length > 0 && (
                    <dl className="mt-2 grid gap-1">
                      {changes.map((change) => (
                        <div
                          key={change.field}
                          className="flex flex-wrap items-baseline gap-x-2 text-[13px]"
                        >
                          <dt className="w-40 shrink-0 truncate text-muted">{change.field}</dt>
                          <dd className="flex min-w-0 flex-wrap items-baseline gap-x-1.5 font-mono text-xs">
                            {change.from !== null && (
                              <>
                                <span className="break-all text-faint line-through decoration-line-strong">
                                  {change.from}
                                </span>
                                <ArrowRight className="size-3 shrink-0 self-center text-faint" />
                              </>
                            )}
                            <span className="break-all text-ink">{change.to ?? '—'}</span>
                          </dd>
                        </div>
                      ))}
                    </dl>
                  )}
                </li>
              )
            })}
          </ul>
          {data.total > data.pageSize && (
            <div className="flex items-center justify-between border-t border-line px-5 py-2.5">
              <p className="tabular text-[13px] text-muted">
                Page {data.page} of {pageCount}
              </p>
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                  aria-label="Previous page"
                >
                  <ChevronLeft className="size-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page >= pageCount}
                  onClick={() => setPage((p) => p + 1)}
                  aria-label="Next page"
                >
                  <ChevronRight className="size-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </Card>
  )
}
