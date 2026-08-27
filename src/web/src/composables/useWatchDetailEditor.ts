import { computed, ref, type Ref } from 'vue'
import type { AuthResponse, UpdateWatch, Watch } from '@/types'
import { fieldMeta, type InlineField } from '@/constants/watch'
import { api } from '@/services/api'
import { toUpdatePayload, updateWatch } from '@/services/watches'
import { dateInputValue, formatCalendarDate, formatInstant } from '@/utils/dateTime'

export interface DetailRowData {
  label: string
  value?: string
  href?: string
  field?: InlineField
}

export interface DetailSection {
  heading: string
  rows: DetailRowData[]
}

export function dispositionLabel(watch: Watch): string {
  if (!watch.disposition) return 'Active'
  return watch.disposition.type === 'Other'
    ? (watch.disposition.otherLabel || 'Other')
    : watch.disposition.type
}

export function serverMessage(error: unknown): string | undefined {
  const data = (error as { response?: { data?: Record<string, unknown> } })?.response?.data
  if (typeof data?.error === 'string') return data.error
  const errors = data?.errors as Record<string, string[]> | undefined
  const first = errors && Object.values(errors)[0]
  if (Array.isArray(first) && typeof first[0] === 'string') return first[0]
  if (typeof data?.title === 'string') return data.title
  return undefined
}

