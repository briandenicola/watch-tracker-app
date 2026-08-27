<template>
  <section class="price-card">
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div>
        <h2 class="detail-heading !mb-1">Price Watch</h2>
        <p class="text-sm text-text-muted">
          Best-effort signals from attributed search listings and eBay, not a complete market sweep.
        </p>
      </div>
      <button
        type="button"
        class="min-h-11 rounded-lg bg-accent px-4 py-2 text-sm font-medium text-bg transition-colors hover:bg-accent-hover disabled:opacity-50"
        :disabled="scanning"
        @click="scan"
      >
        {{ scanning ? 'Checking prices…' : 'Check prices now' }}
      </button>
    </div>

    <form class="mt-5 grid gap-4 sm:grid-cols-[1fr,minmax(11rem,0.6fr),auto] sm:items-end" @submit.prevent="save">
      <label class="flex min-h-11 items-center gap-2 text-sm text-text">
        <input v-model="enabled" type="checkbox" class="h-4 w-4 accent-accent" />
        Watch this item on the schedule
      </label>
      <label class="text-sm text-text-secondary">
        Target price (USD, optional)
        <input
          v-model="targetDraft"
          type="number"
          inputmode="decimal"
          min="0.01"
          max="10000000"
          step="0.01"
          placeholder="No target"
          class="mt-1 block min-h-11 w-full rounded-lg border border-border bg-bg-surface px-3 text-text focus:border-accent focus:outline-none"
        />
      </label>
      <button
        type="submit"
        :disabled="saving"
        class="min-h-11 rounded-lg border border-border bg-bg-surface px-4 py-2 text-sm font-medium text-text transition-colors hover:border-accent disabled:opacity-50"
      >
        {{ saving ? 'Saving…' : 'Save watch' }}
      </button>
    </form>
    <p class="mt-2 text-xs text-text-muted">
      Scheduled checks run only when this option is on. {{ checkedDescription }}
    </p>
    <p v-if="error" role="alert" class="mt-3 text-sm text-danger">{{ error }}</p>
    <p v-if="success" role="status" class="mt-3 text-sm text-success">{{ success }}</p>

    <div v-if="scanResult" class="mt-5 border-t border-border pt-4">
      <h3 class="text-sm font-medium text-text">Latest check</h3>
      <ul class="mt-2 space-y-2" aria-label="Price scan source status">
        <li v-for="source in scanResult.sources" :key="source.source" class="rounded-lg bg-bg-surface p-3 text-sm">
          <p data-testid="scan-source-status" class="font-medium text-text">{{ source.source }}: {{ source.status }}</p>
          <p v-if="source.error" class="mt-1 text-text-muted">{{ source.error }}</p>
          <ul v-if="source.listings.length" class="mt-2 space-y-1">
            <li v-for="listing in source.listings" :key="listing.id || listing.listingUrl">
              <a :href="listing.listingUrl" target="_blank" rel="noopener noreferrer" class="text-accent hover:underline">
                {{ listing.listingTitle }} — {{ money(listing.price, listing.currency) }}
              </a>
              <span class="text-text-muted"> · {{ listing.kind }} · {{ listing.matchConfidence }} confidence</span>
            </li>
          </ul>
        </li>
      </ul>
    </div>

    <div class="mt-5 border-t border-border pt-4">
      <h3 class="text-sm font-medium text-text">Price history</h3>
      <p v-if="loadingHistory" class="mt-2 text-sm text-text-muted">Loading history…</p>
      <p v-else-if="history.length === 0" class="mt-2 text-sm text-text-muted">
        No attributable USD listings have been saved yet.
      </p>
      <ul v-else class="mt-2 divide-y divide-border">
        <li v-for="observation in history" :key="observation.id" class="py-3 text-sm">
          <a :href="observation.listingUrl" target="_blank" rel="noopener noreferrer" class="font-medium text-accent hover:underline">
            {{ money(observation.price, observation.currency) }} · {{ observation.source }}
          </a>
          <p class="mt-0.5 text-text-secondary">{{ observation.listingTitle }}</p>
          <p class="mt-0.5 text-xs text-text-muted">
            {{ observation.kind }} · {{ observation.condition || 'Condition not stated' }} ·
            {{ observation.matchConfidence }} confidence · {{ observedDate(observation.observedAt) }}
          </p>
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch as watchProp } from 'vue'
import type { PriceMonitoring, PriceObservation, PriceScanResult, Watch } from '@/types'
import { getPriceObservations, scanWishlistPrice, updatePriceMonitoring } from '@/services/watches'
import { formatInstant } from '@/utils/dateTime'
import { serverMessage } from '@/utils/serverMessage'

