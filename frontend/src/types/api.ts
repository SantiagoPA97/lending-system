export type CompanyStatus = 'Active' | 'Inactive'
export type FacilityStatus = 'Draft' | 'Active' | 'Completed' | 'Cancelled' | 'Defaulted'
export type RepaymentType = 'Bullet' | 'Amortizing' | 'InterestOnly'
export type Currency = 'USD' | 'EUR' | 'GBP' | 'COP'

export const COMPANY_STATUSES: CompanyStatus[] = ['Active', 'Inactive']
export const FACILITY_STATUSES: FacilityStatus[] = ['Draft', 'Active', 'Completed', 'Cancelled', 'Defaulted']
export const REPAYMENT_TYPES: RepaymentType[] = ['Bullet', 'Amortizing', 'InterestOnly']
export const CURRENCIES: Currency[] = ['USD', 'EUR', 'GBP', 'COP']

export interface Paged<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface CompanyRequest {
  name: string
  legalName: string
  registrationNumber: string
  country: string
  industry: string
  contactEmail: string
}

export interface CompanyResponse {
  id: string
  name: string
  legalName: string
  registrationNumber: string
  country: string
  industry: string
  contactEmail: string
  status: CompanyStatus
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CompanyFacilitySummaryResponse {
  id: string
  reference: string
  commitmentAmount: number
  currency: Currency
  annualInterestRate: number
  termMonths: number
  startDate: string
  repaymentType: RepaymentType
  status: FacilityStatus
  outstandingPrincipal: number
}

export interface CompanyDetailResponse extends CompanyResponse {
  facilities: CompanyFacilitySummaryResponse[]
}

export interface FacilityTermsRequest {
  commitmentAmount: number
  currency: Currency
  annualInterestRate: number
  termMonths: number
  startDate: string
  repaymentType: RepaymentType
}

export interface CreateFacilityRequest extends FacilityTermsRequest {
  companyId: string
}

export type UpdateFacilityRequest = FacilityTermsRequest
export type SchedulePreviewRequest = FacilityTermsRequest

export interface FacilityResponse {
  id: string
  reference: string
  companyId: string
  companyName: string
  commitmentAmount: number
  currency: Currency
  annualInterestRate: number
  termMonths: number
  startDate: string
  repaymentType: RepaymentType
  status: FacilityStatus
  outstandingPrincipal: number
  createdAtUtc: string
  updatedAtUtc: string
}

export interface FacilityCompanySummaryResponse {
  id: string
  name: string
  status: CompanyStatus
  country: string
  industry: string
}

export interface FacilityDetailResponse {
  id: string
  reference: string
  company: FacilityCompanySummaryResponse
  commitmentAmount: number
  currency: Currency
  annualInterestRate: number
  termMonths: number
  startDate: string
  repaymentType: RepaymentType
  status: FacilityStatus
  outstandingPrincipal: number
  totalPrincipalPaid: number
  totalInterestPaid: number
  nextDueInstallment: ScheduleItemResponse | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface ScheduleItemResponse {
  period: number
  dueDate: string
  principalDue: number
  interestDue: number
  totalDue: number
  remainingBalance: number
  principalPaid: number
  interestPaid: number
  isSettled: boolean
}

export interface ScheduleResponse {
  facilityId: string
  reference: string
  currency: Currency
  items: ScheduleItemResponse[]
  totalPrincipal: number
  totalInterest: number
  totalPayable: number
}

export interface SchedulePreviewInstallmentResponse {
  period: number
  dueDate: string
  principalDue: number
  interestDue: number
  totalDue: number
  remainingBalance: number
}

export interface SchedulePreviewResponse {
  currency: Currency
  installments: SchedulePreviewInstallmentResponse[]
  totalPrincipal: number
  totalInterest: number
  totalPayable: number
}

export interface RecordRepaymentRequest {
  amount: number
  currency: Currency
  paymentDate: string
  note?: string | null
}

export interface ReverseRepaymentRequest {
  reversalDate?: string | null
}

export interface RepaymentAllocationResponse {
  period: number
  interest: number
  principal: number
}

export interface RepaymentResponse {
  id: string
  facilityId: string
  amount: number
  currency: Currency
  paymentDate: string
  principalApplied: number
  interestApplied: number
  isReversal: boolean
  reversesRepaymentId: string | null
  reversedByRepaymentId: string | null
  createdAtUtc: string
  allocations: RepaymentAllocationResponse[]
}

export interface RepaymentResultResponse {
  repayment: RepaymentResponse
  interestPaid: number
  principalPaid: number
  newOutstanding: number
  facilityStatus: FacilityStatus
}

export interface SearchResultResponse {
  type: string
  id: string
  name: string
  companyName: string
  status: string
  repaymentType: RepaymentType | null
  currency: Currency | null
  amount: number | null
  outstanding: number | null
  rank: number
}

export interface DashboardMetricsResponse {
  generatedAtUtc: string
  portfolio: CurrencyPortfolioResponse[]
  facilitiesByStatus: Partial<Record<FacilityStatus, number>>
  repaymentsLast30Days: CurrencyRepaymentsResponse[]
  upcomingInstallmentsNext30Days: CurrencyInstallmentsResponse[]
  overdueInstallments: OverdueInstallmentsResponse
  topCompanyExposures: CurrencyExposureResponse[]
}

export interface CurrencyPortfolioResponse {
  currency: Currency
  facilityCount: number
  totalCommitted: number
  totalOutstanding: number
}

export interface CurrencyRepaymentsResponse {
  currency: Currency
  count: number
  netAmount: number
}

export interface CurrencyInstallmentsResponse {
  currency: Currency
  count: number
  amountDue: number
}

export interface OverdueInstallmentsResponse {
  count: number
  oldestDueDate: string | null
}

export interface CurrencyExposureResponse {
  currency: Currency
  companies: CompanyExposureResponse[]
}

export interface CompanyExposureResponse {
  companyId: string
  companyName: string
  outstanding: number
}

export interface AuditLogResponse {
  id: number
  entityType: string
  entityId: string
  action: string
  changedBy: string | null
  timestampUtc: string
  oldValues: unknown
  newValues: unknown
}

export type AuthMode = 'Bypass' | 'Auth0'

export type Role = 'viewer' | 'operator' | 'admin'

export interface AuthMeResponse {
  authenticated: boolean
  name?: string | null
  email?: string | null
  roles?: Role[]
  mode: AuthMode
}

export interface LogoutResponse {
  logoutUrl: string | null
}
