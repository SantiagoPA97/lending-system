import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { ChevronDown, ChevronRight, KeyRound, Send, Sparkles, Wrench } from 'lucide-react'
import { PageHeader } from '@/components/domain/page-header'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useAssistantQuery } from '@/lib/queries'
import { ApiError } from '@/lib/api'
import { cn } from '@/lib/utils'
import type { AssistantChatMessage, AssistantToolCallResponse } from '@/types/api'

const starterQuestions = [
  'Which facilities are more than 50% repaid?',
  'Total EUR exposure?',
  'What payments are due in the next 30 days?',
]

interface ThreadMessage {
  role: 'user' | 'assistant'
  content: string
  toolCalls?: AssistantToolCallResponse[]
  isError?: boolean
}

function renderInline(text: string): ReactNode[] {
  return text.split(/(\*\*[^*]+\*\*)/g).map((part, i) =>
    part.startsWith('**') && part.endsWith('**') ? (
      <strong key={i} className="font-semibold">
        {part.slice(2, -2)}
      </strong>
    ) : (
      part
    ),
  )
}

const listMarker = /^\s*(?:[-*•]|\d+[.)])\s+/

// Markdown-lite: paragraphs, bullet/numbered lists and **bold** — all the
// assistant is prompted to produce.
function MarkdownLite({ text }: { text: string }) {
  const blocks = text.split(/\n{2,}/).filter((b) => b.trim() !== '')
  return (
    <div className="space-y-2">
      {blocks.map((block, i) => {
        const lines = block.split('\n').filter((l) => l.trim() !== '')
        if (lines.length > 0 && lines.every((l) => listMarker.test(l))) {
          return (
            <ul key={i} className="list-disc space-y-1 pl-4">
              {lines.map((line, j) => (
                <li key={j}>{renderInline(line.replace(listMarker, ''))}</li>
              ))}
            </ul>
          )
        }
        return <p key={i}>{renderInline(lines.join(' '))}</p>
      })}
    </div>
  )
}

