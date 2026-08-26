<template>
  <div>
    <div class="flex items-center justify-between gap-4 mb-6">
      <div>
        <h2 class="font-display text-2xl font-semibold text-text">Wear Log</h2>
        <p class="text-sm text-text-muted mt-1">Track what you wore and when.</p>
      </div>
    </div>

    <div class="bg-bg-card border border-border rounded-2xl p-1 mb-6 grid grid-cols-2 gap-1">
      <button
        v-for="tab in tabs"
        :key="tab.value"
        @click="activeView = tab.value"
        class="px-4 py-3 rounded-xl text-sm font-semibold transition-colors whitespace-nowrap"
        :class="activeView === tab.value ? 'bg-bg-elevated text-text shadow-sm' : 'text-text-muted hover:text-text'"
      >
        {{ tab.label }}
      </button>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else-if="error" class="text-center py-20">
      <p class="text-danger mb-2">Failed to load wear log</p>
      <button @click="load" class="text-accent text-sm hover:underline">Retry</button>
    </div>

    <template v-else>
      <section v-if="activeView === 'calendar'" class="space-y-5">
        <div class="bg-bg-card border border-border rounded-3xl p-4 sm:p-6">
          <div class="flex items-center justify-between mb-6">
            <button @click="moveMonth(-1)" class="calendar-nav-button" aria-label="Previous month">‹</button>
            <h3 class="font-display text-3xl sm:text-4xl text-text text-center">{{ monthTitle }}</h3>
            <button @click="moveMonth(1)" class="calendar-nav-button" aria-label="Next month">›</button>
          </div>

          <div class="grid grid-cols-7 gap-1 sm:gap-3 text-center text-xs sm:text-sm font-semibold text-text-muted mb-2">
            <span v-for="day in weekDays" :key="day" class="py-2">{{ day }}</span>
          </div>

          <div class="grid grid-cols-7 gap-1 sm:gap-3">
            <button
              v-for="day in calendarDays"
              :key="day.key"
              @click="selectedDate = day.dateKey"
              class="calendar-day"
              :class="[
                day.inMonth ? 'text-text-secondary' : 'text-text-muted/40',
                selectedDate === day.dateKey ? 'calendar-day-selected' : 'hover:bg-bg-elevated/60'
              ]"
            >
              <span class="calendar-day-number">{{ day.date.getDate() }}</span>
              <span
                v-if="logsByDate[day.dateKey]?.length"
                class="calendar-wear-dot"
                :aria-label="`${logsByDate[day.dateKey].length} wear logs`"
              />
            </button>
          </div>
        </div>

        <div class="bg-bg-card border border-border rounded-2xl p-4">
          <p class="text-xs uppercase tracking-[0.2em] text-accent mb-3">{{ selectedDateLabel }}</p>
          <div v-if="selectedLogs.length === 0" class="text-sm text-text-muted">No watches logged for this day.</div>
          <div v-else class="space-y-3">
            <WearLogItem
              v-for="log in selectedLogs"
              :key="log.id"
              :log="log"
              :editing-id="editingId ?? undefined"
              @edit="startEdit"
              @cancel="cancelEdit"
              @save="saveEdit"
              @delete="handleDelete"
            />
          </div>

          <div class="mt-4 pt-4 border-t border-border">
            <button
              v-if="!pickerOpen"
              type="button"
              data-testid="add-worn-watch"
              class="flex min-h-11 w-full items-center justify-center gap-2 rounded-xl border border-border text-sm font-semibold text-text-secondary hover:border-accent hover:text-text transition-colors"
              @click="openPicker"
            >
              <span class="text-lg leading-none">+</span>
              Add watch worn
            </button>

            <div v-else class="space-y-3">
              <div class="flex items-center justify-between gap-3">
                <p class="text-sm font-semibold text-text">Which watch?</p>
                <button
                  type="button"
                  class="text-xs text-text-muted hover:text-text transition-colors"
                  @click="closePicker"
                >
                  Cancel
                </button>
              </div>

              <input
                v-model="pickerFilter"
                type="search"
                placeholder="Filter by brand or model"
                class="w-full px-3 py-2.5 bg-bg border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
              />

              <p v-if="pickerLoading" class="text-sm text-text-muted">Loading your collection...</p>
              <p v-else-if="pickerError" class="text-sm text-danger">{{ pickerError }}</p>
              <p v-else-if="pickerWatches.length === 0" class="text-sm text-text-muted">
                No watches in your collection yet.
              </p>
              <p v-else-if="filteredPickerWatches.length === 0" class="text-sm text-text-muted">
                No watches match "{{ pickerFilter }}".
              </p>
              <ul v-else class="max-h-72 overflow-y-auto space-y-2">
                <li v-for="candidate in filteredPickerWatches" :key="candidate.id">
                  <button
                    type="button"
                    :disabled="savingWatchId !== null"
                    class="flex min-h-11 w-full items-center gap-3 rounded-xl border border-border bg-bg-surface p-2 text-left hover:border-accent disabled:opacity-50 transition-colors"
                    @click="logWear(candidate)"
                  >
                    <img
                      v-if="candidate.imageUrls.length > 0"
                      :src="imageUrl(candidate.imageUrls[0].url)"
                      :alt="`${candidate.brand} ${candidate.model}`"
                      class="w-10 h-10 rounded-full bg-bg border border-border object-contain"
                      loading="lazy"
                    />
                    <span
                      v-else
                      class="w-10 h-10 rounded-full bg-bg border border-border flex items-center justify-center text-text-muted"
                    >⌚</span>
                    <span class="flex-1 min-w-0">
                      <span class="block text-sm font-semibold text-text truncate">{{ candidate.brand }}</span>
                      <span class="block text-xs text-text-muted truncate">{{ candidate.model }}</span>
                    </span>
                    <span v-if="savingWatchId === candidate.id" class="text-xs text-text-muted">Saving...</span>
                  </button>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section v-else class="bg-bg-card border border-border rounded-2xl p-4">
        <div v-if="wearLogs.length === 0" class="text-sm text-text-muted">No wear logs yet.</div>
        <div v-else class="relative overflow-hidden">
          <div class="absolute left-[19px] top-2 bottom-2 w-px bg-border" />
          <div class="space-y-5">
            <div v-for="group in groupedLogs" :key="group.date" class="relative">
              <p class="ml-12 text-xs uppercase tracking-[0.2em] text-accent mb-3">{{ group.label }}</p>
              <div class="space-y-3">
                <WearLogItem
                  v-for="log in group.logs"
                  :key="log.id"
                  :log="log"
                  :editing-id="editingId ?? undefined"
                  timeline
                  @edit="startEdit"
                  @cancel="cancelEdit"
                  @save="saveEdit"
                  @delete="handleDelete"
                />
              </div>
            </div>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, defineComponent, h, onMounted, reactive, ref } from 'vue'
