<template>
  <section class="mx-auto max-w-3xl">
    <header class="flex flex-wrap items-center justify-between gap-4 border-b border-border pb-5">
      <div>
        <h2 class="font-display text-2xl font-semibold text-text">Notifications</h2>
        <p class="mt-1 text-sm text-text-muted">Price monitoring updates for your wish list.</p>
      </div>
      <button
        v-if="unreadCount"
        type="button"
        data-testid="mark-all-read"
        class="min-h-11 rounded-lg border border-border px-4 py-2 text-sm font-medium text-accent transition-colors hover:border-accent disabled:cursor-not-allowed disabled:opacity-60"
        :disabled="markingAll"
        @click="markAllRead"
      >
        {{ markingAll ? 'Marking…' : 'Mark all read' }}
      </button>
    </header>

    <div v-if="loading" class="flex items-center justify-center py-20" aria-label="Loading notifications">
      <div class="h-8 w-8 animate-spin rounded-full border-2 border-accent border-t-transparent" />
    </div>

    <div v-else-if="error" class="py-20 text-center">
      <p role="alert" class="text-danger">{{ error }}</p>
      <button type="button" class="mt-3 min-h-11 text-sm text-accent hover:underline" @click="load">Try again</button>
    </div>

    <div v-else-if="alerts.length === 0" class="py-20 text-center">
      <AppIcon name="bell" :size="32" class="mx-auto text-text-muted" aria-hidden="true" />
      <p class="mt-4 text-text-secondary">You have no notifications.</p>
      <p class="mt-1 text-sm text-text-muted">New price alerts will appear here.</p>
    </div>

    <ul v-else class="space-y-3 pt-5" aria-label="Notifications">
      <li
        v-for="alert in alerts"
        :key="alert.id"
        data-testid="notification-card"
        class="rounded-xl border p-4 transition-colors sm:p-5"
        :class="alert.isRead ? 'border-border bg-bg-card opacity-70' : 'border-accent/60 bg-bg-surface'"
      >
        <div class="flex items-start gap-3 sm:gap-4">
          <div
            class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border"
            :class="alert.isRead ? 'border-border text-text-muted' : 'border-accent/60 text-accent'"
          >
            <AppIcon name="bell" :size="20" :stroke-width="1.6" aria-hidden="true" />
          </div>
          <div class="min-w-0 flex-1">
            <div class="flex items-start justify-between gap-3">
              <div>
                <h3 class="font-medium text-text">{{ triggerLabel(alert.trigger) }}</h3>
                <p class="mt-1 text-sm leading-6 text-text-secondary">
                  <RouterLink :to="`/watches/${alert.watchId}`" class="font-medium text-accent hover:underline">
                    {{ alert.watchBrand }} {{ alert.watchModel }}
                  </RouterLink>
                  is available for {{ money(alert.observation.price) }} at {{ alert.observation.source }}.
                </p>
              </div>
              <button
                v-if="!alert.isRead"
                type="button"
                :data-testid="`mark-read-${alert.id}`"
                class="flex min-h-11 min-w-11 shrink-0 items-center justify-center rounded-lg text-text-muted transition-colors hover:bg-bg-elevated hover:text-text disabled:cursor-not-allowed disabled:opacity-60"
                :aria-label="`Mark ${alert.watchBrand} ${alert.watchModel} notification as read`"
                :disabled="readingId === alert.id || markingAll"
                @click="markRead(alert.id)"
              >
                <AppIcon :name="readingId === alert.id ? 'check' : 'close'" :size="19" :stroke-width="1.6" />
              </button>
            </div>
            <div class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-text-muted">
              <time :datetime="alert.createdAt">{{ relativeTime(alert.createdAt) }}</time>
              <a
                :href="alert.observation.listingUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="min-h-11 inline-flex items-center text-accent hover:underline"
              >View listing</a>
              <span v-if="alert.isRead">Read</span>
            </div>
          </div>
        </div>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { PriceAlert, PriceAlertTrigger } from '@/types'
import AppIcon from '@/components/icons/AppIcon.vue'
import { getPriceAlerts, markAllPriceAlertsRead, markPriceAlertRead } from '@/services/watches'
import { useNotificationsStore } from '@/stores/notifications'
import { relativeTime } from '@/utils/dateTime'
import { serverMessage } from '@/utils/serverMessage'

const notifications = useNotificationsStore()
const alerts = ref<PriceAlert[]>([])
const loading = ref(true)
const error = ref('')
const readingId = ref<number | null>(null)
const markingAll = ref(false)
const unreadCount = computed(() => alerts.value.filter(alert => !alert.isRead).length)

onMounted(() => { void load() })

function money(value: number): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}

function triggerLabel(trigger: PriceAlertTrigger): string {
  return trigger === 'BelowTarget' ? 'Price below your target' : 'New best price'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    alerts.value = await getPriceAlerts()
    notifications.setUnreadCount(unreadCount.value)
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not load notifications.'
  } finally {
    loading.value = false
  }
}

async function markRead(alertId: number) {
  readingId.value = alertId
  error.value = ''
  try {
    await markPriceAlertRead(alertId)
    alerts.value = alerts.value.map(alert => alert.id === alertId
      ? { ...alert, isRead: true, readAt: new Date().toISOString() }
      : alert)
    notifications.markOneRead()
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not acknowledge this notification.'
  } finally {
    readingId.value = null
  }
}

async function markAllRead() {
  markingAll.value = true
  error.value = ''
  try {
    await markAllPriceAlertsRead()
    const readAt = new Date().toISOString()
    alerts.value = alerts.value.map(alert => alert.isRead ? alert : { ...alert, isRead: true, readAt })
    notifications.markAllRead()
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not mark notifications as read.'
  } finally {
    markingAll.value = false
  }
}
</script>
