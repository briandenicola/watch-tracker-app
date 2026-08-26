import { api } from './api'
import type {
  Watch, CreateWatch, UpdateWatch, WearLog, ResaleValueEntry, CreateResaleValueEntry,
  UpdateWatchDisposition, WatchAnalysisResult, ApplyAnalysisResult,
  WishlistExtractionResult,
} from '@/types'

const BASE_URL = window.location.origin

export function imageUrl(path: string): string {
  if (path.startsWith('http')) return path
  return `${BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`
}

export async function getWatches(includeDisposed = false): Promise<Watch[]> {
  const { data } = await api.get<Watch[]>('/api/watches', {
    params: includeDisposed ? { includeDisposed: true } : undefined,
  })
  return data
}

export async function getWatch(id: number): Promise<Watch> {
  const { data } = await api.get<Watch>(`/api/watches/${id}`)
  return data
}

export async function createWatch(watch: CreateWatch): Promise<Watch> {
  const { data } = await api.post<Watch>('/api/watches', watch)
  return data
}

export async function extractWishlistUrl(url: string): Promise<WishlistExtractionResult> {
  const { data } = await api.post<WishlistExtractionResult>(
    '/api/watches/wishlist/extract',
    { url },
  )
  return data
}

// Every field PUT /api/watches/{id} accepts. Typed as a record over UpdateWatch's
// keys, so adding a field to UpdateWatch fails the build until it is listed here.
// The endpoint replaces the whole watch, and anything missing from the payload is
// written back as null — which is how eight fields were silently wiped on save
// before this list existed.
const UPDATE_FIELDS: Record<keyof UpdateWatch, true> = {
  brand: true,
  model: true,
  movementType: true,
  caseSizeMm: true,
  bandType: true,
  bandColor: true,
  purchaseDate: true,
  purchasePrice: true,
  acquisitionType: true,
  acquiredFrom: true,
  acquisitionSourceUrl: true,
  notes: true,
  crystalType: true,
  caseShape: true,
  crownType: true,
  calendarType: true,
  countryOfOrigin: true,
  waterResistance: true,
  lugWidthMm: true,
  dialColor: true,
  bezelType: true,
  powerReserveHours: true,
  sku: true,
  serialNumber: true,
  productionYear: true,
  batteryType: true,
  lastBatteryChangedDate: true,
  linkUrl: true,
  linkText: true,
  storageLocation: true,
  isWishList: true,
}

/**
 * Build a complete update payload from a loaded watch, so a change to one field
 * does not blank every other one. Pass `overrides` for the fields being changed.
 */
export function toUpdatePayload(watch: Watch, overrides: Partial<UpdateWatch> = {}): UpdateWatch {
  const payload: Record<string, unknown> = {}
  for (const field of Object.keys(UPDATE_FIELDS)) {
    const value = (watch as unknown as Record<string, unknown>)[field]
    // An empty string fails the server's [Url] check and means "not set" anyway.
    payload[field] = value === '' ? undefined : value
  }
  return { ...payload, ...overrides } as UpdateWatch
}

export async function updateWatch(id: number, watch: UpdateWatch): Promise<Watch> {
  const { data } = await api.put<Watch>(`/api/watches/${id}`, watch)
  return data
}

export async function deleteWatch(id: number): Promise<void> {
  await api.delete(`/api/watches/${id}`)
}

export async function recordWear(id: number): Promise<void> {
  await api.post(`/api/watches/${id}/wear`)
}

export async function retireWatch(id: number): Promise<void> {
  await api.put(`/api/watches/${id}/retire`)
}

export async function unretireWatch(id: number): Promise<void> {
  await api.put(`/api/watches/${id}/unretire`)
}

export async function setWatchDisposition(id: number, disposition: UpdateWatchDisposition): Promise<Watch> {
  const { data } = await api.put<Watch>(`/api/watches/${id}/disposition`, disposition)
  return data
}

export async function clearWatchDisposition(id: number): Promise<Watch> {
  const { data } = await api.delete<Watch>(`/api/watches/${id}/disposition`)
  return data
}

export async function reorderWishlist(watchIds: number[]): Promise<void> {
  await api.put('/api/watches/wishlist/order', { watchIds })
}

export async function uploadImage(watchId: number, file: File): Promise<void> {
  const form = new FormData()
  form.append('files', file)
  await api.post(`/api/watches/${watchId}/images`, form)
}

export async function importImageFromUrl(watchId: number, url: string): Promise<void> {
  await api.post(`/api/watches/${watchId}/images/import-url`, { url })
}

export async function deleteImage(watchId: number, imageId: number): Promise<void> {
  await api.delete(`/api/watches/${watchId}/images/${imageId}`)
}

export async function removeBackground(watchId: number, imageId: number): Promise<void> {
  await api.post(`/api/watches/${watchId}/images/${imageId}/remove-background`)
}

/** Describes the watch and proposes values for its empty fields. Writes only the description. */
export async function analyzeWatch(watchId: number): Promise<WatchAnalysisResult> {
  const { data } = await api.post<WatchAnalysisResult>(`/api/watches/${watchId}/analyze`)
  return data
}

/** Writes the suggested values the owner ticked. */
export async function applyAnalysisSuggestions(
  watchId: number,
  values: Record<string, string>,
): Promise<ApplyAnalysisResult> {
  const { data } = await api.post<ApplyAnalysisResult>(`/api/watches/${watchId}/analyze/apply`, { values })
  return data
}

export async function getWearLogs(): Promise<WearLog[]> {
  const { data } = await api.get<WearLog[]>('/api/watches/wear-logs')
  return data
}

export async function deleteWearLog(logId: number): Promise<void> {
  await api.delete(`/api/watches/wear-logs/${logId}`)
}

export async function updateWearLogDate(logId: number, wornDate: string, startedAt?: string, endedAt?: string): Promise<void> {
  await api.put(`/api/watches/wear-logs/${logId}`, { wornDate, startedAt, endedAt })
}

export async function getResaleHistory(watchId: number): Promise<ResaleValueEntry[]> {
  const { data } = await api.get<ResaleValueEntry[]>(`/api/watches/${watchId}/resale-history`)
  return data
}

export async function addManualResaleValue(watchId: number, entry: CreateResaleValueEntry): Promise<Watch> {
  const { data } = await api.post<Watch>(`/api/watches/${watchId}/resale-value`, entry)
  return data
}

export async function deleteResaleValueEntry(entryId: number): Promise<void> {
  await api.delete(`/api/watches/resale-history/${entryId}`)
}

export async function refreshResaleValue(watchId: number): Promise<Watch> {
  const { data } = await api.post<Watch>(`/api/watches/${watchId}/resale-value/refresh`)
  return data
}