import type { Watch, WearLog } from '@/types'
import {
  deleteWearLog,
  getWatches,
  getWearLogs,
  imageUrl,
  recordWear,
  updateWearLogDate,
} from '@/services/watches'
import {
  currentDateKey,
  formatCalendarDate,
  formatInstant,
  instantDateKey,
  instantTimeInput,
  zonedDateTimeToUtc,
} from '@/utils/dateTime'

type ViewMode = 'calendar' | 'timeline'

const tabs: { value: ViewMode; label: string }[] = [
  { value: 'timeline', label: 'Timeline' },
  { value: 'calendar', label: 'Calendar' },
]
const weekDays = ['S', 'M', 'T', 'W', 'T', 'F', 'S']

const activeView = ref<ViewMode>('timeline')
const wearLogs = ref<WearLog[]>([])
const loading = ref(true)
const error = ref(false)
const todayKey = currentDateKey()
const visibleMonth = ref(startOfMonth(fromDateKey(todayKey)))
const selectedDate = ref(todayKey)
const editingId = ref<number | null>(null)
const editingLog = ref<WearLog | null>(null)
const editError = ref('')
const editForm = reactive({ date: '', startTime: '', endTime: '' })

const pickerOpen = ref(false)
const pickerWatches = ref<Watch[]>([])
const pickerLoaded = ref(false)
const pickerLoading = ref(false)
const pickerError = ref('')
const pickerFilter = ref('')
const savingWatchId = ref<number | null>(null)

