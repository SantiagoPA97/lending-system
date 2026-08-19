import { afterEach, describe, expect, it } from 'vitest'
import { cleanup, render } from '@testing-library/react'
import { StatusBadge } from '@/components/domain/status-badge'

afterEach(cleanup)

describe('StatusBadge', () => {
  it('renders the status text', () => {
    const { getByText } = render(<StatusBadge status="Active" />)
    expect(getByText('Active')).toBeTruthy()
  })

  it.each([
    ['Active', 'text-positive'],
    ['Draft', 'text-neutral-tag'],
    ['Completed', 'text-info'],
    ['Cancelled', 'text-warn'],
    ['Defaulted', 'text-danger'],
    ['Inactive', 'text-warn'],
  ])('maps %s to the %s tone', (status, toneClass) => {
    const { getByText } = render(<StatusBadge status={status} />)
    expect(getByText(status).className).toContain(toneClass)
  })

  it('falls back to the neutral tone for unknown statuses', () => {
    const { getByText } = render(<StatusBadge status="Mystery" />)
    expect(getByText('Mystery').className).toContain('text-neutral-tag')
  })
})
