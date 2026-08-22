<template>
  <div>
    <RouterLink
      :to="watch?.isWishList ? '/?tab=wishlist' : watch?.disposition ? '/retired' : '/'"
      class="text-accent text-sm hover:underline mb-4 inline-block"
    >
      ← Back
    </RouterLink>
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>
    <div v-else-if="watch" class="max-w-5xl mx-auto">
      <!-- Header -->
      <div class="relative mb-5 flex items-start justify-between gap-4">
        <div class="min-w-0">
          <p class="text-xs uppercase tracking-[0.24em] text-accent mb-2">{{ watch.isWishList ? 'Wish List' : watch.disposition ? dispositionLabel(watch) : 'Collection' }}</p>
          <h1 ref="titleEl" class="font-display text-3xl font-semibold text-text leading-tight" :class="{ 'title-stacked': titleStacked }"><span class="watch-brand">{{ watch.brand }}</span> {{ watch.model }}<span class="title-probe-clip" aria-hidden="true"><span ref="titleProbeEl" class="title-probe">{{ watch.brand }} {{ watch.model }}</span></span></h1>
        </div>
        <div class="relative flex flex-shrink-0 items-center gap-2">
          <template v-if="editMode">
            <button
              @click="saveEdits"
              :disabled="savingEdits"
              class="header-action text-success"
              aria-label="Save edits"
              title="Save edits"
            >
              <AppIcon name="check" :size="20" :stroke-width="2" />
            </button>
            <button
              @click="discardEdits"
              :disabled="savingEdits"
              class="header-action text-danger"
              aria-label="Discard edits"
              title="Discard edits"
            >
              <AppIcon name="close" :size="20" :stroke-width="2" />
            </button>
          </template>
          <button
            v-else
            @click="beginEdit"
            class="header-action"
            aria-label="Edit watch"
            title="Edit watch"
          >
            <AppIcon name="edit" :size="20" :stroke-width="1.75" />
          </button>
          <button
            @click="actionsOpen = !actionsOpen"
            class="header-action text-xl leading-none"
            aria-label="Watch actions"
          >
            …
          </button>
          <div v-if="actionsOpen" class="absolute right-0 top-12 z-30 w-56 bg-bg-card border border-border rounded-xl shadow-xl overflow-hidden">
            <template v-if="!watch.isWishList">
              <button v-if="!watch.disposition" @click="handleWearFromMenu" :disabled="wearLoading" class="menu-action text-accent">{{ wearLoading ? 'Recording...' : 'Wore Today' }}</button>
              <label class="menu-action cursor-pointer">
                {{ uploading ? 'Uploading…' : 'Upload Images' }}
                <input type="file" accept="image/*" multiple class="hidden" @change="handleImageUpload" :disabled="uploading" />
              </label>
              <button @click="handleAnalyzeFromMenu" :disabled="analyzing || !watch.imageUrls.length" class="menu-action">{{ analyzing ? 'Analyzing…' : 'AI Analyze' }}</button>
              <button @click="openStyleAgent" class="menu-action">Style Agent</button>
              <button @click="handleRefreshResaleFromMenu" :disabled="refreshingResale" class="menu-action">{{ refreshingResale ? 'Queuing…' : 'Refresh Resale' }}</button>
              <button @click="openDisposition" class="menu-action">{{ watch.disposition ? 'Edit disposition' : 'Remove from collection' }}</button>
              <button v-if="watch.disposition" @click="handleRestore" class="menu-action text-accent">Restore to collection</button>
              <button @click="handleDeleteFromMenu" class="menu-action text-danger">Delete</button>
            </template>
            <template v-else>
              <button @click="handlePurchaseFromMenu" :disabled="purchasing" class="menu-action text-accent">{{ purchasing ? 'Moving…' : 'Mark Purchased' }}</button>
              <button @click="openStyleAgent" class="menu-action">Style Agent</button>
              <label class="menu-action cursor-pointer">
                {{ uploading ? 'Uploading…' : 'Upload Images' }}
                <input type="file" accept="image/*" multiple class="hidden" @change="handleImageUpload" :disabled="uploading" />
              </label>
              <button @click="handleDeleteFromMenu" class="menu-action text-danger">Delete</button>
            </template>
          </div>
        </div>
        <p v-if="editSessionError" class="text-sm text-danger mb-5">{{ editSessionError }}</p>
      </div>

      <!-- Image Gallery -->
      <div v-if="watch.imageUrls.length > 0" class="relative rounded-xl overflow-hidden bg-bg-surface mb-6">
        <div class="h-[300px] lg:h-[400px] flex items-center justify-center">
          <img
            :src="imageUrl(watch.imageUrls[imageIndex].url)"
            :alt="`${watch.brand} ${watch.model}`"
            class="max-w-full max-h-full object-contain"
          />
        </div>
        <div v-if="watch.imageUrls.length > 1" class="absolute bottom-3 inset-x-0 flex justify-center gap-1.5">
          <button
            v-for="(_, i) in watch.imageUrls"
            :key="i"
            @click="imageIndex = i"
            class="w-2 h-2 rounded-full transition-colors"
            :class="i === imageIndex ? 'bg-accent' : 'bg-white/40'"
          />
        </div>
      </div>

      <div v-if="watch.imageUrls.length > 0" class="flex flex-wrap gap-2 mb-6">
        <button
          @click="handleRemoveBackground"
          :disabled="removingBg"
          class="px-3 py-1.5 bg-bg-surface border border-border text-text-secondary text-xs rounded-lg hover:border-accent/50 transition-colors disabled:opacity-50"
        >
          {{ removingBg ? 'Removing…' : 'Remove Background' }}
        </button>
        <button
          @click="handleDeleteImage"
          class="px-3 py-1.5 bg-bg-surface border border-danger/50 text-danger text-xs rounded-lg hover:bg-danger/10 transition-colors"
        >
          Delete Image
        </button>
      </div>

      <!-- Rows without a value are dropped, and a section with no rows left is not rendered -->
      <section v-for="section in detailSections" :key="section.heading" class="detail-card mb-6">
        <h2 class="detail-heading">{{ section.heading }}</h2>
        <dl class="detail-list">
          <DetailRow
            v-for="row in section.rows"
            :key="row.label"
            :label="row.label"
            :value="row.value"
            :href="row.href"
            :field="row.field"
            :editable="editMode && !!row.field"
            :editing="editingField === row.field"
            :saving="savingEdits"
            :error="errorFor(row.field)"
            :options="row.field === 'storageLocation' ? storageLocationOptions : undefined"
            :draft="draft"
            @start="startEdit(row)"
            @commit="commitEdit"
            @cancel="cancelEdit"
            @update:draft="draft = $event"
          />
        </dl>
      </section>

      <!-- AI Analysis -->
      <section v-if="watch.aiAnalysis" class="detail-card mb-6">
        <h2 class="detail-heading">AI Analysis</h2>
        <div class="prose-markdown text-sm text-text" v-html="renderMarkdown(watch.aiAnalysis)" />
      </section>

      <!-- Style Agent -->
      <section class="detail-card mb-6">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div class="min-w-0">
            <h2 class="detail-heading !mb-1">Style Agent</h2>
            <p class="text-sm text-text-muted">
              Talk through an outfit for this watch. It asks about the occasion and the weather, and remembers what it
              suggested before.
            </p>
          </div>
          <button
            @click="openStyleAgent"
            class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors flex-shrink-0"
          >
            Style this watch
          </button>
        </div>
      </section>

      <!-- Resale Value History -->
      <section v-if="!watch.isWishList" class="detail-card mb-6">
        <div class="flex items-center justify-between mb-2">
          <h2 class="detail-heading !mb-0">Resale Value History</h2>
          <button @click="showManualResaleForm = !showManualResaleForm" class="text-xs text-accent hover:underline">
            {{ showManualResaleForm ? 'Cancel' : '+ Log Value' }}
          </button>
        </div>
        <p v-if="resaleError" class="text-xs text-danger mb-2">{{ resaleError }}</p>
        <p v-if="resaleQueuedMsg" class="text-xs text-success mb-2">{{ resaleQueuedMsg }}</p>

        <div v-if="showManualResaleForm" class="flex flex-wrap items-end gap-2 mb-4 p-3 bg-bg-surface border border-border rounded-lg">
          <div>
            <label class="block text-xs text-text-muted mb-1">Value</label>
            <input v-model.number="manualResaleValue" type="number" step="0.01" min="0" placeholder="0.00" class="w-28 px-2 py-1.5 bg-bg border border-border rounded-lg text-sm text-text" />
          </div>
          <div>
            <label class="block text-xs text-text-muted mb-1">Date</label>
            <input v-model="manualResaleDate" type="date" class="px-2 py-1.5 bg-bg border border-border rounded-lg text-sm text-text" />
          </div>
          <div class="flex-1 min-w-[8rem]">
            <label class="block text-xs text-text-muted mb-1">Notes</label>
            <input v-model="manualResaleNotes" type="text" placeholder="Optional" class="w-full px-2 py-1.5 bg-bg border border-border rounded-lg text-sm text-text" />
          </div>
          <button
            @click="handleAddManualResale"
            :disabled="savingManualResale || !manualResaleValue"
            class="px-3 py-1.5 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ savingManualResale ? 'Saving…' : 'Save' }}
          </button>
        </div>

        <div v-if="resaleHistory.length === 0" class="text-sm text-text-muted">
          No resale value recorded yet — log one manually or refresh an estimate.
        </div>
        <div v-else class="space-y-2">
          <div v-for="entry in resaleHistory" :key="entry.id" class="flex items-start justify-between gap-3 text-sm">
            <div class="min-w-0">
              <span class="text-text font-medium">${{ entry.value.toFixed(2) }}</span>
              <span class="ml-2 px-1.5 py-0.5 rounded-full text-[10px] uppercase tracking-wide bg-bg-surface border border-border text-text-muted">
                {{ entry.source === 'Manual' ? 'Manual' : 'Web Estimate' }}
              </span>
              <span class="ml-2 text-xs text-text-muted">{{ formatInstant(entry.recordedAt, { year: 'numeric', month: 'numeric', day: 'numeric' }) }}</span>
              <p v-if="entry.reasoning" class="text-xs text-text-muted mt-0.5 truncate" :title="entry.reasoning">{{ entry.reasoning }}</p>
            </div>
            <button @click="handleDeleteResaleEntry(entry.id)" class="text-xs text-danger hover:underline flex-shrink-0">Remove</button>
          </div>
        </div>
      </section>

      <!-- Notes -->
      <section v-if="watch.notes" class="detail-card">
        <h2 class="detail-heading">Notes</h2>
        <div class="prose-markdown text-sm text-text" v-html="renderMarkdown(watch.notes)" />
      </section>
    </div>
    <div v-else class="text-center py-20 text-text-muted">Watch not found.</div>
    <StyleAgentModal
      v-if="showStyleAgent && watch"
      :watch-id="watch.id"
      :watch-name="`${watch.brand} ${watch.model}`"
      :has-photo="watch.imageUrls.length > 0"
      @close="showStyleAgent = false"
    />
    <DispositionModal
      v-if="showDispositionModal && watch"
      :current-watch-id="watch.id"
      :disposition="watch.disposition"
      :watches="allWatches"
      :saving="savingDisposition"
      :error-message="dispositionError"
      @cancel="showDispositionModal = false"
      @save="handleSaveDisposition"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch as vueWatch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { marked } from 'marked'
