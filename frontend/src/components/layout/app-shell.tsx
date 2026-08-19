import { useState, type FormEvent } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Banknote, Building2, CircleUserRound, LayoutDashboard, Loader2, LogOut, Search, Sparkles } from 'lucide-react'
import { useAuth, useLogout, usePermissions } from '@/lib/auth'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/companies', label: 'Companies', icon: Building2 },
  { to: '/facilities', label: 'Facilities', icon: Banknote },
  { to: '/search', label: 'Search', icon: Search },
  { to: '/assistant', label: 'Assistant', icon: Sparkles },
]

function QuickSearch() {
  const navigate = useNavigate()
  const [value, setValue] = useState('')

  const submit = (e: FormEvent) => {
    e.preventDefault()
    const q = value.trim()
    if (!q) return
    navigate(`/search?q=${encodeURIComponent(q)}`)
    setValue('')
  }

  return (
    <form onSubmit={submit} className="relative w-full max-w-sm">
      <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-faint" />
      <input
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder="Search companies and facilities"
        aria-label="Search companies and facilities"
        className={cn(
          'h-9 w-full rounded-sm border border-line bg-paper pl-9 pr-3 text-sm text-ink',
          'placeholder:text-faint',
          'focus:border-accent focus:bg-surface focus:outline-none focus:ring-2 focus:ring-accent/15',
        )}
      />
    </form>
  )
}

const roleLabels: Record<string, string> = {
  viewer: 'Viewer',
  operator: 'Operator',
  admin: 'Admin',
}

function UserArea() {
  const { data } = useAuth()
  const { role, readOnly } = usePermissions()
  const logout = useLogout()

  if (!data?.authenticated) return null

  return (
    <div className="border-t border-white/10 px-5 py-4">
      <div className="flex items-center gap-2.5">
        <CircleUserRound className="size-6 shrink-0 text-white/40" />
        <div className="min-w-0 flex-1">
          <p className="truncate text-[13px] font-medium text-white/90">{data.name}</p>
          <div className="mt-0.5 flex items-center gap-1.5">
            {role && (
              <span className="rounded-xs bg-white/10 px-1.5 py-px text-[10px] font-semibold uppercase tracking-wider text-accent-glow">
                {roleLabels[role]}
              </span>
            )}
            {readOnly && <span className="text-[10px] text-white/45">Read-only</span>}
          </div>
        </div>
        <button
          type="button"
          onClick={() => logout.mutate()}
          disabled={logout.isPending}
          aria-label="Log out"
          title="Log out"
          className="rounded-sm p-1.5 text-white/50 transition-colors hover:bg-white/10 hover:text-white"
        >
          {logout.isPending ? (
            <Loader2 className="size-4 animate-spin" />
          ) : (
            <LogOut className="size-4" />
          )}
        </button>
      </div>
    </div>
  )
}

export function AppShell() {
  return (
    <div className="flex min-h-screen">
      <aside className="fixed inset-y-0 left-0 z-40 flex w-[228px] flex-col bg-ink text-white/90">
        <div className="border-b border-white/10 px-5 py-5">
          <p className="font-display text-lg font-semibold tracking-tight text-white">Ledgerline</p>
          <p className="mt-0.5 text-[11px] font-medium uppercase tracking-[0.16em] text-accent-glow">
            Lending operations
          </p>
        </div>
        <nav className="flex-1 space-y-0.5 px-3 py-4">
          {navItems.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2.5 rounded-sm px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-white/10 text-white'
                    : 'text-white/60 hover:bg-white/5 hover:text-white',
                )
              }
            >
              <Icon className="size-4" />
              {label}
            </NavLink>
          ))}
        </nav>
        <UserArea />
      </aside>
      <div className="flex min-w-0 flex-1 flex-col pl-[228px]">
        <header className="sticky top-0 z-30 flex h-14 items-center gap-4 border-b border-line bg-surface/95 px-6 backdrop-blur">
          <QuickSearch />
        </header>
        <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