function ToolCallDetails({ toolCalls }: { toolCalls: AssistantToolCallResponse[] }) {
  const [open, setOpen] = useState(false)
  if (toolCalls.length === 0) return null
  return (
    <div className="mt-2 border-t border-line pt-2">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1 text-xs font-medium text-muted transition-colors hover:text-ink"
      >
        {open ? <ChevronDown className="size-3.5" /> : <ChevronRight className="size-3.5" />}
        <Wrench className="size-3" />
        {toolCalls.length} tool{toolCalls.length === 1 ? '' : 's'} used
      </button>
      {open && (
        <ul className="mt-1.5 space-y-1">
          {toolCalls.map((call, i) => (
            <li key={i} className="flex items-baseline gap-2 text-xs text-muted">
              <code className="shrink-0 rounded-xs bg-paper px-1.5 py-0.5 font-mono text-[11px] text-body">
                {call.name}
              </code>
              <span>{call.summary}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function MessageBubble({ message }: { message: ThreadMessage }) {
  if (message.role === 'user') {
    return (
      <div className="flex justify-end">
        <div className="max-w-[85%] rounded-lg rounded-br-sm bg-accent px-4 py-2.5 text-sm text-white">
          {message.content}
        </div>
      </div>
    )
  }
  return (
    <div className="flex justify-start">
      <div
        className={cn(
          'max-w-[85%] rounded-lg rounded-bl-sm border px-4 py-2.5 text-sm',
          message.isError
            ? 'border-danger/30 bg-danger/5 text-danger'
            : 'border-line bg-paper text-body',
        )}
      >
        <MarkdownLite text={message.content} />
        {message.toolCalls && <ToolCallDetails toolCalls={message.toolCalls} />}
      </div>
    </div>
  )
}

function ThinkingBubble() {
  return (
    <div className="flex justify-start">
      <div className="w-[60%] max-w-[85%] space-y-2 rounded-lg rounded-bl-sm border border-line bg-paper px-4 py-3">
        <Skeleton className="h-3.5 w-full" />
        <Skeleton className="h-3.5 w-4/5" />
        <Skeleton className="h-3.5 w-3/5" />
      </div>
    </div>
  )
}

function NotConfiguredState() {
  return (
    <div className="flex flex-col items-center gap-3 px-6 py-14 text-center">
      <span className="flex size-11 items-center justify-center rounded-full bg-accent-soft text-accent">
        <KeyRound className="size-5" />
      </span>
      <p className="font-display text-[15px] font-semibold text-ink">Assistant not configured</p>
      <p className="max-w-md text-sm text-muted">
        The AI assistant needs an Anthropic API key. Set the{' '}
        <code className="rounded-xs bg-paper px-1.5 py-0.5 font-mono text-[12px] text-body">
          ANTHROPIC_API_KEY
        </code>{' '}
        environment variable on the API server and restart it — no other setup is required.
      </p>
    </div>
  )
}

function EmptyState({ onPick }: { onPick: (question: string) => void }) {
  return (
    <div className="flex flex-col items-center gap-4 px-6 py-12 text-center">
      <span className="flex size-11 items-center justify-center rounded-full bg-accent-soft text-accent">
        <Sparkles className="size-5" />
      </span>
      <div>
        <p className="font-display text-[15px] font-semibold text-ink">Ask about the portfolio</p>
        <p className="mt-1 max-w-md text-sm text-muted">
          Answers are grounded in live portfolio data — facilities, companies, schedules and
          repayments. Amounts are always reported per currency.
        </p>
      </div>
      <div className="flex flex-wrap justify-center gap-2">
        {starterQuestions.map((question) => (
          <button
            key={question}
            type="button"
            onClick={() => onPick(question)}
            className={cn(
              'rounded-full border border-line bg-surface px-3.5 py-1.5 text-[13px] text-body transition-colors',
              'hover:border-accent hover:bg-accent-soft hover:text-accent',
            )}
          >
            {question}
          </button>
        ))}
      </div>
    </div>
  )
}

export default function Assistant() {
  const [messages, setMessages] = useState<ThreadMessage[]>([])
  const [input, setInput] = useState('')
  const [configured, setConfigured] = useState(true)
  const query = useAssistantQuery()
  const endRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [messages, query.isPending])

  const send = (question: string) => {
    const trimmed = question.trim()
    if (!trimmed || query.isPending) return

    const history: AssistantChatMessage[] = messages
      .filter((m) => !m.isError)
      .slice(-10)
      .map((m) => ({ role: m.role, content: m.content }))

    setMessages((prev) => [...prev, { role: 'user', content: trimmed }])
    setInput('')

    query.mutate(
      { question: trimmed, history },
      {
        onSuccess: (response) => {
          if (!response.configured) {
            setConfigured(false)
            return
          }
          setMessages((prev) => [
            ...prev,
            {
              role: 'assistant',
              content: response.answer ?? '',
              toolCalls: response.toolCalls,
            },
          ])
        },
        onError: (error) => {
          const detail =
            error instanceof ApiError
              ? error.detail || error.title
              : 'Something went wrong while contacting the assistant.'
          setMessages((prev) => [...prev, { role: 'assistant', content: detail, isError: true }])
        },
      },
    )
  }

  const submit = (e: FormEvent) => {
    e.preventDefault()
    send(input)
  }

  return (
    <>
      <PageHeader
        eyebrow="AI"
        title="Assistant"
        description="Ask questions about companies, facilities, repayments and exposure."
      />
      <Card className="flex min-h-[60vh] flex-col">
        <div className="flex-1 overflow-y-auto">
          {!configured ? (
            <NotConfiguredState />
          ) : messages.length === 0 ? (
            <EmptyState onPick={send} />
          ) : (
            <div className="space-y-4 px-5 py-5">
              {messages.map((message, i) => (
                <MessageBubble key={i} message={message} />
              ))}
              {query.isPending && <ThinkingBubble />}
              <div ref={endRef} />
            </div>
          )}
        </div>
        <form onSubmit={submit} className="flex items-center gap-2 border-t border-line px-4 py-3">
          <input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder={configured ? 'Ask about the portfolio…' : 'Assistant is not configured'}
            aria-label="Ask the assistant"
            disabled={!configured || query.isPending}
            className={cn(
              'h-9 flex-1 rounded-sm border border-line bg-paper px-3 text-sm text-ink',
              'placeholder:text-faint disabled:opacity-60',
              'focus:border-accent focus:bg-surface focus:outline-none focus:ring-2 focus:ring-accent/15',
            )}
          />
          <Button
            type="submit"
            size="md"
            loading={query.isPending}
            disabled={!configured || input.trim() === ''}
            aria-label="Send question"
          >
            {!query.isPending && <Send className="size-4" />}
            Send
          </Button>
        </form>
      </Card>
    </>
  )
}