import type { AuthResponse, UpdateWatch, UpdateWatchDisposition, Watch, ResaleValueEntry } from '@/types'
import { fieldMeta, type InlineField } from '@/constants/watch'
import { api } from '@/services/api'
import DetailRow from '@/components/common/DetailRow.vue'
import DispositionModal from '@/components/common/DispositionModal.vue'
import StyleAgentModal from '@/components/common/StyleAgentModal.vue'
import AppIcon from '@/components/icons/AppIcon.vue'
import { dateInputValue, formatCalendarDate, formatInstant } from '@/utils/dateTime'
import {
  getWatch, getWatches, imageUrl, recordWear, deleteWatch, uploadImage, deleteImage, removeBackground,
  analyzeWatch, updateWatch, toUpdatePayload, getResaleHistory, addManualResaleValue,
  deleteResaleValueEntry, refreshResaleValue, setWatchDisposition, clearWatchDisposition,
} from '@/services/watches'

const route = useRoute()
const router = useRouter()

function renderMarkdown(text: string): string {
  return marked.parse(text, { async: false }) as string
}

function formatFullDate(dateStr?: string): string | undefined {
  if (!dateStr) return undefined
  return formatInstant(dateStr, { year: 'numeric', month: 'short', day: 'numeric' })
}

function formatCalendarValue(dateStr?: string): string | undefined {
  if (!dateStr) return undefined
  return formatCalendarDate(
    dateStr.slice(0, 10),
    { year: 'numeric', month: 'short', day: 'numeric' },
  )
}

