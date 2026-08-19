import { describe, expect, it } from 'vitest'
import { repaymentTypeLabel, repaymentTypeLabels } from '@/lib/labels'
import { REPAYMENT_TYPES } from '@/types/api'

describe('repayment type labels', () => {
  it('has a label for every repayment type', () => {
    for (const type of REPAYMENT_TYPES) {
      expect(repaymentTypeLabels[type]).toBeTruthy()
    }
  })

  it('maps InterestOnly to a human label', () => {
    expect(repaymentTypeLabel('InterestOnly')).toBe('Interest only')
  })

  it('falls back to the raw value for unknown types', () => {
    expect(repaymentTypeLabel('Balloon')).toBe('Balloon')
  })
})