const WearLogItem = defineComponent({
  props: {
    log: { type: Object as () => WearLog, required: true },
    editingId: { type: Number, required: false, default: null },
    timeline: { type: Boolean, required: false, default: false },
  },
  emits: ['edit', 'cancel', 'save', 'delete'],
  setup(props, { emit }) {
    return () => {
      const isEditing = props.editingId === props.log.id
      return h('div', { class: 'wear-log-row' }, [
        h('div', { class: 'wear-dot' }),
        h('div', { class: 'wear-card' }, [
          h('div', { class: 'flex items-center gap-3' }, [
            props.log.watchImageUrl
              ? h('img', { src: imageUrl(props.log.watchImageUrl), class: 'w-10 h-10 rounded-full bg-bg-surface border border-border object-contain' })
              : h('span', { class: 'w-10 h-10 rounded-full bg-bg-surface border border-border flex items-center justify-center text-text-muted' }, '⌚'),
            h('div', { class: 'flex-1 min-w-0' }, [
              h('p', { class: 'text-sm font-semibold text-text truncate' }, `${props.log.watchBrand} ${props.log.watchModel}`),
              h('p', { class: 'text-xs text-text-muted' }, formatLogTime(props.log)),
            ]),
            h('span', { class: 'text-sm text-text-secondary whitespace-nowrap' }, formatDuration(props.log.durationMinutes)),
          ]),
          isEditing
            ? h('div', { class: 'grid grid-cols-1 sm:grid-cols-3 gap-2 mt-3' }, [
                h('input', { class: 'wear-input', type: 'date', value: editForm.date, onInput: (e: Event) => { editForm.date = (e.target as HTMLInputElement).value } }),
                h('input', { class: 'wear-input', type: 'time', value: editForm.startTime, onInput: (e: Event) => { editForm.startTime = (e.target as HTMLInputElement).value } }),
                h('input', { class: 'wear-input', type: 'time', value: editForm.endTime, onInput: (e: Event) => { editForm.endTime = (e.target as HTMLInputElement).value } }),
                editError.value
                  ? h('p', { class: 'sm:col-span-3 text-xs text-danger' }, editError.value)
                  : null,
                h('div', { class: 'sm:col-span-3 flex gap-2 justify-end' }, [
                  h('button', { class: 'wear-action', onClick: () => emit('cancel') }, 'Cancel'),
                  h('button', { class: 'wear-action-primary', onClick: () => emit('save', props.log.id) }, 'Save'),
                ]),
              ])
            : h('div', { class: 'flex justify-end gap-2 mt-3' }, [
                h('button', { class: 'wear-action', onClick: () => emit('edit', props.log) }, 'Edit'),
                h('button', { class: 'wear-action-danger', onClick: () => emit('delete', props.log.id) }, 'Remove'),
              ]),
        ]),
      ])
    }
  },
})

const monthTitle = computed(() =>
  formatCalendarDate(toDateKey(visibleMonth.value), { month: 'long', year: 'numeric' })
)

const logsByDate = computed(() => {
  const map: Record<string, WearLog[]> = {}
  for (const log of wearLogs.value) {
    const key = instantDateKey(log.wornDate)
    map[key] ??= []
    map[key].push(log)
  }
  return map
})

const calendarDays = computed(() => {
  const first = visibleMonth.value
  const start = new Date(first)
  start.setDate(first.getDate() - first.getDay())
  return Array.from({ length: 42 }, (_, i) => {
    const date = new Date(start)
    date.setDate(start.getDate() + i)
    return {
      key: `${date.toISOString()}-${i}`,
      date,
      dateKey: toDateKey(date),
      inMonth: date.getMonth() === first.getMonth(),
    }
  })
})