const watch = ref<Watch | null>(null)
const loading = ref(true)

function dispositionLabel(w: Watch): string {
  if (!w.disposition) return 'Active'
  return w.disposition.type === 'Other'
    ? (w.disposition.otherLabel || 'Other')
    : w.disposition.type
}

interface DetailRowData {
  label: string
  value?: string
  href?: string
  /** Set on rows the user can edit in place. Absent means derived or system-set. */
  field?: InlineField
}

// --- Inline editing -------------------------------------------------------

const editMode = ref(false)
const editingField = ref<InlineField | null>(null)
const editingLabel = ref('')
const draft = ref('')
const draftChanges = ref<Partial<UpdateWatch>>({})
const savingEdits = ref(false)
const editSessionError = ref('')
const fieldError = ref<{ field: InlineField, message: string } | null>(null)
const storageLocations = ref<string[]>([])

function formatDateForInput(value?: string | null): string {
  return dateInputValue(value)
}

/** The stored value of a field, as the matching input wants to receive it. */
function toInputValue(w: Watch, field: InlineField): string {
  const raw = (w as unknown as Record<string, unknown>)[field]
  if (raw === null || raw === undefined) return ''
  if (fieldMeta[field].input === 'date') return formatDateForInput(String(raw))
  return String(raw)
}

