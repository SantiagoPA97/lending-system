import type { RepaymentType } from '@/types/api'

export const repaymentTypeLabels: Record<RepaymentType, string> = {
  Bullet: 'Bullet',
  Amortizing: 'Amortizing',
  InterestOnly: 'Interest only',
}

export function repaymentTypeLabel(type: RepaymentType | string): string {
  return repaymentTypeLabels[type as RepaymentType] ?? String(type)
}
