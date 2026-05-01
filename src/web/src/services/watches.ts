import { api } from './api'
import type { Watch, CreateWatch, UpdateWatch, WearLog } from '@/types'

const BASE_URL = window.location.origin

export function imageUrl(path: string): string {
  if (path.startsWith('http')) return path
  return `${BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`
}

export async function getWatches(): Promise<Watch[]> {
  const { data } = await api.get<Watch[]>('/api/watches')
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

export async function updateWatch(id: number, watch: UpdateWatch): Promise<void> {
  await api.put(`/api/watches/${id}`, watch)
}

export async function deleteWatch(id: number): Promise<void> {
  await api.delete(`/api/watches/${id}`)
}

export async function recordWear(id: number): Promise<void> {
  await api.post(`/api/watches/${id}/wear`)
}

export async function retireWatch(id: number): Promise<void> {
  await api.post(`/api/watches/${id}/retire`)
}

export async function unretireWatch(id: number): Promise<void> {
  await api.post(`/api/watches/${id}/unretire`)
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
  await api.post(`/api/watchimages/${imageId}/remove-background`)
}

export async function analyzeWatch(watchId: number, imageId: number): Promise<string> {
  const { data } = await api.post<{ analysis: string }>(`/api/watchimages/${imageId}/analyze`)
  return data.analysis
}

export async function getWearLogs(): Promise<WearLog[]> {
  const { data } = await api.get<WearLog[]>('/api/watches/wear-logs')
  return data
}