const props = defineProps<{ watch: Watch }>()
const emit = defineEmits<{ updated: [monitoring: PriceMonitoring] }>()

const enabled = ref(props.watch.priceAlertEnabled)
const targetDraft = ref(props.watch.priceAlertTarget?.toString() ?? '')
const saving = ref(false)
const scanning = ref(false)
const loadingHistory = ref(true)
const error = ref('')
const success = ref('')
const history = ref<PriceObservation[]>([])
const scanResult = ref<PriceScanResult | null>(null)

const checkedDescription = computed(() => props.watch.priceCheckedAt
  ? `Last checked ${observedDate(props.watch.priceCheckedAt)}.`
  : 'Not checked yet.')

watchProp(
  () => [props.watch.priceAlertEnabled, props.watch.priceAlertTarget] as const,
  ([nextEnabled, nextTarget]) => {
    enabled.value = nextEnabled
    targetDraft.value = nextTarget?.toString() ?? ''
  },
)

onMounted(() => { void loadHistory() })

function money(value: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(value)
}

function observedDate(value: string): string {
  return formatInstant(value, { dateStyle: 'medium', timeStyle: 'short' }) || 'at an unknown time'
}

async function loadHistory() {
  loadingHistory.value = true
  try {
    history.value = await getPriceObservations(props.watch.id)
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not load price history.'
  } finally {
    loadingHistory.value = false
  }
}

async function save() {
  const targetText = String(targetDraft.value).trim()
  const target = targetText === '' ? null : Number(targetText)
  if (target !== null && (!Number.isFinite(target) || target < 0.01 || target > 10_000_000)) {
    error.value = 'Enter a target from $0.01 to $10,000,000, or leave it blank.'
    return
  }

  saving.value = true
  error.value = ''
  success.value = ''
  try {
    const monitoring = await updatePriceMonitoring(props.watch.id, {
      priceAlertEnabled: enabled.value,
      priceAlertTarget: target,
    })
    emit('updated', monitoring)
    success.value = enabled.value ? 'Scheduled price watch saved.' : 'Scheduled price watch turned off.'
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not save price watch settings.'
  } finally {
    saving.value = false
  }
}

async function scan() {
  scanning.value = true
  error.value = ''
  success.value = ''
  try {
    scanResult.value = await scanWishlistPrice(props.watch.id)
    emit('updated', {
      priceAlertEnabled: props.watch.priceAlertEnabled,
      priceAlertTarget: props.watch.priceAlertTarget,
      priceCheckedAt: scanResult.value.checkedAt,
    })
    await loadHistory()
    success.value = scanResult.value.alertsCreated > 0
      ? `${scanResult.value.alertsCreated} new price alert${scanResult.value.alertsCreated === 1 ? '' : 's'} created.`
      : `${scanResult.value.observationsAdded} new attributable listing${scanResult.value.observationsAdded === 1 ? '' : 's'} saved.`
  } catch (requestError) {
    error.value = serverMessage(requestError) || 'Could not check prices.'
  } finally {
    scanning.value = false
  }
}
</script>

<style scoped>
.price-card { margin-bottom: 1.5rem; border: 1px solid var(--color-border); border-radius: 1rem; background: var(--color-bg-card); padding: 1.25rem; box-shadow: inset 0 1px 0 rgb(255 255 255 / 0.03); }
.detail-heading { margin-bottom: 0.85rem; color: var(--color-accent); font-size: 0.8rem; font-weight: 700; letter-spacing: 0.22em; text-transform: uppercase; }
@media (min-width: 768px) { .price-card { padding: 1.5rem 1.75rem; } }
</style>
