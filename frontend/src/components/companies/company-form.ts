import { z } from 'zod'
import type { UseFormSetError } from 'react-hook-form'
import { ApiError } from '@/lib/api'
import type { CompanyRequest, CompanyResponse } from '@/types/api'

export const companySchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(200, 'Keep it under 200 characters'),
  legalName: z.string().trim().min(1, 'Legal name is required').max(300, 'Keep it under 300 characters'),
  registrationNumber: z
    .string()
    .trim()
    .min(1, 'Registration number is required')
    .max(100, 'Keep it under 100 characters'),
  country: z.string().trim().regex(/^[A-Za-z]{2}$/, 'Two-letter ISO code, e.g. CO'),
  industry: z.string().trim().min(1, 'Industry is required').max(100, 'Keep it under 100 characters'),
  contactEmail: z.email('Enter a valid email address').max(320, 'Keep it under 320 characters'),
})

export type CompanyFormValues = z.infer<typeof companySchema>

export const emptyCompanyValues: CompanyFormValues = {
  name: '',
  legalName: '',
  registrationNumber: '',
  country: '',
  industry: '',
  contactEmail: '',
}

export function companyToValues(company: CompanyResponse): CompanyFormValues {
  return {
    name: company.name,
    legalName: company.legalName,
    registrationNumber: company.registrationNumber,
    country: company.country,
    industry: company.industry,
    contactEmail: company.contactEmail,
  }
}

export function toCompanyRequest(values: CompanyFormValues): CompanyRequest {
  return { ...values, country: values.country.toUpperCase() }
}

const errorMessages: Record<string, string> = {
  'company.duplicate_name': 'A company with this name already exists.',
  'company.invalid_transition': 'The company is already in that state.',
  'company.inactive': 'This company is inactive and cannot receive new facilities.',
}

export function apiErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 409) return 'Someone else changed this record. Reload and retry.'
    if (err.errorCode && errorMessages[err.errorCode]) return errorMessages[err.errorCode]
    return err.detail ?? err.title
  }
  return 'Something went wrong. Try again.'
}

export function applyFieldErrors(
  err: unknown,
  setError: UseFormSetError<CompanyFormValues>,
): boolean {
  if (!(err instanceof ApiError) || !err.errors) return false
  let applied = false
  for (const [key, messages] of Object.entries(err.errors)) {
    const field = (key.charAt(0).toLowerCase() + key.slice(1)) as keyof CompanyFormValues
    if (field in emptyCompanyValues && messages.length > 0) {
      setError(field, { type: 'server', message: messages[0] })
      applied = true
    }
  }
  return applied
}
