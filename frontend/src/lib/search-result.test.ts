import { describe, expect, it } from 'vitest'
import { isCompanyResult, normalizeAmountRange, searchResultPath } from '@/lib/search-result'

describe('search result discrimination', () => {
  it('recognises the lowercase wire value for companies', () => {
    expect(isCompanyResult({ type: 'company' })).toBe(true)
    expect(isCompanyResult({ type: 'facility' })).toBe(false)
  })

  it('routes companies to /companies and facilities to /facilities', () => {
    expect(searchResultPath({ type: 'company', id: 'abc' })).toBe('/companies/abc')
    expect(searchResultPath({ type: 'facility', id: 'def' })).toBe('/facilities/def')
  })
})

describe('normalizeAmountRange', () => {
  it('keeps an ordered range', () => {
    expect(normalizeAmountRange(100, 500)).toEqual([100, 500])
  })

  it('swaps an inverted range', () => {
    expect(normalizeAmountRange(500, 100)).toEqual([100, 500])
  })

  it('leaves open-ended ranges alone', () => {
    expect(normalizeAmountRange(undefined, 100)).toEqual([undefined, 100])
    expect(normalizeAmountRange(100, undefined)).toEqual([100, undefined])
    expect(normalizeAmountRange(undefined, undefined)).toEqual([undefined, undefined])
  })

  it('treats an equal min and max as valid', () => {
    expect(normalizeAmountRange(100, 100)).toEqual([100, 100])
  })
})
