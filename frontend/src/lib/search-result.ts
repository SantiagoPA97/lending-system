import type { SearchResultResponse } from '@/types/api'

// The backend emits lowercase discriminators ("company" / "facility").
export function isCompanyResult(result: Pick<SearchResultResponse, 'type'>): boolean {
  return result.type === 'company'
}

export function searchResultPath(result: Pick<SearchResultResponse, 'type' | 'id'>): string {
  return isCompanyResult(result) ? `/companies/${result.id}` : `/facilities/${result.id}`
}

// Swaps an inverted amount range so the API never receives min > max.
export function normalizeAmountRange(
  min: number | undefined,
  max: number | undefined,
): [number | undefined, number | undefined] {
  if (min !== undefined && max !== undefined && min > max) return [max, min]
  return [min, max]
}
