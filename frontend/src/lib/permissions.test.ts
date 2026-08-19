import { describe, expect, it } from 'vitest'
import { PERMISSIONS, permissionFlags } from './permissions'

describe('permissionFlags', () => {
  it('grants nothing for an empty permission list', () => {
    const flags = permissionFlags([])
    expect(flags.canRead).toBe(false)
    expect(flags.canManagePortfolio).toBe(false)
    expect(flags.canRecordRepayments).toBe(false)
    expect(flags.canReverseRepayments).toBe(false)
    expect(flags.canCloseFacilities).toBe(false)
    expect(flags.readOnly).toBe(true)
  })

  it('maps each wire value to its boolean', () => {
    const flags = permissionFlags([
      PERMISSIONS.read,
      PERMISSIONS.recordRepayments,
      PERMISSIONS.reverseRepayments,
    ])
    expect(flags.canRead).toBe(true)
    expect(flags.canRecordRepayments).toBe(true)
    expect(flags.canReverseRepayments).toBe(true)
    expect(flags.canManagePortfolio).toBe(false)
    expect(flags.canCloseFacilities).toBe(false)
  })

  it('exposes can() for arbitrary permission checks', () => {
    const flags = permissionFlags([PERMISSIONS.closeFacilities])
    expect(flags.can(PERMISSIONS.closeFacilities)).toBe(true)
    expect(flags.can(PERMISSIONS.read)).toBe(false)
  })

  it('derives readOnly from lacking portfolio.manage', () => {
    expect(permissionFlags([PERMISSIONS.read]).readOnly).toBe(true)
    expect(permissionFlags([PERMISSIONS.read, PERMISSIONS.managePortfolio]).readOnly).toBe(false)
  })

  it('ignores unknown wire values', () => {
    const flags = permissionFlags(['unknown.permission'])
    expect(flags.canRead).toBe(false)
    expect(flags.readOnly).toBe(true)
  })
})
