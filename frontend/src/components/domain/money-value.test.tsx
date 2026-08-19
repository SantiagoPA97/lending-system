import { afterEach, describe, expect, it } from 'vitest'
import { cleanup, render } from '@testing-library/react'
import { MoneyValue } from '@/components/domain/money-value'

afterEach(cleanup)

describe('MoneyValue', () => {
  it('renders the currency code and the formatted amount', () => {
    const { getByText } = render(<MoneyValue amount={1234567.8} currency="USD" />)
    expect(getByText('USD')).toBeTruthy()
    expect(getByText('1,234,567.80')).toBeTruthy()
  })

  it('renders negative amounts (reversals) with two decimals', () => {
    const { getByText } = render(<MoneyValue amount={-500} currency="EUR" />)
    expect(getByText('-500.00')).toBeTruthy()
  })

  it('uses muted text tones when muted', () => {
    const { getByText } = render(<MoneyValue amount={10} currency="GBP" muted />)
    expect(getByText('10.00').className).toContain('text-muted')
    expect(getByText('GBP').className).toContain('text-faint')
  })
})
