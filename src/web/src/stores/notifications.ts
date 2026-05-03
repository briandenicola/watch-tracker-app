import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Notification } from '@/types'
import { notificationsApi } from '@/services/notifications'

export const useNotificationsStore = defineStore('notifications', () => {
  const notifications = ref<Notification[]>([])
  const unreadCount = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  let pollingInterval: number | null = null

  const hasUnread = computed(() => unreadCount.value > 0)

  async function fetchNotifications() {
    try {
      isLoading.value = true
      error.value = null
      const { data } = await notificationsApi.getAll()
      notifications.value = data
      unreadCount.value = data.filter(n => !n.isRead).length
    } catch (err) {
      error.value = 'Failed to fetch notifications'
      console.error('Error fetching notifications:', err)
    } finally {
      isLoading.value = false
    }
  }

  async function fetchUnreadCount() {
    try {
      const { data } = await notificationsApi.getUnreadCount()
      unreadCount.value = data
    } catch (err) {
      console.error('Error fetching unread count:', err)
    }
  }

  async function markAsRead(id: number) {
    try {
      await notificationsApi.markAsRead(id)
      const notification = notifications.value.find(n => n.id === id)
      if (notification) {
        notification.isRead = true
        unreadCount.value = Math.max(0, unreadCount.value - 1)
      }
    } catch (err) {
      console.error('Error marking notification as read:', err)
      throw err
    }
  }

  async function markAllAsRead() {
    try {
      await notificationsApi.markAllAsRead()
      notifications.value.forEach(n => n.isRead = true)
      unreadCount.value = 0
    } catch (err) {
      console.error('Error marking all notifications as read:', err)
      throw err
    }
  }

  async function deleteNotification(id: number) {
    try {
      await notificationsApi.delete(id)
      const notification = notifications.value.find(n => n.id === id)
      if (notification && !notification.isRead) {
        unreadCount.value = Math.max(0, unreadCount.value - 1)
      }
      notifications.value = notifications.value.filter(n => n.id !== id)
    } catch (err) {
      console.error('Error deleting notification:', err)
      throw err
    }
  }

  function startPolling(intervalMs = 30000) {
    // Initial fetch
    fetchUnreadCount()

    // Stop any existing polling
    stopPolling()

    // Start new polling interval
    pollingInterval = window.setInterval(() => {
      fetchUnreadCount()
    }, intervalMs)
  }

  function stopPolling() {
    if (pollingInterval !== null) {
      clearInterval(pollingInterval)
      pollingInterval = null
    }
  }

  return {
    notifications,
    unreadCount,
    hasUnread,
    isLoading,
    error,
    fetchNotifications,
    fetchUnreadCount,
    markAsRead,
    markAllAsRead,
    deleteNotification,
    startPolling,
    stopPolling,
  }
})
