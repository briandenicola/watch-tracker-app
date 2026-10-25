import { api } from './api'
import type { Label, SaveLabel, SavedView, SaveSavedView } from '@/types'

export async function getLabels(): Promise<Label[]> {
  const { data } = await api.get<Label[]>('/api/labels')
  return data
}

export async function createLabel(label: SaveLabel): Promise<Label> {
  const { data } = await api.post<Label>('/api/labels', label)
  return data
}

export async function updateLabel(id: number, label: SaveLabel): Promise<Label> {
  const { data } = await api.put<Label>(`/api/labels/${id}`, label)
  return data
}

export async function deleteLabel(id: number): Promise<void> {
  await api.delete(`/api/labels/${id}`)
}

export async function setWatchLabels(watchId: number, labelIds: number[]): Promise<number[]> {
  const { data } = await api.put<number[]>(`/api/watches/${watchId}/labels`, { labelIds })
  return data
}

export async function getSavedViews(): Promise<SavedView[]> {
  const { data } = await api.get<SavedView[]>('/api/saved-views')
  return data
}

export async function createSavedView(view: SaveSavedView): Promise<SavedView> {
  const { data } = await api.post<SavedView>('/api/saved-views', view)
  return data
}

export async function updateSavedView(id: number, view: SaveSavedView): Promise<SavedView> {
  const { data } = await api.put<SavedView>(`/api/saved-views/${id}`, view)
  return data
}

export async function setSavedViewPinned(id: number, pinned: boolean): Promise<SavedView> {
  const { data } = await api.put<SavedView>(`/api/saved-views/${id}/pin`, { pinned })
  return data
}

export async function deleteSavedView(id: number): Promise<void> {
  await api.delete(`/api/saved-views/${id}`)
}
