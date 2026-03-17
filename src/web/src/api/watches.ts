import client from './client';
import type { Watch, CreateWatch, UpdateWatch, WatchImage, WearLog } from '../types';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL !== undefined
  ? import.meta.env.VITE_API_BASE_URL
  : 'http://localhost:5062';

export async function getWatches(): Promise<Watch[]> {
  const { data } = await client.get<Watch[]>('/watches');
  return data;
}

export async function getWatch(id: number): Promise<Watch> {
  const { data } = await client.get<Watch>(`/watches/${id}`);
  return data;
}

export async function createWatch(watch: CreateWatch): Promise<Watch> {
  const { data } = await client.post<Watch>('/watches', watch);
  return data;
}

export async function updateWatch(id: number, watch: UpdateWatch): Promise<void> {
  await client.put(`/watches/${id}`, watch);
}

export async function deleteWatch(id: number): Promise<void> {
  await client.delete(`/watches/${id}`);
}

export async function uploadWatchImages(watchId: number, files: File[]): Promise<WatchImage[]> {
  const form = new FormData();
  files.forEach((f) => form.append('files', f));
  const { data } = await client.post<WatchImage[]>(`/watches/${watchId}/images`, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function deleteWatchImage(watchId: number, imageId: number): Promise<void> {
  await client.delete(`/watches/${watchId}/images/${imageId}`);
}

export async function setCoverImage(watchId: number, imageId: number): Promise<void> {
  await client.put(`/watches/${watchId}/images/${imageId}/cover`);
}

export function imageUrl(path: string): string {
  return `${apiBaseUrl}${path}`;
}

export async function analyzeWatch(id: number): Promise<string> {
  const { data } = await client.post<{ analysis: string }>(`/watches/${id}/analyze`);
  return data.analysis;
}

export async function recordWear(id: number): Promise<Watch> {
  const { data } = await client.post<Watch>(`/watches/${id}/wear`);
  return data;
}

export async function importImageFromUrl(watchId: number, url: string): Promise<WatchImage> {
  const { data } = await client.post<WatchImage>(`/watches/${watchId}/images/import-url`, { url });
  return data;
}

export async function getWearLogs(): Promise<WearLog[]> {
  const { data } = await client.get<WearLog[]>('/watches/wear-logs');
  return data;
}

export async function deleteWearLog(logId: number): Promise<void> {
  await client.delete(`/watches/wear-logs/${logId}`);
}

export async function updateWearLogDate(logId: number, wornDate: string): Promise<void> {
  await client.put(`/watches/wear-logs/${logId}`, { wornDate });
}
