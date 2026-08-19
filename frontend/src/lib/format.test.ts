import { describe, expect, it } from 'vitest'
import { formatDate, formatDateTime, money, moneyAmount, percent, toDateInputValue } from '@/lib/format'

describe('money', () => {
  it('prefixes the currency and formats to two decimals', () => {
    expect(money(1234.5, 'USD')).toBe('USD 1,234.50')
  })

  it('keeps two decimals for whole amounts', () => {
    expect(money(100, 'EUR')).toBe('EUR 100.00')
  })

  it('formats zero', () => {
    expect(money(0, 'USD')).toBe('USD 0.00')
  })

  it('formats negative amounts (reversal ledger entries)', () => {
    expect(money(-40000, 'USD')).toBe('USD -40,000.00')
  })

  it('groups large amounts with thousands separators', () => {
    expect(money(987654321.09, 'COP')).toBe('COP 987,654,321.09')
  })

  it('rounds beyond two decimals', () => {
    expect(moneyAmount(1.239)).toBe('1.24')
  })
})

describe('percent', () => {
  it('defaults to two decimals', () => {
    expect(percent(7.5)).toBe('7.50%')
  })

  it('honours a custom decimal count', () => {
    expect(percent(7.456, 1)).toBe('7.5%')
  })

  it('formats zero and negative rates', () => {
    expect(percent(0)).toBe('0.00%')
    expect(percent(-3)).toBe('-3.00%')
  })
})

describe('dates', () => {
  it('formats ISO dates as d MMM yyyy', () => {
    expect(formatDate('2026-01-05')).toBe('5 Jan 2026')
    expect(formatDate('2026-12-31')).toBe('31 Dec 2026')
  })

  it('treats naive timestamps as UTC (same output with or without Z suffix)', () => {
    expect(formatDateTime('2026-08-18T14:30:00')).toBe(formatDateTime('2026-08-18T14:30:00Z'))
  })

  it('round-trips a date input value', () => {
    expect(toDateInputValue(new Date(2026, 0, 9))).toBe('2026-01-09')
  })
})