const selectedLogs = computed(() =>
  [...(logsByDate.value[selectedDate.value] || [])].sort(compareLogsDesc)
)

const selectedDateLabel = computed(() =>
  formatDayLabel(selectedDate.value)
)

const groupedLogs = computed(() => {
  const groups: Record<string, WearLog[]> = {}
  for (const log of [...wearLogs.value].sort(compareLogsDesc)) {
    const key = instantDateKey(log.wornDate)
    groups[key] ??= []
    groups[key].push(log)
  }
  return Object.entries(groups).map(([date, logs]) => ({ date, label: formatDayLabel(date), logs }))
})

const filteredPickerWatches = computed(() => {
  const needle = pickerFilter.value.trim().toLowerCase()
  if (!needle) return pickerWatches.value
  return pickerWatches.value.filter(candidate =>
    `${candidate.brand} ${candidate.model}`.toLowerCase().includes(needle))
})

async function openPicker() {
  pickerOpen.value = true
  pickerFilter.value = ''
  // The collection is only needed once the picker is actually used, so it is
  // fetched on first open rather than with the page.
  if (pickerLoaded.value || pickerLoading.value) return

  pickerLoading.value = true
  pickerError.value = ''
  try {
    const watches = await getWatches()
    pickerWatches.value = watches
      .filter(candidate => !candidate.isWishList && !candidate.isRetired)
      .sort((a, b) => `${a.brand} ${a.model}`.localeCompare(`${b.brand} ${b.model}`))
    pickerLoaded.value = true
  } catch {
    pickerError.value = 'Could not load your collection.'
  } finally {
    pickerLoading.value = false
  }
}

function closePicker() {
  pickerOpen.value = false
  pickerFilter.value = ''
  pickerError.value = ''
}

async function logWear(candidate: Watch) {
  if (savingWatchId.value !== null) return

  savingWatchId.value = candidate.id
  pickerError.value = ''
  try {
    // Noon on the selected day, matching the fallback the edit form uses when
    // no start time is given.
    const wornDate = zonedDateTimeToUtc(selectedDate.value, '12:00')
    await recordWear(candidate.id, { wornDate })
    closePicker()
    await load()
  } catch (e: any) {
    pickerError.value = e.response?.data?.error || 'Could not log this wear.'
  } finally {
    savingWatchId.value = null
  }
}

function startEdit(log: WearLog) {
  editingId.value = log.id
  editingLog.value = log
  editError.value = ''
  editForm.date = instantDateKey(log.wornDate)
  editForm.startTime = instantTimeInput(log.startedAt || log.wornDate)
  editForm.endTime = instantTimeInput(log.endedAt)
}

function cancelEdit() {
  editingId.value = null
  editingLog.value = null
  editError.value = ''
}

async function saveEdit(logId: number) {
  editError.value = ''
  try {
    const wornDate = zonedDateTimeToUtc(
      editForm.date,
      editForm.startTime || '12:00',
      editingLog.value?.wornDate,
    )
    const startedAt = editForm.startTime
      ? zonedDateTimeToUtc(editForm.date, editForm.startTime, editingLog.value?.startedAt)
      : undefined
    const endedAt = editForm.endTime
      ? zonedDateTimeToUtc(editForm.date, editForm.endTime, editingLog.value?.endedAt)
      : undefined
    await updateWearLogDate(logId, wornDate, startedAt, endedAt)
    editingId.value = null
    editingLog.value = null
    await load()
  } catch (error) {
    editError.value = error instanceof Error
      ? error.message
      : 'Unable to update this wear log.'
  }
}

async function handleDelete(logId: number) {
  if (!confirm('Remove this wear log?')) return
  await deleteWearLog(logId)
  wearLogs.value = wearLogs.value.filter(log => log.id !== logId)
}

function moveMonth(direction: number) {
  const next = new Date(visibleMonth.value)
  next.setMonth(next.getMonth() + direction)
  visibleMonth.value = startOfMonth(next)
}