export function useWatchDetailEditor(watch: Ref<Watch | null>) {
  const editMode = ref(false)
  const editingField = ref<InlineField | null>(null)
  const editingLabel = ref('')
  const draft = ref('')
  const draftChanges = ref<Partial<UpdateWatch>>({})
  const savingEdits = ref(false)
  const editSessionError = ref('')
  const fieldError = ref<{ field: InlineField, message: string } | null>(null)
  const storageLocations = ref<string[]>([])

  function toInputValue(value: Watch, field: InlineField): string {
    const raw = (value as unknown as Record<string, unknown>)[field]
    if (raw === null || raw === undefined) return ''
    return fieldMeta[field].input === 'date' ? dateInputValue(String(raw)) : String(raw)
  }

  function fromInputValue(field: InlineField, text: string): string | number | undefined {
    const trimmed = text.trim()
    if (trimmed === '') return undefined
    if (fieldMeta[field].input === 'number') {
      const parsed = Number(trimmed)
      return Number.isFinite(parsed) ? parsed : undefined
    }
    return trimmed
  }

  const editableWatch = computed<Watch | null>(() => {
    if (!watch.value) return null
    return editMode.value ? { ...watch.value, ...draftChanges.value } : watch.value
  })

  const storageLocationOptions = computed(() => {
    const options = [...storageLocations.value]
    const current = watch.value?.storageLocation
    if (current && !options.includes(current)) options.push(current)
    return options
  })

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

  function commitEdit(): boolean {
    const value = editableWatch.value
    const field = editingField.value
    if (!value || !field || savingEdits.value) return false

    const next = fromInputValue(field, draft.value)
    if (toInputValue(value, field) === draft.value.trim()) {
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

  const notesDraft = computed<string>({
    get: () => draftChanges.value.notes ?? watch.value?.notes ?? '',
    set: (value) => {
      draftChanges.value = { ...draftChanges.value, notes: value.trim() === '' ? undefined : value }
    },
  })

  async function beginEdit() {
    editMode.value = true
    draftChanges.value = {}
    editSessionError.value = ''
    cancelEdit()
    fieldError.value = null

    if (!storageLocations.value.length) {
      try {
        const { data } = await api.get<AuthResponse>('/api/auth/me')
        storageLocations.value = data.storageLocations || []
      } catch {
        // The picker can still accept the existing storage location.
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
      watch.value = await updateWatch(watch.value.id, toUpdatePayload(watch.value, draftChanges.value))
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

  const detailSections = computed<DetailSection[]>(() => {
    const value = editableWatch.value
    if (!value) return []

    const money = (amount?: number) => (amount ? `$${amount.toFixed(2)}` : undefined)
    const mm = (amount?: number) => (amount ? `${amount} mm` : undefined)
    const fullDate = (date?: string) => date && formatInstant(date, { year: 'numeric', month: 'short', day: 'numeric' })
    const calendarDate = (date?: string) => date && formatCalendarDate(date.slice(0, 10), { year: 'numeric', month: 'short', day: 'numeric' })
    const ownership: DetailRowData[] = value.isWishList ? [] : [
      { label: 'Wear Count', value: value.timesWorn.toString() },
      { label: 'Last Worn', value: fullDate(value.lastWornDate) },
      { label: 'Status', value: dispositionLabel(value) },
    ]

    const sections: DetailSection[] = [
      { heading: 'Identification', rows: [
        { label: 'Brand', value: value.brand, field: 'brand' }, { label: 'Model', value: value.model, field: 'model' },
        { label: 'SKU / Reference', value: value.sku, field: 'sku' }, { label: 'Serial', value: value.serialNumber, field: 'serialNumber' },
        { label: 'Production Year', value: value.productionYear?.toString(), field: 'productionYear' }, { label: 'Origin', value: value.countryOfOrigin, field: 'countryOfOrigin' },
      ] },
      { heading: 'Case & Band', rows: [
        { label: 'Case Size', value: mm(value.caseSizeMm), field: 'caseSizeMm' }, { label: 'Lug Width', value: mm(value.lugWidthMm), field: 'lugWidthMm' },
        { label: 'Lug-to-Lug', value: mm(value.lugToLugMm), field: 'lugToLugMm' }, { label: 'Case Shape', value: value.caseShape, field: 'caseShape' },
        { label: 'Crystal', value: value.crystalType, field: 'crystalType' }, { label: 'Bezel', value: value.bezelType, field: 'bezelType' },
        { label: 'Crown', value: value.crownType, field: 'crownType' }, { label: 'Dial', value: value.dialColor, field: 'dialColor' },
        { label: 'Water Resistance', value: value.waterResistance, field: 'waterResistance' }, { label: 'Band Type', value: value.bandType, field: 'bandType' },
        { label: 'Band Color', value: value.bandColor, field: 'bandColor' },
      ] },
      { heading: 'Movement', rows: [
        { label: 'Movement Type', value: value.movementType, field: 'movementType' }, { label: 'Power Reserve', value: value.powerReserveHours ? `${value.powerReserveHours} hours` : undefined, field: 'powerReserveHours' },
        { label: 'Calendar', value: value.calendarType, field: 'calendarType' }, { label: 'Battery Type', value: value.batteryType, field: 'batteryType' },
        { label: 'Last Battery Changed', value: calendarDate(value.lastBatteryChangedDate), field: 'lastBatteryChangedDate' },
      ] },
      { heading: 'Purchase Details', rows: [
        { label: value.isWishList ? 'Target Price' : 'Purchase Price', value: money(value.purchasePrice), field: 'purchasePrice' },
        { label: 'Purchase Date', value: calendarDate(value.purchaseDate), field: 'purchaseDate' }, { label: 'Acquisition Type', value: value.acquisitionType, field: 'acquisitionType' },
        { label: 'Acquired From', value: value.acquiredFrom, field: 'acquiredFrom' },
        ...(editMode.value ? [{ label: 'Acquisition Source URL', value: value.acquisitionSourceUrl, field: 'acquisitionSourceUrl' as InlineField }] : [{ label: 'Acquisition Source', value: value.acquisitionSourceUrl ? (value.acquiredFrom || 'Source Link') : undefined, href: value.acquisitionSourceUrl }]),
        { label: 'Current Resale', value: money(value.currentResaleValue) }, { label: 'Resale Updated', value: fullDate(value.resaleValueUpdatedAt) },
        ...(editMode.value ? [
          { label: 'Product / Reference URL', value: value.linkUrl, field: 'linkUrl' as InlineField },
          { label: 'Product Link Text', value: value.linkText, field: 'linkText' as InlineField },
        ] : [{ label: 'Product / Reference', value: value.linkUrl ? (value.linkText || 'Product Link') : undefined, href: value.linkUrl }]),
      ] },
      ...(value.disposition ? [{ heading: 'Disposition', rows: [
        { label: 'Action', value: dispositionLabel(value) }, { label: 'Date', value: calendarDate(value.disposition.dispositionDate) },
        { label: 'Sold To', value: value.disposition.soldTo }, { label: 'Sale Price', value: money(value.disposition.salePrice) },
        { label: 'Received Watch', value: value.disposition.receivedWatchName }, { label: 'Trade Details', value: value.disposition.tradeDetails },
        { label: 'Return Reason', value: value.disposition.returnReason }, { label: 'Returned To', value: value.disposition.returnedTo },
        { label: 'Refund Amount', value: money(value.disposition.refundAmount) }, { label: 'Notes', value: value.disposition.notes },
      ] }] : []),
      { heading: 'Ownership', rows: [
        { label: 'Storage', value: value.storageLocation, field: 'storageLocation' }, ...ownership,
        { label: 'Added', value: fullDate(value.createdAt) }, { label: 'Last Updated', value: fullDate(value.updatedAt) },
      ] },
    ]

    return sections.map(section => ({
      ...section,
      rows: editMode.value ? section.rows.filter(row => row.field || row.value) : section.rows.filter(row => Boolean(row.value)),
    })).filter(section => section.rows.length > 0)
  })

  return {
    beginEdit, cancelEdit, commitEdit, detailSections, discardEdits, draft, editMode, editSessionError,
    editingField, errorFor, notesDraft, saveEdits, savingEdits, startEdit, storageLocationOptions,
  }
}
