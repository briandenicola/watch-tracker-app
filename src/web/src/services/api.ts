import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'

export const api = axios.create({
  baseURL: '',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  return config
})

let isRefreshing = false
let failedQueue: Array<{ resolve: (token: string) => void; reject: (err: unknown) => void }> = []

function processQueue(error: unknown, token: string | null) {
  failedQueue.forEach((prom) => {
    if (token) prom.resolve(token)
    else prom.reject(error)
  })
  failedQueue = []
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`
          return api(originalRequest)
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      const auth = useAuthStore()
      const success = await auth.refresh()
      isRefreshing = false

      if (success) {
        processQueue(null, auth.token)
        originalRequest.headers.Authorization = `Bearer ${auth.token}`
        return api(originalRequest)
      } else {
        processQueue(error, null)
        router.push({ name: 'login' })
        return Promise.reject(error)
      }
    }
    return Promise.reject(error)
  }
)
