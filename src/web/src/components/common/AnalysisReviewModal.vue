<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-[80] flex items-end sm:items-center justify-center bg-black/60 p-0 sm:p-4"
      @click.self="emit('close')"
    >
      <div
        ref="dialogEl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="analysis-title"
        tabindex="-1"
        class="w-full sm:max-w-lg max-h-[92vh] overflow-y-auto bg-bg-card border border-border rounded-t-2xl sm:rounded-2xl shadow-xl focus:outline-none"
        @keydown.esc="emit('close')"
      >
        <div class="p-5 space-y-4">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <p class="text-[0.65rem] uppercase tracking-[0.24em] text-accent mb-1">AI Analysis</p>
              <h2 id="analysis-title" class="font-display text-xl font-semibold text-text truncate">
                {{ watchName }}
              </h2>
            </div>
            <button
              type="button"
              class="w-11 h-11 inline-flex items-center justify-center text-text-muted hover:text-text flex-shrink-0"
              aria-label="Close"
              @click="emit('close')"
            >
              <AppIcon name="close" :size="20" :stroke-width="2" />
            </button>
          </div>

          <div class="prose-markdown text-sm text-text" v-html="renderMarkdown(result.summary)" />
          <p class="text-xs text-text-muted">Saved to this watch's AI analysis.</p>

          <p v-if="result.sources.length" class="text-xs text-text-muted">
            Read alongside the photo:
            <template v-for="(source, index) in result.sources" :key="source.url">
              <a :href="source.url" target="_blank" rel="noopener noreferrer" class="text-accent hover:underline">{{ source.label }}</a><span v-if="index < result.sources.length - 1">, </span>
            </template>
          </p>

          <!-- Suggestions -->
          <template v-if="rows.length">
            <div class="flex items-center justify-between gap-3 pt-1">
              <h3 class="text-sm font-medium text-text">Fill in missing details</h3>
              <button type="button" class="text-xs text-accent hover:underline" @click="toggleAll">
                {{ allSelected ? 'Clear all' : 'Select all' }}
              </button>
            </div>
            <p class="text-xs text-text-muted -mt-2">
              Nothing is written until you apply. Edit anything that looks off.
            </p>

            <div class="space-y-2">
              <div
                v-for="row in rows"
                :key="row.field"
                class="rounded-lg border p-3 transition-colors"
                :class="row.selected ? 'border-accent/60 bg-bg-surface' : 'border-border bg-bg-surface'"
              >
                <label class="flex items-start gap-2.5 cursor-pointer">
                  <input v-model="row.selected" type="checkbox" class="mt-1 accent-accent" />
                  <span class="min-w-0 flex-1">
                    <span class="flex flex-wrap items-center gap-2">
                      <span class="text-sm text-text">{{ row.label }}</span>
                      <span class="chip" :class="`chip-${row.confidence}`">{{ row.confidence }}</span>
                    </span>
                    <span v-if="row.reason" class="block text-xs text-text-muted mt-0.5">{{ row.reason }}</span>
                  </span>
                </label>
                <input
                  v-model="row.value"
                  :type="row.kind === 'text' ? 'text' : 'number'"
                  :step="row.kind === 'integer' ? '1' : 'any'"
                  :maxlength="row.kind === 'text' ? 100 : undefined"
                  class="form-control mt-2 text-sm"
                  :aria-label="row.label"
                  @focus="row.selected = true"
                />
              </div>
            </div>

            <p v-if="error" class="text-sm text-danger">{{ error }}</p>
            <p v-if="rejected.length" class="text-xs text-danger">
              Not saved — {{ rejected.join('; ') }}.
            </p>

            <div class="flex items-center gap-2 pt-1">
              <button type="button" class="btn-accent flex-1" :disabled="saving || !selectedCount" @click="apply">
                {{ saving ? 'Saving…' : selectedCount ? `Apply ${selectedCount} value${selectedCount === 1 ? '' : 's'}` : 'Apply' }}
              </button>
              <button type="button" class="btn-quiet" :disabled="saving" @click="emit('close')">Not now</button>
            </div>
          </template>

          <template v-else>
            <p class="text-sm text-text-secondary">
              {{ appliedNote || 'No missing details it could fill in — this record already has everything the analysis covers.' }}
            </p>
            <button type="button" class="btn-accent w-full" @click="emit('close')">Done</button>
          </template>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { marked } from 'marked'
