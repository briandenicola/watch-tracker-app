import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { AuthResponse, LoginCredentials, RegisterCredentials } from '@/types'
import { api } from '@/services/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const user = ref<{ username: string; email: string; role: string; profileImage?: string } | null>(
    JSON.parse(localStorage.getItem('user') || 'null')
  )

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')

  function setAuth(data: AuthResponse) {
    token.value = data.token
    refreshToken.value = data.refreshToken || null
    user.value = { username: data.username, email: data.email, role: data.role, profileImage: data.profileImage }
    localStorage.setItem('token', data.token)
    if (data.refreshToken) localStorage.setItem('refreshToken', data.refreshToken)
    localStorage.setItem('user', JSON.stringify(user.value))
  }

  function clearAuth() {
    token.value = null
    refreshToken.value = null
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('user')
  }

  async function login(credentials: LoginCredentials) {
    const { data } = await api.post<AuthResponse>('/api/auth/login', credentials)
    setAuth(data)
  }

  async function register(credentials: RegisterCredentials) {
    const { data } = await api.post<AuthResponse>('/api/auth/register', credentials)
    setAuth(data)
  }

  async function refresh(): Promise<boolean> {
    if (!refreshToken.value) return false
    try {
      const { data } = await api.post<AuthResponse>('/api/auth/refresh', { refreshToken: refreshToken.value })
      setAuth(data)
      return true
    } catch {
      clearAuth()
      return false
    }
  }

  function logout() {
    clearAuth()
  }

  return { token, refreshToken, user, isAuthenticated, isAdmin, login, register, refresh, logout, setAuth }
})
