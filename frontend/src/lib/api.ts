export class ApiError extends Error {
  status: number
  title: string
  detail?: string
  errorCode?: string
  errors?: Record<string, string[]>

  constructor(
    status: number,
    title: string,
    detail?: string,
    errorCode?: string,
    errors?: Record<string, string[]>,
  ) {
    super(detail || title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.errorCode = errorCode
    this.errors = errors
  }
}

export type QueryParams = {
  [key: string]: string | number | boolean | null | undefined
}

export function withQuery(path: string, params?: QueryParams): string {
  if (!params) return path
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }
  const qs = search.toString()
  return qs ? `${path}?${qs}` : path
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await fetch(path, {
    method,
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      ...(method !== 'GET' ? { 'X-Requested-With': 'fetch' } : {}),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    let problem: Record<string, unknown> = {}
    try {
      problem = await response.json()
    } catch {
      problem = {}
    }
    throw new ApiError(
      response.status,
      typeof problem.title === 'string' ? problem.title : response.statusText || 'Request failed',
      typeof problem.detail === 'string' ? problem.detail : undefined,
      typeof problem.errorCode === 'string' ? problem.errorCode : undefined,
      problem.errors as Record<string, string[]> | undefined,
    )
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const api = {
  get: <T>(path: string, params?: QueryParams) => request<T>('GET', withQuery(path, params)),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  delete: <T>(path: string) => request<T>('DELETE', path),
}