/** What the user typed, as the API wants to receive it. */
function fromInputValue(field: InlineField, text: string): string | number | undefined {
  const trimmed = text.trim()
  if (trimmed === '') return undefined
  if (fieldMeta[field].input === 'number') {
    const parsed = Number(trimmed)
    return Number.isFinite(parsed) ? parsed : undefined
  }
  return trimmed
}

function startEdit(row: DetailRowData) {
  if (!row.field || !editableWatch.value) return
  editingField.value = row.field
  editingLabel.value = row.label
  draft.value = toInputValue(editableWatch.value, row.field)
  fieldError.value = null
}

function cancelEdit() {
  editingField.value = null
  draft.value = ''
}

/** Client-side mirror of the DTO's validation, so a bad value never round-trips. */
function validate(field: InlineField, value: string | number | undefined): string | null {
  const meta = fieldMeta[field]
  if (meta.required && (value === undefined || value === '')) return `${editingLabel.value} cannot be empty.`
  if (typeof value === 'number') {
    if (meta.min !== undefined && value < meta.min) return `Must be ${meta.min} or more.`
    if (meta.max !== undefined && value > meta.max) return `Must be ${meta.max} or less.`
  }
  if (typeof value === 'string' && meta.maxlength && value.length > meta.maxlength) {
    return `Must be ${meta.maxlength} characters or fewer.`
  }
  return null
}

function errorFor(field?: InlineField): string | undefined {
  const current = fieldError.value
  return field && current?.field === field ? current.message : undefined
}

function serverMessage(error: unknown): string | undefined {
  const data = (error as { response?: { data?: Record<string, unknown> } })?.response?.data
  if (typeof data?.error === 'string') return data.error
  const errors = data?.errors as Record<string, string[]> | undefined
  const first = errors && Object.values(errors)[0]
  if (Array.isArray(first) && typeof first[0] === 'string') return first[0]
  if (typeof data?.title === 'string') return data.title
  return undefined
}

function commitEdit(): boolean {
  const w = editableWatch.value
  const field = editingField.value
  if (!w || !field || savingEdits.value) return false

  const next = fromInputValue(field, draft.value)

  if (toInputValue(w, field) === draft.value.trim()) {
    cancelEdit()
    return true
  }

  const problem = validate(field, next)
  if (problem) {
    fieldError.value = { field, message: problem }
    return false
  }

  draftChanges.value = { ...draftChanges.value, [field]: next }
  fieldError.value = null
  cancelEdit()
  return true
}

const editableWatch = computed<Watch | null>(() => {
  if (!watch.value) return null
  return editMode.value
    ? { ...watch.value, ...draftChanges.value }
    : watch.value
})

// Keeps a location the user already has but that is no longer configured.
const storageLocationOptions = computed(() => {
  const options = [...storageLocations.value]
  const current = watch.value?.storageLocation
  if (current && !options.includes(current)) options.push(current)
  return options
})

