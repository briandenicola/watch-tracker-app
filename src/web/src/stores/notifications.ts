import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getPriceAlerts } from '@/services/watches'

export const useNotificationsStore = defineStore('notifications', () => {
  const unreadCount = ref(0)

  async function refreshUnreadCount() {
    try {
      unreadCount.value = (await getPriceAlerts(true)).length
    } catch {
      // Notifications are supplementary; a failed badge request must not disrupt the shell.
    }
  }

  function setUnreadCount(count: number) {
    unreadCount.value = Math.max(0, count)
  }

  function markOneRead() {
    unreadCount.value = Math.max(0, unreadCount.value - 1)
  }

  function markAllRead() {
    unreadCount.value = 0
  }

  return { unreadCount, refreshUnreadCount, setUnreadCount, markOneRead, markAllRead }
})
