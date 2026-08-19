import { ApiError } from '@/lib/api'

const codeMessages: Record<string, string> = {
  'facility.not_active': 'Repayments can only be recorded while the facility is active.',
  'facility.not_editable': 'Only draft facilities can be edited.',
  'facility.invalid_transition': 'That action is not available in the current status.',
  'facility.invalid_terms': 'The facility terms are invalid.',
  'company.inactive': 'Inactive companies cannot receive new facilities.',
  'repayment.currency_mismatch': 'The payment currency must match the facility currency.',
  'repayment.invalid_amount': 'The repayment amount must be positive.',
  'repayment.exceeds_outstanding':
    'The amount exceeds what is payable — outstanding principal plus unpaid interest due.',
  'repayment.already_reversed': 'This repayment has already been reversed.',
  'repayment.cannot_reverse_reversal': 'A reversal entry cannot be reversed.',
  'repayment.not_found': 'This repayment no longer exists on the facility.',
}

export function facilityErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 409) return 'This facility changed in another session — reload and retry.'
    if (err.errorCode && codeMessages[err.errorCode]) return codeMessages[err.errorCode]
    return err.detail ?? err.title
  }
  return 'Something went wrong. Try again.'
}