async function beginEdit() {
  actionsOpen.value = false
  editMode.value = true
  draftChanges.value = {}
  editSessionError.value = ''
  cancelEdit()
  fieldError.value = null

  // Only the storage picker needs these, so they are fetched the first time
  // editing starts rather than on every view of the page.
  if (!storageLocations.value.length) {
    try {
      const { data } = await api.get<AuthResponse>('/api/auth/me')
      storageLocations.value = data.storageLocations || []
    } catch {
      // Not fatal — the picker just offers nothing but the current value.
    }
  }
}

async function saveEdits() {
  if (!watch.value || savingEdits.value) return
  if (editingField.value && !commitEdit()) return

  if (Object.keys(draftChanges.value).length === 0) {
    discardEdits()
    return
  }

  savingEdits.value = true
  editSessionError.value = ''
  try {
    watch.value = await updateWatch(
      watch.value.id,
      toUpdatePayload(watch.value, draftChanges.value),
    )
    editMode.value = false
    draftChanges.value = {}
  } catch (error) {
    editSessionError.value = serverMessage(error) || 'Could not save these changes.'
  } finally {
    savingEdits.value = false
  }
}

function discardEdits() {
  if (savingEdits.value) return
  editMode.value = false
  draftChanges.value = {}
  editSessionError.value = ''
  fieldError.value = null
  cancelEdit()
}

const detailSections = computed(() => {
  const w = editableWatch.value
  if (!w) return []

  const money = (value?: number) => (value ? `$${value.toFixed(2)}` : undefined)
  const mm = (value?: number) => (value ? `${value} mm` : undefined)
  const ownership: DetailRowData[] = w.isWishList ? [] : [
    { label: 'Wear Count', value: w.timesWorn.toString() },
    { label: 'Last Worn', value: formatFullDate(w.lastWornDate) },
    { label: 'Status', value: dispositionLabel(w) },
  ]

  const sections: { heading: string, rows: DetailRowData[] }[] = [
    {
      heading: 'Identification',
      rows: [
        { label: 'Brand', value: w.brand, field: 'brand' },
        { label: 'Model', value: w.model, field: 'model' },
        { label: 'SKU / Reference', value: w.sku, field: 'sku' },
        { label: 'Serial', value: w.serialNumber, field: 'serialNumber' },
        { label: 'Production Year', value: w.productionYear?.toString(), field: 'productionYear' },
        { label: 'Origin', value: w.countryOfOrigin, field: 'countryOfOrigin' },
      ],
    },
    {
      heading: 'Case & Band',
      rows: [
        { label: 'Case Size', value: mm(w.caseSizeMm), field: 'caseSizeMm' },
        { label: 'Lug Width', value: mm(w.lugWidthMm), field: 'lugWidthMm' },
        { label: 'Case Shape', value: w.caseShape, field: 'caseShape' },
        { label: 'Crystal', value: w.crystalType, field: 'crystalType' },
        { label: 'Bezel', value: w.bezelType, field: 'bezelType' },
        { label: 'Crown', value: w.crownType, field: 'crownType' },
        { label: 'Dial', value: w.dialColor, field: 'dialColor' },
        { label: 'Water Resistance', value: w.waterResistance, field: 'waterResistance' },
        { label: 'Band Type', value: w.bandType, field: 'bandType' },
        { label: 'Band Color', value: w.bandColor, field: 'bandColor' },
      ],
    },
    {
      heading: 'Movement',
      rows: [
        { label: 'Movement Type', value: w.movementType, field: 'movementType' },
        { label: 'Power Reserve', value: w.powerReserveHours ? `${w.powerReserveHours} hours` : undefined, field: 'powerReserveHours' },
        { label: 'Calendar', value: w.calendarType, field: 'calendarType' },
        { label: 'Battery Type', value: w.batteryType, field: 'batteryType' },
        { label: 'Last Battery Changed', value: formatCalendarValue(w.lastBatteryChangedDate), field: 'lastBatteryChangedDate' },
      ],
    },
    {
      heading: 'Purchase Details',
      rows: [
        { label: w.isWishList ? 'Target Price' : 'Purchase Price', value: money(w.purchasePrice), field: 'purchasePrice' },
        { label: 'Purchase Date', value: formatCalendarValue(w.purchaseDate), field: 'purchaseDate' },
        { label: 'Acquisition Type', value: w.acquisitionType, field: 'acquisitionType' },
        { label: 'Acquired From', value: w.acquiredFrom, field: 'acquiredFrom' },
        ...(editMode.value
          ? [{ label: 'Acquisition Source URL', value: w.acquisitionSourceUrl, field: 'acquisitionSourceUrl' as InlineField }]
          : [{ label: 'Acquisition Source', value: w.acquisitionSourceUrl ? (w.acquiredFrom || 'Source Link') : undefined, href: w.acquisitionSourceUrl }]),
        { label: 'Current Resale', value: money(w.currentResaleValue) },
        { label: 'Resale Updated', value: formatFullDate(w.resaleValueUpdatedAt) },
        // One display row is two fields underneath, so it splits to be edited.
        ...(editMode.value
          ? [
            { label: 'Product / Reference URL', value: w.linkUrl, field: 'linkUrl' as InlineField },
            { label: 'Product Link Text', value: w.linkText, field: 'linkText' as InlineField },
          ]
          : [
            { label: 'Product / Reference', value: w.linkUrl ? (w.linkText || 'Product Link') : undefined, href: w.linkUrl },
          ]),
      ],
    },
    ...(w.disposition
      ? [{
          heading: 'Disposition',
          rows: [
            { label: 'Action', value: dispositionLabel(w) },
            { label: 'Date', value: formatCalendarValue(w.disposition.dispositionDate) },
            { label: 'Sold To', value: w.disposition.soldTo },
            { label: 'Sale Price', value: money(w.disposition.salePrice) },
            { label: 'Received Watch', value: w.disposition.receivedWatchName },
            { label: 'Trade Details', value: w.disposition.tradeDetails },
            { label: 'Return Reason', value: w.disposition.returnReason },
            { label: 'Returned To', value: w.disposition.returnedTo },
            { label: 'Refund Amount', value: money(w.disposition.refundAmount) },
            { label: 'Notes', value: w.disposition.notes },
          ],
        }]
      : []),
    {
      heading: 'Ownership',
      rows: [
        { label: 'Storage', value: w.storageLocation, field: 'storageLocation' },
        ...ownership,
        { label: 'Added', value: formatFullDate(w.createdAt) },
        { label: 'Last Updated', value: formatFullDate(w.updatedAt) },
      ],
    },
  ]

  // Edit mode keeps every editable row, so a field with no value yet can be filled in.
  return sections
    .map(section => ({
      ...section,
      rows: editMode.value
        ? section.rows.filter(row => row.field || row.value)
        : section.rows.filter(row => Boolean(row.value)),
    }))
    .filter(section => section.rows.length > 0)
})

