export const PERMISSIONS = {
  read: 'portfolio.read',
  managePortfolio: 'portfolio.manage',
  recordRepayments: 'repayments.record',
  reverseRepayments: 'repayments.reverse',
  closeFacilities: 'facilities.close',
} as const

export type Permission = (typeof PERMISSIONS)[keyof typeof PERMISSIONS]

export function permissionFlags(permissions: readonly string[]) {
  const can = (permission: Permission) => permissions.includes(permission)
  return {
    can,
    canRead: can(PERMISSIONS.read),
    canManagePortfolio: can(PERMISSIONS.managePortfolio),
    canRecordRepayments: can(PERMISSIONS.recordRepayments),
    canReverseRepayments: can(PERMISSIONS.reverseRepayments),
    canCloseFacilities: can(PERMISSIONS.closeFacilities),
    readOnly: !can(PERMISSIONS.managePortfolio),
  }
}
