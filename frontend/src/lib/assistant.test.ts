import { describe, expect, it } from 'vitest'
import { HISTORY_MESSAGE_MAX_CHARS, truncateForHistory } from '@/lib/assistant'

describe('truncateForHistory', () => {
  it('returns short content unchanged', () => {
    expect(truncateForHistory('hello')).toBe('hello')
  })

  it('returns content exactly at the cap unchanged', () => {
    const content = 'a'.repeat(HISTORY_MESSAGE_MAX_CHARS)
    expect(truncateForHistory(content)).toBe(content)
  })

  it('truncates long content to the cap with a marker', () => {
    const truncated = truncateForHistory('a'.repeat(HISTORY_MESSAGE_MAX_CHARS + 1))
    expect(truncated.length).toBe(HISTORY_MESSAGE_MAX_CHARS)
    expect(truncated.endsWith('…[truncated]')).toBe(true)
  })
})