// Keep brand and model on one line while they fit, and only then split them.
const titleEl = ref<HTMLElement | null>(null)
const titleProbeEl = ref<HTMLElement | null>(null)
const titleStacked = ref(false)
let titleObserver: ResizeObserver | null = null

function measureTitle() {
  const el = titleEl.value
  const probe = titleProbeEl.value
  if (!el || !probe) return
  // The probe is nowrap and out of flow, so its width is the natural one-line width.
  titleStacked.value = probe.getBoundingClientRect().width > el.clientWidth + 0.5
}

// Stacking changes the title's height but never its width, so observing width is stable.
vueWatch(titleEl, (el) => {
  titleObserver?.disconnect()
  titleObserver = null
  if (!el) return
  titleObserver = new ResizeObserver(() => measureTitle())
  titleObserver.observe(el)
})

// The display font loads after first paint and changes the measurement.
onMounted(() => { document.fonts?.ready.then(measureTitle) })

// Re-measure when the title text itself changes.
vueWatch(() => watch.value && `${watch.value.brand} ${watch.value.model}`, () => nextTick(measureTitle))

onBeforeUnmount(() => {
  titleObserver?.disconnect()
  titleObserver = null
})
const imageIndex = ref(0)
const wearLoading = ref(false)
const uploading = ref(false)
const removingBg = ref(false)
const analyzing = ref(false)
const purchasing = ref(false)
const actionsOpen = ref(false)

const resaleHistory = ref<ResaleValueEntry[]>([])
const refreshingResale = ref(false)
const resaleError = ref('')
const resaleQueuedMsg = ref('')
const showManualResaleForm = ref(false)
const manualResaleValue = ref<number | null>(null)
const manualResaleDate = ref('')
const manualResaleNotes = ref('')
const savingManualResale = ref(false)
const allWatches = ref<Watch[]>([])
const showStyleAgent = ref(false)
const showDispositionModal = ref(false)
const savingDisposition = ref(false)
const dispositionError = ref('')

