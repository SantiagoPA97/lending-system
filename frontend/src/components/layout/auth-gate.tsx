import { useEffect, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { ArrowRight, Eye, Loader2, LogIn, ShieldCheck, Wrench } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { setUnauthorizedHandler } from '@/lib/api'
import { authKeys, useAuth } from '@/lib/auth'
import type { AuthMode, Role } from '@/types/api'

const demoRoles: { role: Role; label: string; blurb: string; icon: typeof Eye }[] = [
  { role: 'viewer', label: 'Viewer', blurb: 'Browse the book, read-only', icon: Eye },
  { role: 'operator', label: 'Operator', blurb: 'Manage facilities and repayments', icon: Wrench },
  { role: 'admin', label: 'Admin', blurb: 'Full control, including reversals', icon: ShieldCheck },
]

export function AuthGate({ children }: { children: ReactNode }) {
  const client = useQueryClient()
  const { data, isLoading } = useAuth()

  useEffect(() => {
    setUnauthorizedHandler(() => {
      void client.invalidateQueries({ queryKey: authKeys.me })
    })
    return () => setUnauthorizedHandler(null)
  }, [client])

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-paper">
        <Loader2 className="size-6 animate-spin text-accent" />
      </div>
    )
  }

  if (!data?.authenticated) return <LoginScreen mode={data?.mode ?? 'Bypass'} />

  return <>{children}</>
}

function LoginScreen({ mode }: { mode: AuthMode }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-ink px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <p className="font-display text-2xl font-semibold tracking-tight text-white">Ledgerline</p>
          <p className="mt-1 text-[11px] font-medium uppercase tracking-[0.16em] text-accent-glow">
            Lending operations
          </p>
        </div>
        <div className="rounded-md border border-line bg-surface p-6 shadow-card">
          {mode === 'Bypass' ? <DemoAccess /> : <Auth0SignIn />}
        </div>
        <p className="mt-4 text-center text-xs text-white/40">
          {mode === 'Bypass'
            ? 'Demo mode — sessions are local and roles are simulated.'
            : 'Access is managed by your organisation.'}
        </p>
      </div>
    </div>
  )
}

function DemoAccess() {
  return (
    <>
      <h1 className="text-sm font-semibold text-ink">Demo access</h1>
      <p className="mt-1 text-[13px] text-muted">Pick a role to explore the workspace.</p>
      <div className="mt-4 grid gap-2">
        {demoRoles.map(({ role, label, blurb, icon: Icon }) => (
          <a
            key={role}
            href={`/auth/dev-login?role=${role}`}
            className="group flex items-center gap-3 rounded-sm border border-line px-3.5 py-3 transition-colors hover:border-accent hover:bg-accent-soft/40"
          >
            <span className="flex size-8 items-center justify-center rounded-sm bg-accent-soft text-accent">
              <Icon className="size-4" />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block text-sm font-medium text-ink">{label}</span>
              <span className="block text-xs text-muted">{blurb}</span>
            </span>
            <ArrowRight className="size-4 text-faint transition-transform group-hover:translate-x-0.5 group-hover:text-accent" />
          </a>
        ))}
      </div>
    </>
  )
}

function Auth0SignIn() {
  return (
    <>
      <h1 className="text-sm font-semibold text-ink">Sign in</h1>
      <p className="mt-1 text-[13px] text-muted">
        Continue with your organisation account to access the lending workspace.
      </p>
      <Button
        className="mt-4 w-full"
        onClick={() => {
          window.location.href = `/auth/login?returnUrl=${encodeURIComponent(
            window.location.pathname + window.location.search,
          )}`
        }}
      >
        <LogIn className="size-4" />
        Sign in
      </Button>
    </>
  )
}
