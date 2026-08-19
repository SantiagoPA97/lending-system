import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { permissionFlags } from '@/lib/permissions'
import type { AuthMeResponse, LogoutResponse, Role } from '@/types/api'

export const authKeys = {
  me: ['auth', 'me'] as const,
}

export function useAuth() {
  return useQuery({
    queryKey: authKeys.me,
    queryFn: () => api.get<AuthMeResponse>('/auth/me'),
    staleTime: 5 * 60_000,
  })
}

const roleRank: Record<Role, number> = { viewer: 1, operator: 2, admin: 3 }

export function primaryRole(roles: Role[] | undefined): Role | null {
  if (!roles || roles.length === 0) return null
  return roles.reduce((best, role) => (roleRank[role] > roleRank[best] ? role : best))
}

export function usePermissions() {
  const { data } = useAuth()
  const roles = (data?.authenticated && data.roles) || []
  const permissions = (data?.authenticated && data.permissions) || []
  return { roles, role: primaryRole(roles), permissions, ...permissionFlags(permissions) }
}

export function useLogout() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: () => api.post<LogoutResponse>('/auth/logout'),
    onSuccess: (result) => {
      if (result.logoutUrl) {
        window.location.href = result.logoutUrl
        return
      }
      client.clear()
    },
  })
}
