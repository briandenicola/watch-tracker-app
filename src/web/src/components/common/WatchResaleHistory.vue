<template>
  <section class="detail-card mb-6">
    <div class="mb-2 flex items-center justify-between">
      <h2 class="detail-heading !mb-0">Resale Value History</h2>
      <button class="text-xs text-accent hover:underline" @click="showManualForm = !showManualForm">{{ showManualForm ? 'Cancel' : '+ Log Value' }}</button>
    </div>
    <p v-if="error" class="mb-2 text-xs text-danger">{{ error }}</p>
    <p v-if="queuedMessage" class="mb-2 text-xs text-success">{{ queuedMessage }}</p>

    <div v-if="showManualForm" class="mb-4 flex flex-wrap items-end gap-2 rounded-lg border border-border bg-bg-surface p-3">
      <div><label class="mb-1 block text-xs text-text-muted">Value</label><input v-model.number="value" type="number" step="0.01" min="0" placeholder="0.00" class="w-28 rounded-lg border border-border bg-bg px-2 py-1.5 text-sm text-text" /></div>
      <div><label class="mb-1 block text-xs text-text-muted">Date</label><input v-model="recordedAt" type="date" class="rounded-lg border border-border bg-bg px-2 py-1.5 text-sm text-text" /></div>
      <div class="min-w-[8rem] flex-1"><label class="mb-1 block text-xs text-text-muted">Notes</label><input v-model="notes" type="text" placeholder="Optional" class="w-full rounded-lg border border-border bg-bg px-2 py-1.5 text-sm text-text" /></div>
      <button :disabled="saving || !value" class="rounded-lg bg-accent px-3 py-1.5 text-sm text-bg transition-colors hover:bg-accent-hover disabled:opacity-50" @click="add">{{ saving ? 'Saving…' : 'Save' }}</button>
    </div>

    <div v-if="history.length === 0" class="text-sm text-text-muted">No resale value recorded yet — log one manually or refresh an estimate.</div>
    <div v-else class="space-y-2">
      <div v-for="entry in history" :key="entry.id" class="flex items-start justify-between gap-3 text-sm">
        <div class="min-w-0">
          <span class="font-medium text-text">${{ entry.value.toFixed(2) }}</span>
          <span class="ml-2 rounded-full border border-border bg-bg-surface px-1.5 py-0.5 text-[10px] uppercase tracking-wide text-text-muted">{{ entry.source === 'Manual' ? 'Manual' : 'Web Estimate' }}</span>
          <span class="ml-2 text-xs text-text-muted">{{ formatInstant(entry.recordedAt, { year: 'numeric', month: 'numeric', day: 'numeric' }) }}</span>
          <p v-if="entry.reasoning" class="mt-0.5 truncate text-xs text-text-muted" :title="entry.reasoning">{{ entry.reasoning }}</p>
        </div>
        <button class="flex-shrink-0 text-xs text-danger hover:underline" @click="emit('remove', entry.id)">Remove</button>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { CreateResaleValueEntry, ResaleValueEntry } from '@/types'
import { formatInstant } from '@/utils/dateTime'

defineProps<{
  history: ResaleValueEntry[]
  error: string
  queuedMessage: string
  saving: boolean
}>()

const emit = defineEmits<{
  add: [entry: CreateResaleValueEntry]
  remove: [entryId: number]
}>()

const showManualForm = ref(false)
const value = ref<number | null>(null)
const recordedAt = ref('')
const notes = ref('')

function add() {
  if (value.value === null) return
  emit('add', { value: value.value, recordedAt: recordedAt.value || undefined, notes: notes.value || undefined })
}

function clearManualForm() {
  value.value = null
  recordedAt.value = ''
  notes.value = ''
  showManualForm.value = false
}

defineExpose({ clearManualForm })
</script>

<style scoped>
.detail-heading {
  margin-bottom: 0.85rem;
  color: var(--color-accent);
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.22em;
  text-transform: uppercase;
}
</style>
