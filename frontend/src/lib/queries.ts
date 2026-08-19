import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { api } from '@/lib/api'
import type {
  AuditLogResponse,
  CompanyDetailResponse,
  CompanyRequest,
  CompanyResponse,
  CreateFacilityRequest,
  DashboardMetricsResponse,
  FacilityDetailResponse,
  FacilityResponse,
  Paged,
  RecordRepaymentRequest,
  RepaymentResponse,
  RepaymentResultResponse,
  ReverseRepaymentRequest,
  ScheduleResponse,
  SchedulePreviewRequest,
  SchedulePreviewResponse,
  SearchResultResponse,
  UpdateFacilityRequest,
} from '@/types/api'

export type CompanyListParams = {
  page?: number
  pageSize?: number
  sort?: string
  status?: string
  q?: string
}

export type FacilityListParams = {
  page?: number
  pageSize?: number
  sort?: string
  status?: string
  repaymentType?: string
  currency?: string
  companyId?: string
}

export type SearchParams = {
  q?: string
  type?: string
  status?: string
  repaymentType?: string
  currency?: string
  minAmount?: number
  maxAmount?: number
  page?: number
  pageSize?: number
}

export type AuditListParams = {
  entityType?: string
  entityId?: string
  page?: number
  pageSize?: number
}

export const queryKeys = {
  companies: ['companies'] as const,
  companyList: (params: CompanyListParams) => ['companies', 'list', params] as const,
  company: (id: string) => ['companies', 'detail', id] as const,
  facilities: ['facilities'] as const,
  facilityList: (params: FacilityListParams) => ['facilities', 'list', params] as const,
  facility: (id: string) => ['facilities', 'detail', id] as const,
  schedule: (facilityId: string) => ['facilities', 'schedule', facilityId] as const,
  repayments: (facilityId: string) => ['facilities', 'repayments', facilityId] as const,
  search: (params: SearchParams) => ['search', params] as const,
  dashboard: ['dashboard'] as const,
  audit: (params: AuditListParams) => ['audit', params] as const,
}

function invalidateCompanyData(client: QueryClient) {
  client.invalidateQueries({ queryKey: queryKeys.companies })
  client.invalidateQueries({ queryKey: ['search'] })
  client.invalidateQueries({ queryKey: ['audit'] })
}

function invalidateFacilityData(client: QueryClient) {
  client.invalidateQueries({ queryKey: queryKeys.facilities })
  client.invalidateQueries({ queryKey: queryKeys.companies })
  client.invalidateQueries({ queryKey: queryKeys.dashboard })
  client.invalidateQueries({ queryKey: ['search'] })
  client.invalidateQueries({ queryKey: ['audit'] })
}

export function useCompanies(params: CompanyListParams = {}) {
  return useQuery({
    queryKey: queryKeys.companyList(params),
    queryFn: () => api.get<Paged<CompanyResponse>>('/api/companies', params),
  })
}

export function useCompany(id: string) {
  return useQuery({
    queryKey: queryKeys.company(id),
    queryFn: () => api.get<CompanyDetailResponse>(`/api/companies/${id}`),
    enabled: !!id,
  })
}

export function useCreateCompany() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (body: CompanyRequest) => api.post<CompanyResponse>('/api/companies', body),
    onSuccess: () => invalidateCompanyData(client),
  })
}

export function useUpdateCompany(id: string) {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (body: CompanyRequest) => api.put<CompanyResponse>(`/api/companies/${id}`, body),
    onSuccess: () => invalidateCompanyData(client),
  })
}

export function useActivateCompany() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post<CompanyResponse>(`/api/companies/${id}/activate`),
    onSuccess: () => invalidateCompanyData(client),
  })
}

export function useDeactivateCompany() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post<CompanyResponse>(`/api/companies/${id}/deactivate`),
    onSuccess: () => invalidateCompanyData(client),
  })
}

export function useFacilities(params: FacilityListParams = {}) {
  return useQuery({
    queryKey: queryKeys.facilityList(params),
    queryFn: () => api.get<Paged<FacilityResponse>>('/api/facilities', params),
  })
}

export function useFacility(id: string) {
  return useQuery({
    queryKey: queryKeys.facility(id),
    queryFn: () => api.get<FacilityDetailResponse>(`/api/facilities/${id}`),
    enabled: !!id,
  })
}

export function useCreateFacility() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateFacilityRequest) => api.post<FacilityResponse>('/api/facilities', body),
    onSuccess: () => invalidateFacilityData(client),
  })
}

export function useUpdateFacility(id: string) {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateFacilityRequest) => api.put<FacilityResponse>(`/api/facilities/${id}`, body),
    onSuccess: () => invalidateFacilityData(client),
  })
}

export function useActivateFacility() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post<FacilityResponse>(`/api/facilities/${id}/activate`),
    onSuccess: () => invalidateFacilityData(client),
  })
}

export function useCancelFacility() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post<FacilityResponse>(`/api/facilities/${id}/cancel`),
    onSuccess: () => invalidateFacilityData(client),
  })
}

export function useDefaultFacility() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post<FacilityResponse>(`/api/facilities/${id}/default`),
    onSuccess: () => invalidateFacilityData(client),
  })
}

export function useSchedule(facilityId: string) {
  return useQuery({
    queryKey: queryKeys.schedule(facilityId),
    queryFn: () => api.get<ScheduleResponse>(`/api/facilities/${facilityId}/schedule`),
    enabled: !!facilityId,
  })
}

export function useSchedulePreview() {
  return useMutation({
    mutationFn: (body: SchedulePreviewRequest) =>
      api.post<SchedulePreviewResponse>('/api/facilities/schedule-preview', body),
  })
}

export function useRepayments(facilityId: string) {
  return useQuery({
    queryKey: queryKeys.repayments(facilityId),
    queryFn: () => api.get<RepaymentResponse[]>(`/api/facilities/${facilityId}/repayments`),
    enabled: !!facilityId,
  })
}

export function useRecordRepayment(facilityId: string) {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (body: RecordRepaymentRequest) =>
      api.post<RepaymentResultResponse>(`/api/facilities/${facilityId}/repayments`, body),
    onSuccess: () => invalidateFacilityData(client),
  })
}

export function useReverseRepayment() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: ({ repaymentId, body }: { repaymentId: string; body?: ReverseRepaymentRequest }) =>
      api.post<RepaymentResultResponse>(`/api/repayments/${repaymentId}/reverse`, body ?? {}),
    onSuccess: () => invalidateFacilityData(client),
  })
}

export function useSearch(params: SearchParams, enabled = true) {
  return useQuery({
    queryKey: queryKeys.search(params),
    queryFn: () => api.get<Paged<SearchResultResponse>>('/api/search', params),
    enabled,
  })
}

export function useDashboardMetrics() {
  return useQuery({
    queryKey: queryKeys.dashboard,
    queryFn: () => api.get<DashboardMetricsResponse>('/api/dashboard/metrics'),
    refetchInterval: 60_000,
  })
}

export function useAuditLog(params: AuditListParams = {}) {
  return useQuery({
    queryKey: queryKeys.audit(params),
    queryFn: () => api.get<Paged<AuditLogResponse>>('/api/audit', params),
  })
}
