<template>
  <button
    @click="toggleNotifications"
    class="relative p-2 text-text-muted hover:text-text transition-colors"
    aria-label="Notifications"
  >
    <AppIcon name="bell" :size="size" />
    <span
      v-if="unreadCount > 0"
      class="absolute -top-1 -right-1 min-w-[20px] h-5 px-1.5 flex items-center justify-center bg-danger text-white text-xs font-semibold rounded-full"
    >
      {{ displayCount }}
    </span>
  </button>

  <!-- Notification Dropdown -->
  <Teleport to="body">
    <Transition name="dropdown">
      <div
        v-if="isOpen"
        class="fixed inset-0 z-[70]"
        @click="closeNotifications"
      >
        <div class="absolute inset-0 bg-black/20" />
        <div
          class="absolute right-4 top-16 w-96 max-w-[calc(100vw-2rem)] bg-bg border border-border rounded-lg shadow-xl overflow-hidden"
          @click.stop
        >
          <!-- Header -->
          <div class="flex items-center justify-between p-4 border-b border-border bg-bg-elevated">
            <h3 class="font-semibold text-text">Notifications</h3>
            <button
              v-if="notifications.length > 0"
              @click="handleMarkAllAsRead"
              class="text-xs text-accent hover:text-accent-hover transition-colors"
            >
              Mark all read
            </button>
          </div>

          <!-- Notification List -->
          <div class="max-h-96 overflow-y-auto">
            <div v-if="isLoading" class="p-8 text-center text-text-muted">
              Loading...
            </div>
            <div v-else-if="notifications.length === 0" class="p-8 text-center text-text-muted">
              No notifications
            </div>
            <div v-else>
              <div
                v-for="notification in notifications"
                :key="notification.id"
                class="border-b border-border last:border-b-0 hover:bg-bg-elevated transition-colors"
              >
                <div class="p-4">
                  <div class="flex items-start justify-between gap-2">
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2">
                        <h4 class="font-medium text-text text-sm">{{ notification.title }}</h4>
                        <span
                          v-if="!notification.isRead"
                          class="w-2 h-2 rounded-full bg-accent flex-shrink-0"
                        />
                      </div>
                      <p class="text-sm text-text-muted mt-1">{{ notification.message }}</p>
                      <div class="flex items-center gap-3 mt-2">
                        <span class="text-xs text-text-muted">{{ formatDate(notification.createdAt) }}</span>
                        <button
                          v-if="!notification.isRead"
                          @click="handleMarkAsRead(notification.id)"
                          class="text-xs text-accent hover:text-accent-hover transition-colors"
                        >
                          Mark read
                        </button>
                      </div>
                    </div>
                    <button
                      @click="handleDelete(notification.id)"
                      class="p-1 text-text-muted hover:text-danger transition-colors flex-shrink-0"
                      aria-label="Delete notification"
                    >
                      <AppIcon name="close" :size="16" />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useNotificationsStore } from '@/stores/notifications'
import AppIcon from '@/components/icons/AppIcon.vue'

defineProps<{
  size?: number
}>()

const notificationsStore = useNotificationsStore()
const isOpen = ref(false)

const unreadCount = computed(() => notificationsStore.unreadCount)
const notifications = computed(() => notificationsStore.notifications)
const isLoading = computed(() => notificationsStore.isLoading)

const displayCount = computed(() => {
  return unreadCount.value > 99 ? '99+' : unreadCount.value.toString()
})

function toggleNotifications() {
  isOpen.value = !isOpen.value
  if (isOpen.value && notifications.value.length === 0) {
    notificationsStore.fetchNotifications()
  }
}

function closeNotifications() {
  isOpen.value = false
}

async function handleMarkAsRead(id: number) {
  try {
    await notificationsStore.markAsRead(id)
  } catch (err) {
    console.error('Failed to mark notification as read:', err)
  }
}

async function handleMarkAllAsRead() {
  try {
    await notificationsStore.markAllAsRead()
  } catch (err) {
    console.error('Failed to mark all notifications as read:', err)
  }
}

async function handleDelete(id: number) {
  try {
    await notificationsStore.deleteNotification(id)
  } catch (err) {
    console.error('Failed to delete notification:', err)
  }
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`
  return date.toLocaleDateString()
}

// Start polling when component is mounted
onMounted(() => {
  notificationsStore.startPolling(30000) // Poll every 30 seconds
})

// Stop polling when component is unmounted
onUnmounted(() => {
  notificationsStore.stopPolling()
})
</script>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.2s ease;
}

.dropdown-enter-active > div:last-child,
.dropdown-leave-active > div:last-child {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
}

.dropdown-enter-from > div:last-child,
.dropdown-leave-to > div:last-child {
  transform: translateY(-10px);
  opacity: 0;
}
</style>
