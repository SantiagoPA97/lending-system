import { useState, type FormEvent } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Banknote, Building2, CircleUserRound, LayoutDashboard, Search } from 'lucide-react'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/companies', label: 'Companies', icon: Building2 },
  { to: '/facilities', label: 'Facilities', icon: Banknote },
  { to: '/search', label: 'Search', icon: Search },
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
        <div className="border-t border-white/10 px-5 py-4">
          <div className="flex items-center gap-2.5">
            <CircleUserRound className="size-6 text-white/40" />
            <div className="min-w-0">
              <p className="truncate text-[13px] font-medium text-white/90">Operations user</p>
              <p className="text-[11px] text-white/45">Sign-in coming soon</p>
            </div>
          </div>
        </div>
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
