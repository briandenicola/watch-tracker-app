<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-[80] flex items-end sm:items-center justify-center bg-black/60 p-0 sm:p-4" @click.self="emit('cancel')">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="disposition-title"
        class="w-full sm:max-w-lg max-h-[92vh] overflow-y-auto bg-bg-card border border-border rounded-t-2xl sm:rounded-2xl shadow-xl"
        @keydown.esc="emit('cancel')"
      >
        <form @submit.prevent="submit" class="p-5 space-y-4">
          <div class="flex items-center justify-between gap-4">
            <h2 id="disposition-title" class="font-display text-xl font-semibold text-text">
              {{ disposition ? 'Edit disposition' : 'Remove from collection' }}
            </h2>
            <button type="button" class="w-11 h-11 text-text-muted hover:text-text" aria-label="Close" @click="emit('cancel')">Close</button>
          </div>

          <div>
            <label for="disposition-type" class="form-label">Action</label>
            <select id="disposition-type" v-model="form.type" required class="form-control">
              <option value="Retired">Retired</option>
              <option value="Returned">Returned</option>
              <option value="Sold">Sold</option>
              <option value="Traded">Traded</option>
              <option value="Other">Other</option>
            </select>
          </div>

          <div>
            <label for="disposition-date" class="form-label">Date</label>
            <input id="disposition-date" v-model="form.dispositionDate" type="date" required class="form-control" />
          </div>

          <template v-if="form.type === 'Sold'">
            <div>
              <label for="sold-to" class="form-label">Sold to</label>
              <input id="sold-to" v-model="form.soldTo" required maxlength="200" class="form-control" />
            </div>
            <div>
              <label for="sale-price" class="form-label">Sale price</label>
              <input id="sale-price" v-model.number="form.salePrice" type="number" required min="0" max="10000000" step="0.01" class="form-control" />
            </div>
          </template>

          <template v-if="form.type === 'Traded'">
            <div>
              <label for="received-watch" class="form-label">Watch received</label>
              <select id="received-watch" v-model="form.receivedWatchId" class="form-control">
                <option value="">Not tracked in the app</option>
                <option v-for="watch in tradeCandidates" :key="watch.id" :value="watch.id">
                  {{ watch.brand }} {{ watch.model }}
                </option>
              </select>
            </div>
            <div>
              <label for="trade-details" class="form-label">What was received</label>
              <textarea
                id="trade-details"
                v-model="form.tradeDetails"
                rows="3"
                maxlength="2000"
                placeholder="Untracked watch, cash adjustment, accessories, or other details"
                class="form-control"
              />
            </div>
          </template>

          <template v-if="form.type === 'Returned'">
            <div>
              <label for="return-reason" class="form-label">Reason for return</label>
              <textarea id="return-reason" v-model="form.returnReason" rows="3" required maxlength="2000" class="form-control" />
            </div>
            <div>
              <label for="returned-to" class="form-label">Returned to</label>
              <input id="returned-to" v-model="form.returnedTo" maxlength="200" class="form-control" />
            </div>
            <div>
              <label for="refund-amount" class="form-label">Refund amount</label>
              <input id="refund-amount" v-model.number="form.refundAmount" type="number" min="0" max="10000000" step="0.01" class="form-control" />
            </div>
          </template>

          <div v-if="form.type === 'Other'">
            <label for="other-label" class="form-label">Disposition label</label>
            <input id="other-label" v-model="form.otherLabel" required maxlength="100" class="form-control" />
          </div>

          <div>
            <label for="disposition-notes" class="form-label">Notes</label>
            <textarea id="disposition-notes" v-model="form.notes" rows="3" maxlength="2000" class="form-control" />
          </div>

          <p v-if="error || errorMessage" class="text-sm text-danger">{{ error || errorMessage }}</p>

          <div class="flex gap-3 pt-2">
            <button type="button" class="flex-1 min-h-11 px-4 py-2 border border-border rounded-lg text-text-secondary" @click="emit('cancel')">
              Cancel
            </button>
            <button type="submit" :disabled="saving" class="flex-1 min-h-11 px-4 py-2 bg-accent hover:bg-accent-hover text-bg font-medium rounded-lg disabled:opacity-50">
              {{ saving ? 'Saving...' : 'Save' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import type { DispositionType, UpdateWatchDisposition, Watch, WatchDisposition } from '@/types'
import { currentDateKey, dateInputValue } from '@/utils/dateTime'

const props = defineProps<{
  currentWatchId: number
  disposition?: WatchDisposition
  watches: Watch[]
  saving?: boolean
  errorMessage?: string
}>()

const emit = defineEmits<{
  cancel: []
  save: [disposition: UpdateWatchDisposition]
}>()

function dateInput(value?: string): string {
  return value ? dateInputValue(value) : currentDateKey()
}

const form = reactive({
  type: (props.disposition?.type || 'Retired') as DispositionType,
  dispositionDate: dateInput(props.disposition?.dispositionDate),
  notes: props.disposition?.notes || '',
  soldTo: props.disposition?.soldTo || '',
  salePrice: props.disposition?.salePrice,
  receivedWatchId: props.disposition?.receivedWatchId || ('' as number | ''),
  tradeDetails: props.disposition?.tradeDetails || '',
  otherLabel: props.disposition?.otherLabel || '',
  returnReason: props.disposition?.returnReason || '',
  returnedTo: props.disposition?.returnedTo || '',
  refundAmount: props.disposition?.refundAmount,
})

const error = ref('')
const tradeCandidates = computed(() =>
  props.watches.filter(watch => watch.id !== props.currentWatchId && !watch.isWishList),
)

function text(value: string): string | undefined {
  const trimmed = value.trim()
  return trimmed || undefined
}

function submit() {
  error.value = ''
  if (form.type === 'Traded' && !form.receivedWatchId && !text(form.tradeDetails)) {
    error.value = 'Select a received watch or describe what was received.'
    return
  }

  emit('save', {
    type: form.type,
    dispositionDate: form.dispositionDate,
    notes: text(form.notes),
    soldTo: form.type === 'Sold' ? text(form.soldTo) : undefined,
    salePrice: form.type === 'Sold' ? form.salePrice : undefined,
    receivedWatchId: form.type === 'Traded' && form.receivedWatchId
      ? Number(form.receivedWatchId)
      : undefined,
    tradeDetails: form.type === 'Traded' ? text(form.tradeDetails) : undefined,
    otherLabel: form.type === 'Other' ? text(form.otherLabel) : undefined,
    returnReason: form.type === 'Returned' ? text(form.returnReason) : undefined,
    returnedTo: form.type === 'Returned' ? text(form.returnedTo) : undefined,
    refundAmount: form.type === 'Returned' ? form.refundAmount : undefined,
  })
}
</script>

<style scoped>
.form-label {
  display: block;
  margin-bottom: 0.25rem;
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  font-weight: 500;
}

.form-control {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-bg-surface);
  color: var(--color-text);
}

.form-control:focus {
  border-color: var(--color-accent);
  outline: none;
}
</style>
