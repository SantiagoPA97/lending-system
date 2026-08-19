import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Bar,
  BarChart,
  Cell,
  LabelList,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { money } from '@/lib/format'
import type {
  CompanyExposureResponse,
  CurrencyExposureResponse,
  CurrencyInstallmentsResponse,
  FacilityStatus,
} from '@/types/api'
import { FACILITY_STATUSES } from '@/types/api'

const ACCENT = '#17614a'
const DANGER = '#b3261e'
const INK = '#16211e'
const BODY = '#3d4a46'
const MUTED = '#67756f'
const MONO = "'IBM Plex Mono', ui-monospace, monospace"
const CURSOR_FILL = 'rgba(22, 33, 30, 0.05)'

function TipShell({ children }: { children: ReactNode }) {
  return (
    <div className="rounded-md border border-line bg-surface px-3 py-2 text-xs shadow-overlay">
      {children}
    </div>
  )
}

type BarLabelProps = {
  x?: number | string
  y?: number | string
  width?: number | string
  height?: number | string
  index?: number
}

function moneyEndLabel(text: (index: number) => string) {
  return function MoneyEndLabel(props: unknown) {
    const { x, y, width, height, index } = props as BarLabelProps
    if (index === undefined) return null
    return (
      <text
        x={Number(x ?? 0) + Number(width ?? 0) + 8}
        y={Number(y ?? 0) + Number(height ?? 0) / 2}
        dominantBaseline="central"
        fill={INK}
        fontSize={11.5}
        fontWeight={600}
        style={{ fontVariantNumeric: 'tabular-nums' }}
      >
        {text(index)}
      </text>
    )
  }
}

type StatusRow = { status: FacilityStatus; count: number }

export function FacilitiesByStatusChart({
  counts,
}: {
  counts: Partial<Record<FacilityStatus, number>>
}) {
  const data: StatusRow[] = FACILITY_STATUSES.map((status) => ({
    status,
    count: counts[status] ?? 0,
  }))
  const max = Math.max(...data.map((d) => d.count), 1)
  return (
    <ResponsiveContainer width="100%" height={216}>
      <BarChart
        data={data}
        layout="vertical"
        margin={{ top: 4, right: 40, bottom: 4, left: 0 }}
        barCategoryGap="35%"
      >
        <XAxis type="number" domain={[0, max]} hide />
        <YAxis
          type="category"
          dataKey="status"
          width={82}
          axisLine={false}
          tickLine={false}
          tick={{ fill: BODY, fontSize: 12 }}
        />
        <Tooltip
          cursor={{ fill: CURSOR_FILL }}
          content={(props) => {
            const { active, payload } = props as {
              active?: boolean
              payload?: ReadonlyArray<{ payload: StatusRow }>
            }
            if (!active || !payload?.length) return null
            const row = payload[0].payload
            return (
              <TipShell>
                <p className="font-medium text-ink">{row.status}</p>
                <p className="tabular text-muted">
                  {row.count} {row.count === 1 ? 'facility' : 'facilities'}
                </p>
              </TipShell>
            )
          }}
        />
        <Bar dataKey="count" barSize={16} radius={[0, 4, 4, 0]} isAnimationActive={false}>
          {data.map((d) => (
            <Cell key={d.status} fill={d.status === 'Defaulted' ? DANGER : ACCENT} />
          ))}
          <LabelList dataKey="count" position="right" fill={INK} fontSize={12} fontWeight={600} />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}

export function UpcomingInstallmentsChart({
  rows,
}: {
  rows: CurrencyInstallmentsResponse[]
}) {
  return (
    <ResponsiveContainer width="100%" height={rows.length * 52 + 36}>
      <BarChart
        data={rows}
        layout="vertical"
        margin={{ top: 4, right: 160, bottom: 0, left: 0 }}
        barCategoryGap="35%"
      >
        <XAxis
          type="number"
          allowDecimals={false}
          axisLine={false}
          tickLine={false}
          tick={{ fill: MUTED, fontSize: 11 }}
        />
        <YAxis
          type="category"
          dataKey="currency"
          width={44}
          axisLine={false}
          tickLine={false}
          tick={{ fill: BODY, fontSize: 12, fontFamily: MONO }}
        />
        <Tooltip
          cursor={{ fill: CURSOR_FILL }}
          content={(props) => {
            const { active, payload } = props as {
              active?: boolean
              payload?: ReadonlyArray<{ payload: CurrencyInstallmentsResponse }>
            }
            if (!active || !payload?.length) return null
            const row = payload[0].payload
            return (
              <TipShell>
                <p className="font-medium text-ink">
                  {row.count} {row.count === 1 ? 'installment' : 'installments'}
                </p>
                <p className="tabular text-muted">{money(row.amountDue, row.currency)} due</p>
              </TipShell>
            )
          }}
        />
        <Bar
          dataKey="count"
          fill={ACCENT}
          barSize={16}
          radius={[0, 4, 4, 0]}
          isAnimationActive={false}
        >
          <LabelList
            content={moneyEndLabel((i) => money(rows[i].amountDue, rows[i].currency))}
          />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}

export function ExposureChart({ group }: { group: CurrencyExposureResponse }) {
  const navigate = useNavigate()
  const data = group.companies
  return (
    <ResponsiveContainer width="100%" height={data.length * 48 + 12}>
      <BarChart
        data={data}
        layout="vertical"
        margin={{ top: 4, right: 160, bottom: 4, left: 0 }}
        barCategoryGap="35%"
      >
        <XAxis type="number" hide />
        <YAxis
          type="category"
          dataKey="companyName"
          width={132}
          axisLine={false}
          tickLine={false}
          tick={{ fill: BODY, fontSize: 12 }}
          tickFormatter={(value: string) =>
            value.length > 17 ? `${value.slice(0, 16)}…` : value
          }
        />
        <Tooltip
          cursor={{ fill: CURSOR_FILL }}
          content={(props) => {
            const { active, payload } = props as {
              active?: boolean
              payload?: ReadonlyArray<{ payload: CompanyExposureResponse }>
            }
            if (!active || !payload?.length) return null
            const row = payload[0].payload
            return (
              <TipShell>
                <p className="font-medium text-ink">{row.companyName}</p>
                <p className="tabular text-muted">
                  {money(row.outstanding, group.currency)} outstanding
                </p>
              </TipShell>
            )
          }}
        />
        <Bar
          dataKey="outstanding"
          fill={ACCENT}
          barSize={16}
          radius={[0, 4, 4, 0]}
          isAnimationActive={false}
        >
          {data.map((row) => (
            <Cell
              key={row.companyId}
              className="cursor-pointer"
              onClick={() => navigate(`/companies/${row.companyId}`)}
            />
          ))}
          <LabelList
            content={moneyEndLabel((i) => money(data[i].outstanding, group.currency))}
          />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}