onMounted(async () => {
  try {
    const [loadedWatch, loadedWatches] = await Promise.all([
      getWatch(Number(route.params.id)),
      getWatches(true),
    ])
    watch.value = loadedWatch
    allWatches.value = loadedWatches
    if (watch.value && !watch.value.isWishList) {
      resaleHistory.value = await getResaleHistory(watch.value.id)
    }
  } finally {
    loading.value = false
  }
})

async function handleWear() {
  if (!watch.value) return
  wearLoading.value = true
  try {
    await recordWear(watch.value.id)
    watch.value.timesWorn++
  } finally {
    wearLoading.value = false
  }
}

async function handleWearFromMenu() {
  actionsOpen.value = false
  await handleWear()
}

function openStyleAgent() {
  actionsOpen.value = false
  showStyleAgent.value = true
}

function openDisposition() {
  actionsOpen.value = false
  dispositionError.value = ''
  showDispositionModal.value = true
}

async function handleDeleteFromMenu() {
  actionsOpen.value = false
  await handleDelete()
}

async function handlePurchaseFromMenu() {
  actionsOpen.value = false
  await handlePurchase()
}

async function handleAnalyzeFromMenu() {
  actionsOpen.value = false
  await handleAnalyze()
}

async function handleRefreshResaleFromMenu() {
  actionsOpen.value = false
  await handleRefreshResale()
}

async function handleSaveDisposition(disposition: UpdateWatchDisposition) {
  if (!watch.value) return
  savingDisposition.value = true
  dispositionError.value = ''
  try {
    watch.value = await setWatchDisposition(watch.value.id, disposition)
    allWatches.value = allWatches.value.map(item => item.id === watch.value?.id ? watch.value : item)
    showDispositionModal.value = false
  } catch (error) {
    dispositionError.value = serverMessage(error) || 'Could not save the disposition.'
  } finally {
    savingDisposition.value = false
  }
}

async function handleRestore() {
  actionsOpen.value = false
  if (!watch.value || !confirm('Restore this watch to your active collection?')) return
  watch.value = await clearWatchDisposition(watch.value.id)
  allWatches.value = allWatches.value.map(item => item.id === watch.value?.id ? watch.value : item)
}

async function handleDelete() {
  if (!watch.value || !confirm('Delete this watch permanently?')) return
  const returnTo = watch.value.isWishList
    ? { path: '/', query: { tab: 'wishlist' } }
    : { path: '/' }
  await deleteWatch(watch.value.id)
  router.push(returnTo)
}

async function handlePurchase() {
  if (!watch.value || !confirm('Move this watch from your wish list to your collection?')) return
  purchasing.value = true
  try {
    const w = watch.value
    watch.value = await updateWatch(w.id, toUpdatePayload(w, { isWishList: false }))
  } finally {
    purchasing.value = false
  }
}

async function handleImageUpload(e: Event) {
  if (!watch.value) return
  const files = (e.target as HTMLInputElement).files
  if (!files?.length) return
  uploading.value = true
  try {
    for (const file of Array.from(files)) {
      await uploadImage(watch.value.id, file)
    }
    watch.value = await getWatch(watch.value.id)
  } finally {
    uploading.value = false
    ;(e.target as HTMLInputElement).value = ''
  }
}

async function handleDeleteImage() {
  if (!watch.value || !watch.value.imageUrls[imageIndex.value]) return
  if (!confirm('Delete this image?')) return
  const img = watch.value.imageUrls[imageIndex.value]
  await deleteImage(watch.value.id, img.id)
  watch.value = await getWatch(watch.value.id)
  if (imageIndex.value >= watch.value.imageUrls.length) {
    imageIndex.value = Math.max(0, watch.value.imageUrls.length - 1)
  }
}

async function handleRemoveBackground() {
  if (!watch.value || !watch.value.imageUrls[imageIndex.value]) return
  removingBg.value = true
  try {
    const img = watch.value.imageUrls[imageIndex.value]
    await removeBackground(watch.value.id, img.id)
    watch.value = await getWatch(watch.value.id)
  } finally {
    removingBg.value = false
  }
}

async function handleAnalyze() {
  if (!watch.value || !watch.value.imageUrls[imageIndex.value]) return
  analyzing.value = true
  try {
    const img = watch.value.imageUrls[imageIndex.value]
    const analysis = await analyzeWatch(watch.value.id, img.id)
    watch.value.aiAnalysis = analysis
    watch.value = await getWatch(watch.value.id)
  } finally {
    analyzing.value = false
  }
}

