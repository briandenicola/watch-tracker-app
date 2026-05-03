import { api } from './api'
import type { Notification, CreateNotification } from '@/types'

export const notificationsApi = {
  getAll: () => api.get<Notification[]>('/api/notifications'),

  getUnreadCount: () => api.get<number>('/api/notifications/unread-count'),

  getById: (id: number) => api.get<Notification>(`/api/notifications/${id}`),

  create: (notification: CreateNotification) =>
    api.post<Notification>('/api/notifications', notification),

  markAsRead: (id: number) =>
    api.put(`/api/notifications/${id}/read`),

  markAllAsRead: () =>
    api.put('/api/notifications/read-all'),

  delete: (id: number) =>
    api.delete(`/api/notifications/${id}`),
}