async function load() {
  loading.value = true
  error.value = false
  try {
    wearLogs.value = await getWearLogs()
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

function compareLogsDesc(a: WearLog, b: WearLog) {
  return new Date(b.startedAt || b.wornDate).getTime() - new Date(a.startedAt || a.wornDate).getTime()
}

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

function toDateKey(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function fromDateKey(dateKey: string) {
  return new Date(`${dateKey}T12:00:00`)
}

function formatDayLabel(dateKey: string) {
  return formatCalendarDate(dateKey, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function formatLogTime(log: WearLog) {
  if (!log.startedAt && !log.endedAt) {
    return formatInstant(log.wornDate, { year: 'numeric', month: 'numeric', day: 'numeric' })
  }
  const start = log.startedAt
    ? formatInstant(log.startedAt, { hour: 'numeric', minute: '2-digit' })
    : ''
  const end = log.endedAt
    ? formatInstant(log.endedAt, { hour: 'numeric', minute: '2-digit' })
    : ''
  return end ? `${start} - ${end}` : start
}

function formatDuration(minutes?: number) {
  if (!minutes) return ''
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m}m`
  if (m === 0) return `${h}h`
  return `${h}h ${m}m`
}

onMounted(load)
</script>

<style scoped>
:deep(.wear-log-row) {
  display: flex;
  gap: 0.75rem;
  min-width: 0;
  position: relative;
  width: 100%;
}

:deep(.wear-dot) {
  background: var(--color-accent);
  border: 3px solid var(--color-bg-card);
  border-radius: 999px;
  flex: 0 0 auto;
  height: 0.9rem;
  margin-left: 0.75rem;
  margin-top: 1.2rem;
  width: 0.9rem;
  z-index: 1;
}

:deep(.wear-card) {
  background: var(--color-bg-surface);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  flex: 1;
  min-width: 0;
  overflow: hidden;
  padding: 0.85rem;
}

:deep(.wear-input) {
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 0.6rem;
  color: var(--color-text);
  font-size: 0.875rem;
  padding: 0.5rem 0.65rem;
  /* Fill the grid track rather than the native control's intrinsic width */
  width: 100%;
}

:deep(.wear-action),
:deep(.wear-action-primary),
:deep(.wear-action-danger) {
  border-radius: 0.55rem;
  font-size: 0.75rem;
  padding: 0.4rem 0.7rem;
  white-space: nowrap;
}

:deep(.wear-action) {
  border: 1px solid var(--color-border);
  color: var(--color-text-secondary);
}

:deep(.wear-action-primary) {
  background: var(--color-accent);
  color: var(--color-bg);
}

:deep(.wear-action-danger) {
  border: 1px solid rgb(239 68 68 / 0.5);
  color: var(--color-danger);
}

.calendar-nav-button {
  color: var(--color-text-muted);
  font-size: 2.5rem;
  line-height: 1;
  padding: 0.25rem 0.5rem;
  transition: color 150ms ease;
}

.calendar-nav-button:hover {
  color: var(--color-text);
}

.calendar-day {
  align-items: center;
  aspect-ratio: 1 / 1;
  border: 1px solid transparent;
  border-radius: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  justify-content: center;
  min-width: 0;
  transition:
    background-color 150ms ease,
    border-color 150ms ease,
    color 150ms ease;
}

.calendar-day-selected {
  background: color-mix(in srgb, var(--color-accent) 10%, transparent);
  border-color: color-mix(in srgb, var(--color-accent) 45%, var(--color-border));
  color: var(--color-text);
}

.calendar-day-number {
  font-size: 0.95rem;
  font-weight: 650;
  line-height: 1;
}

.calendar-wear-dot {
  background: var(--color-accent);
  border-radius: 999px;
  height: 0.42rem;
  width: 0.42rem;
}

@media (min-width: 640px) {
  .calendar-day-number {
    font-size: 1.1rem;
  }

  .calendar-wear-dot {
    height: 0.5rem;
    width: 0.5rem;
  }
}
</style>