import type { Watch, WatchAnalysisResult } from '@/types'
import AppIcon from '@/components/icons/AppIcon.vue'
import { applyAnalysisSuggestions } from '@/services/watches'

const props = defineProps<{ watchId: number, watchName: string, result: WatchAnalysisResult }>()
const emit = defineEmits<{ close: [], applied: [watch: Watch] }>()

interface Row {
  field: string
  label: string
  kind: 'text' | 'number' | 'integer'
  value: string
  confidence: string
  reason?: string | null
  selected: boolean
}

// Low-confidence guesses start unticked: approving should be a decision, not a reflex.
const rows = ref<Row[]>(props.result.suggestions.map(suggestion => ({
  field: suggestion.field,
  label: suggestion.label,
  kind: suggestion.kind,
  value: suggestion.value,
  confidence: suggestion.confidence,
  reason: suggestion.reason,
  selected: suggestion.confidence !== 'low',
})))

const saving = ref(false)
const error = ref('')
const rejected = ref<string[]>([])
const appliedNote = ref('')
const dialogEl = ref<HTMLElement | null>(null)

const selectedCount = computed(() => rows.value.filter(row => row.selected && row.value.trim()).length)
const allSelected = computed(() => rows.value.length > 0 && rows.value.every(row => row.selected))

function renderMarkdown(text: string): string {
  return marked.parse(text, { async: false }) as string
}

function toggleAll() {
  const next = !allSelected.value
  rows.value.forEach(row => { row.selected = next })
}

onMounted(() => dialogEl.value?.focus())

async function apply() {
  const values: Record<string, string> = {}
  for (const row of rows.value) {
    if (row.selected && row.value.trim()) values[row.field] = row.value.trim()
  }
  if (Object.keys(values).length === 0) return

  saving.value = true
  error.value = ''
  rejected.value = []
  try {
    const result = await applyAnalysisSuggestions(props.watchId, values)
    emit('applied', result.watch)
    rejected.value = result.rejected

    // Saved rows drop off the list; anything the server refused stays on screen
    // with its reason, so it can be corrected and applied again.
    rows.value = rows.value.filter(row => !result.applied.includes(row.label))
    appliedNote.value = result.applied.length ? `Saved ${result.applied.join(', ')}.` : ''
  } catch {
    error.value = 'Could not save those values.'
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.form-control {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-bg);
  color: var(--color-text);
}

.form-control:focus {
  border-color: var(--color-accent);
  outline: none;
}

.chip {
  padding: 0.1rem 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 9999px;
  font-size: 0.65rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--color-text-muted);
}

.chip-high {
  border-color: color-mix(in srgb, var(--color-success) 50%, transparent);
  color: var(--color-success);
}

.chip-low {
  border-color: color-mix(in srgb, var(--color-danger) 45%, transparent);
  color: var(--color-danger);
}

.btn-accent {
  padding: 0.6rem 1rem;
  border-radius: 0.5rem;
  background: var(--color-accent);
  color: var(--color-bg);
  font-size: 0.875rem;
  font-weight: 500;
  transition: background-color 0.15s ease;
}

.btn-accent:hover:not(:disabled) {
  background: var(--color-accent-hover);
}

.btn-accent:disabled {
  opacity: 0.5;
}

.btn-quiet {
  padding: 0.6rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-bg-surface);
  color: var(--color-text-secondary);
  font-size: 0.875rem;
}

.btn-quiet:hover:not(:disabled) {
  color: var(--color-text);
}
</style>
