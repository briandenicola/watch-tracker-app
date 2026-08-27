<template>
  <section class="alerts-card" aria-labelledby="price-alerts-heading">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h2 id="price-alerts-heading" class="font-display text-lg font-medium text-text">
          Price alerts
          <span
            v-if="unreadCount"
            data-testid="unread-alert-badge"
            class="ml-1 inline-flex min-w-6 items-center justify-center rounded-full bg-accent px-2 py-0.5 text-xs text-bg"
            :aria-label="`${unreadCount} unread price alerts`"
          >{{ unreadCount }}</span>
        </h2>
        <p class="mt-1 text-sm text-text-muted">Alerts are in-app signals from high-confidence listing matches.</p>
      </div>
      <button type="button" class="min-h-11 text-sm text-accent hover:underline" @click="load">Refresh</button>
    </div>
    <p v-if="loading" class="mt-3 text-sm text-text-muted">Loading price alerts…</p>
    <p v-else-if="error" role="alert" class="mt-3 text-sm text-danger">{{ error }}</p>
    <p v-else-if="alerts.length === 0" class="mt-3 text-sm text-text-muted">No price alerts yet.</p>
    <ul v-else class="mt-3 divide-y divide-border">
      <li v-for="alert in alerts" :key="alert.id" class="flex flex-wrap items-start justify-between gap-3 py-3">
        <div class="min-w-0">
          <RouterLink :to="`/watches/${alert.watchId}`" class="font-medium text-accent hover:underline">
            {{ alert.watchBrand }} {{ alert.watchModel }}
          </RouterLink>
          <p class="text-sm text-text">{{ triggerLabel(alert.trigger) }}: {{ money(alert.observation.price) }} at {{ alert.observation.source }}</p>
          <a :href="alert.observation.listingUrl" target="_blank" rel="noopener noreferrer" class="text-xs text-text-muted hover:text-accent hover:underline">
            View attributed listing
          </a>
        </div>
        <button
          v-if="!alert.isRead"
          type="button"
          class="min-h-11 rounded-lg border border-border bg-bg-surface px-3 py-2 text-sm text-text hover:border-accent"
          :disabled="readingId === alert.id"
          @click="markRead(alert.id)"
        >
          {{ readingId === alert.id ? 'Marking…' : 'Mark as read' }}
        </button>
        <span v-else class="pt-2 text-xs text-text-muted">Read</span>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { PriceAlert, PriceAlertTrigger } from '@/types'
import { getPriceAlerts, markPriceAlertRead } from '@/services/watches'
import { serverMessage } from '@/utils/serverMessage'

const alerts = ref<PriceAlert[]>([])
const loading = ref(true)
const error = ref('')
const readingId = ref<number | null>(null)
const unreadCount = computed(() => alerts.value.filter(alert => !alert.isRead).length)

onMounted(() => { void load() })

function money(value: number): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}

function triggerLabel(trigger: PriceAlertTrigger): string {
  return trigger === 'BelowTarget' ? 'Below your target' : 'New best price'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    alerts.value = await getPriceAlerts()
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not load price alerts.'
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
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not acknowledge this price alert.'
  } finally {
    readingId.value = null
  }
}
</script>

<style scoped>
.alerts-card { margin-bottom: 1rem; border: 1px solid var(--color-border); border-radius: 0.75rem; background: var(--color-bg-card); padding: 1rem; }
</style>
