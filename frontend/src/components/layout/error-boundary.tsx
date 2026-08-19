import { Component, type ErrorInfo, type ReactNode } from 'react'
import { TriangleAlert } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface Props {
  children: ReactNode
}

interface State {
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled render error', error, info)
  }

  render() {
    if (!this.state.error) return this.props.children
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-paper px-6 text-center">
        <TriangleAlert className="size-8 text-danger" />
        <h1 className="font-display text-xl font-semibold text-ink">Something went wrong</h1>
        <p className="max-w-md text-sm text-muted">
          The page hit an unexpected error. Reload to continue working — if it keeps happening,
          note what you were doing and report it.
        </p>
        <Button onClick={() => window.location.reload()}>Reload page</Button>
      </div>
    )
  }
}
