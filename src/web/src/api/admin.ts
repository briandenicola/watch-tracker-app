import client from './client';
import type { UserDto, AppSettingDto } from '../types';

export async function getUsers(): Promise<UserDto[]> {
  const { data } = await client.get<UserDto[]>('/admin/users');
  return data;
}

export async function unlockUser(id: number): Promise<void> {
  await client.post(`/admin/users/${id}/unlock`);
}

export async function resetUserPassword(id: number, newPassword: string): Promise<void> {
  await client.post(`/admin/users/${id}/reset-password`, { newPassword });
}

export async function getSettings(): Promise<Record<string, string>> {
  const { data } = await client.get<Record<string, string>>('/admin/settings');
  return data;
}

export async function updateSettings(settings: AppSettingDto[]): Promise<void> {
  await client.put('/admin/settings', settings);
}

export async function getOllamaModels(url: string): Promise<string[]> {
  const { data } = await client.post<string[]>('/admin/ollama/models', { url });
  return data;
}