async function handleRefreshResale() {
  if (!watch.value) return
  refreshingResale.value = true
  resaleError.value = ''
  resaleQueuedMsg.value = ''
  try {
    await refreshResaleValue(watch.value.id)
    resaleQueuedMsg.value = 'Refresh queued — check back in a bit for the updated value.'
  } catch (e: any) {
    resaleError.value = e?.response?.data?.error || 'Could not queue a resale value refresh.'
  } finally {
    refreshingResale.value = false
  }
}

async function handleAddManualResale() {
  if (!watch.value || manualResaleValue.value === null) return
  savingManualResale.value = true
  resaleError.value = ''
  try {
    watch.value = await addManualResaleValue(watch.value.id, {
      value: manualResaleValue.value,
      recordedAt: manualResaleDate.value || undefined,
      notes: manualResaleNotes.value || undefined,
    })
    resaleHistory.value = await getResaleHistory(watch.value.id)
    manualResaleValue.value = null
    manualResaleDate.value = ''
    manualResaleNotes.value = ''
    showManualResaleForm.value = false
  } finally {
    savingManualResale.value = false
  }
}

async function handleDeleteResaleEntry(entryId: number) {
  if (!watch.value || !confirm('Remove this resale value entry?')) return
  await deleteResaleValueEntry(entryId)
  resaleHistory.value = resaleHistory.value.filter(e => e.id !== entryId)
  watch.value = await getWatch(watch.value.id)
}
</script>

<style scoped>
/* "Brand Model" wraps wherever it runs out of room, which splits the model
   itself across lines. Only once the pair no longer fits does .title-stacked
   get set, giving the brand its own line so the break lands between the two. */
.title-stacked .watch-brand {
  display: block;
}

/* Off-flow copy of the full title on one line, used to measure whether it fits.
   An absolutely positioned box still counts toward the page's scrollable
   overflow even while hidden, so the probe is clipped inside a zero-sized box.
   Its own width survives that: nowrap makes min-content equal max-content, so
   the inner span still reports the natural one-line width. */
.title-probe-clip {
  position: absolute;
  top: 0;
  left: 0;
  width: 0;
  height: 0;
  overflow: hidden;
}

.title-probe {
  display: inline-block;
  visibility: hidden;
  white-space: nowrap;
  pointer-events: none;
}

.detail-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  padding: 1.25rem;
  box-shadow: inset 0 1px 0 rgb(255 255 255 / 0.03);
}

.header-action {
  display: inline-flex;
  width: 2.75rem;
  height: 2.75rem;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-bg-surface);
  color: var(--color-text);
  transition: border-color 0.15s ease, color 0.15s ease;
}

.header-action:hover:not(:disabled),
.header-action:focus-visible {
  border-color: color-mix(in srgb, var(--color-accent) 50%, transparent);
  color: var(--color-accent);
}

.header-action:disabled {
  cursor: default;
  opacity: 0.5;
}

.detail-heading {
  color: var(--color-accent);
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.22em;
  margin-bottom: 0.85rem;
  text-transform: uppercase;
}

.detail-list {
  display: grid;
  gap: 0;
}

:deep(.detail-row) {
  display: grid;
  grid-template-columns: minmax(8rem, 0.7fr) minmax(0, 1fr);
  gap: 1rem;
  padding: 0.85rem 0;
  border-bottom: 1px solid var(--color-border);
}

:deep(.detail-row:last-child) {
  border-bottom: 0;
}

:deep(.detail-label) {
  color: var(--color-text-secondary);
  font-size: 0.95rem;
}

:deep(.detail-value) {
  color: var(--color-text);
  font-size: 1rem;
  min-width: 0;
  overflow-wrap: anywhere;
}

:deep(.detail-link) {
  color: var(--color-accent);
}

:deep(.detail-link:hover) {
  text-decoration: underline;
}

.menu-action {
  display: block;
  width: 100%;
  padding: 0.75rem 1rem;
  text-align: left;
  color: var(--color-text);
  font-size: 0.9rem;
  font-weight: 500;
  white-space: nowrap;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.menu-action:hover {
  background: var(--color-bg-surface);
  color: var(--color-accent);
}

.menu-action:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

@media (min-width: 768px) {
  .detail-card {
    padding: 1.5rem 1.75rem;
  }
}
</style>
