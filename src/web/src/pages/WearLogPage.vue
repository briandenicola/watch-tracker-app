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
        <div class="flex items-center justify-between">
          <button @click="moveMonth(-1)" class="p-2 text-text-muted hover:text-text transition-colors" aria-label="Previous month">‹</button>
          <h3 class="font-display text-3xl text-text">{{ monthTitle }}</h3>
          <button @click="moveMonth(1)" class="p-2 text-text-muted hover:text-text transition-colors" aria-label="Next month">›</button>
        </div>

        <div class="grid grid-cols-7 gap-2 text-center text-xs font-semibold text-text-muted">
          <span v-for="day in weekDays" :key="day">{{ day }}</span>
        </div>

        <div class="grid grid-cols-7 gap-2">
          <button
            v-for="day in calendarDays"
            :key="day.key"
            @click="selectedDate = day.dateKey"
            class="min-h-20 rounded-2xl border p-2 text-left transition-colors"
            :class="[
              day.inMonth ? 'bg-bg-card' : 'bg-bg-surface/40 opacity-50',
              selectedDate === day.dateKey ? 'border-accent bg-accent/10' : 'border-transparent hover:border-border'
            ]"
          >
            <span class="block text-sm text-text-secondary">{{ day.date.getDate() }}</span>
            <div class="mt-2 flex flex-wrap gap-1">
              <img
                v-for="log in logsByDate[day.dateKey]?.slice(0, 3)"
                :key="log.id"
                :src="log.watchImageUrl ? imageUrl(log.watchImageUrl) : ''"
                :alt="`${log.watchBrand} ${log.watchModel}`"
                class="w-6 h-6 rounded-full bg-bg-surface border border-border object-contain"
              />
              <span v-if="logsByDate[day.dateKey]?.length > 3" class="text-[10px] text-text-muted">+{{ logsByDate[day.dateKey].length - 3 }}</span>
            </div>
          </button>
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
import type { WearLog } from '@/types'
import { deleteWearLog, getWearLogs, imageUrl, updateWearLogDate } from '@/services/watches'

type ViewMode = 'calendar' | 'timeline'

const tabs: { value: ViewMode; label: string }[] = [
  { value: 'calendar', label: 'Calendar' },
  { value: 'timeline', label: 'Timeline' },
]
const weekDays = ['S', 'M', 'T', 'W', 'T', 'F', 'S']

const activeView = ref<ViewMode>('calendar')
const wearLogs = ref<WearLog[]>([])
const loading = ref(true)
const error = ref(false)
const visibleMonth = ref(startOfMonth(new Date()))
const selectedDate = ref(toDateKey(new Date()))
const editingId = ref<number | null>(null)
const editForm = reactive({ date: '', startTime: '', endTime: '' })

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
  visibleMonth.value.toLocaleDateString(undefined, { month: 'long', year: 'numeric' })
)

const logsByDate = computed(() => {
  const map: Record<string, WearLog[]> = {}
  for (const log of wearLogs.value) {
    const key = toDateKey(new Date(log.wornDate))
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
    const key = toDateKey(new Date(log.wornDate))
    groups[key] ??= []
    groups[key].push(log)
  }
  return Object.entries(groups).map(([date, logs]) => ({ date, label: formatDayLabel(date), logs }))
})

function startEdit(log: WearLog) {
  editingId.value = log.id
  editForm.date = toDateKey(new Date(log.wornDate))
  editForm.startTime = toTimeInput(log.startedAt || log.wornDate)
  editForm.endTime = toTimeInput(log.endedAt)
}

function cancelEdit() {
  editingId.value = null
}

async function saveEdit(logId: number) {
  const wornDate = fromDateTime(editForm.date, editForm.startTime || '12:00')
  const startedAt = editForm.startTime ? fromDateTime(editForm.date, editForm.startTime) : undefined
  const endedAt = editForm.endTime ? fromDateTime(editForm.date, editForm.endTime) : undefined
  await updateWearLogDate(logId, wornDate, startedAt, endedAt)
  editingId.value = null
  await load()
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

function toTimeInput(dateStr?: string) {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  if (Number.isNaN(date.getTime())) return ''
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}

function fromDateTime(date: string, time: string) {
  return new Date(`${date}T${time}:00`).toISOString()
}

function formatDayLabel(dateKey: string) {
  return new Date(`${dateKey}T12:00:00`).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function formatLogTime(log: WearLog) {
  if (!log.startedAt && !log.endedAt) return new Date(log.wornDate).toLocaleDateString()
  const start = log.startedAt ? new Date(log.startedAt).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) : ''
  const end = log.endedAt ? new Date(log.endedAt).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) : ''
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
</style>
