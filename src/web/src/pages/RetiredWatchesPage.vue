<template>
  <div>
    <PullToRefresh :pulling="pulling" :refreshing="refreshing" :pull-distance="pullDistance" />
    <div class="mb-6">
      <h2 class="font-display text-2xl font-semibold text-text">Former Watches</h2>
      <p class="text-sm text-text-muted mt-1">Retired, returned, sold, traded, and otherwise removed watches remain in your ownership history.</p>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else-if="error" class="text-center py-20">
      <p class="text-danger mb-2">{{ error }}</p>
      <button @click="load" class="text-accent text-sm hover:underline">Retry</button>
    </div>

    <div v-else-if="formerWatches.length === 0" class="text-center py-20">
      <p class="text-text-secondary">No former watches</p>
      <p class="text-sm text-text-muted mt-1">All your watches are still in the active collection.</p>
    </div>

    <div v-else class="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-6">
      <article
        v-for="watch in formerWatches"
        :key="watch.id"
        class="bg-bg-card border border-border rounded-xl overflow-hidden"
      >
        <RouterLink :to="`/watches/${watch.id}`" class="block aspect-square bg-bg-surface overflow-hidden">
          <img
            v-if="watch.imageUrls.length > 0"
            :src="imageUrl(watch.imageUrls[0].url)"
            :alt="`${watch.brand} ${watch.model}`"
            class="w-full h-full object-contain p-4"
            loading="lazy"
          />
          <div v-else class="w-full h-full flex items-center justify-center text-text-muted">No image</div>
        </RouterLink>

        <div class="p-4 space-y-3">
          <div>
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <p class="font-display text-lg font-medium text-text truncate">{{ watch.brand }}</p>
                <p class="text-sm text-text-secondary truncate">{{ watch.model }}</p>
              </div>
              <span class="px-2 py-1 bg-bg-surface border border-border rounded-full text-xs text-accent flex-shrink-0">
                {{ dispositionLabel(watch) }}
              </span>
            </div>
            <p class="text-xs text-text-muted mt-1">
              {{ formatDate(watch.disposition?.dispositionDate) }} · {{ watch.timesWorn }} wear{{ watch.timesWorn === 1 ? '' : 's' }}
            </p>
          </div>

          <dl v-if="watch.disposition" class="space-y-1 text-sm">
            <div v-if="watch.disposition.soldTo" class="flex gap-2">
              <dt class="text-text-muted">Sold to</dt>
              <dd class="text-text ml-auto text-right">{{ watch.disposition.soldTo }}</dd>
            </div>
            <div v-if="watch.disposition.salePrice !== undefined" class="flex gap-2">
              <dt class="text-text-muted">Sale price</dt>
              <dd class="text-text ml-auto">{{ formatCurrency(watch.disposition.salePrice) }}</dd>
            </div>
            <div v-if="watch.disposition.receivedWatchName || watch.disposition.tradeDetails" class="flex gap-2">
              <dt class="text-text-muted">Received</dt>
              <dd class="text-text ml-auto text-right">{{ watch.disposition.receivedWatchName || watch.disposition.tradeDetails }}</dd>
            </div>
            <div v-if="watch.disposition.returnReason" class="block">
              <dt class="text-text-muted">Return reason</dt>
              <dd class="text-text mt-0.5">{{ watch.disposition.returnReason }}</dd>
            </div>
            <div v-if="watch.disposition.returnedTo" class="flex gap-2">
              <dt class="text-text-muted">Returned to</dt>
              <dd class="text-text ml-auto text-right">{{ watch.disposition.returnedTo }}</dd>
            </div>
            <div v-if="watch.disposition.refundAmount !== undefined" class="flex gap-2">
              <dt class="text-text-muted">Refund</dt>
              <dd class="text-text ml-auto">{{ formatCurrency(watch.disposition.refundAmount) }}</dd>
            </div>
            <div v-if="watch.disposition.notes" class="block">
              <dt class="text-text-muted">Notes</dt>
              <dd class="text-text mt-0.5">{{ watch.disposition.notes }}</dd>
            </div>
          </dl>

          <div class="grid grid-cols-2 gap-2 pt-1">
            <button
              @click="editDisposition(watch)"
              class="min-h-11 px-3 py-2 border border-border text-text-secondary text-sm rounded-lg hover:border-accent/50"
            >
              Edit details
            </button>
            <button
              @click="restore(watch)"
              :disabled="restoring === watch.id"
              class="min-h-11 px-3 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg disabled:opacity-50"
            >
              {{ restoring === watch.id ? 'Restoring...' : 'Restore' }}
            </button>
          </div>
        </div>
      </article>
    </div>

    <DispositionModal
      v-if="editingWatch"
      :current-watch-id="editingWatch.id"
      :disposition="editingWatch.disposition"
      :watches="allWatches"
      :saving="saving"
      :error-message="modalError"
      @cancel="editingWatch = null"
      @save="saveDisposition"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { UpdateWatchDisposition, Watch } from '@/types'
import {
  clearWatchDisposition,
  getWatches,
  imageUrl,
  setWatchDisposition,
} from '@/services/watches'
import { usePullToRefresh } from '@/composables/usePullToRefresh'
import DispositionModal from '@/components/common/DispositionModal.vue'
import PullToRefresh from '@/components/common/PullToRefresh.vue'
import { formatCalendarDate } from '@/utils/dateTime'

const { refreshing, pullDistance, pulling } = usePullToRefresh(load)

const allWatches = ref<Watch[]>([])
const formerWatches = computed(() => allWatches.value.filter(watch => Boolean(watch.disposition)))
const loading = ref(true)
const error = ref('')
const restoring = ref<number | null>(null)
const editingWatch = ref<Watch | null>(null)
const saving = ref(false)
const modalError = ref('')

function dispositionLabel(watch: Watch): string {
  if (!watch.disposition) return 'Active'
  return watch.disposition.type === 'Other'
    ? (watch.disposition.otherLabel || 'Other')
    : watch.disposition.type
}

function formatDate(value?: string): string {
  return value
    ? formatCalendarDate(value.slice(0, 10), { year: 'numeric', month: 'numeric', day: 'numeric' })
    : 'Date unknown'
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    allWatches.value = await getWatches(true)
  } catch {
    error.value = 'Failed to load former watches'
  } finally {
    loading.value = false
  }
}

function editDisposition(watch: Watch) {
  modalError.value = ''
  editingWatch.value = watch
}

async function saveDisposition(disposition: UpdateWatchDisposition) {
  if (!editingWatch.value) return
  saving.value = true
  modalError.value = ''
  try {
    const updated = await setWatchDisposition(editingWatch.value.id, disposition)
    allWatches.value = allWatches.value.map(watch => watch.id === updated.id ? updated : watch)
    editingWatch.value = null
  } catch (requestError: any) {
    modalError.value = requestError?.response?.data?.error || 'Could not save the disposition.'
  } finally {
    saving.value = false
  }
}

async function restore(watch: Watch) {
  if (!confirm('Restore this watch to your active collection?')) return
  restoring.value = watch.id
  try {
    const updated = await clearWatchDisposition(watch.id)
    allWatches.value = allWatches.value.map(item => item.id === updated.id ? updated : item)
  } finally {
    restoring.value = null
  }
}

onMounted(load)
</script>
