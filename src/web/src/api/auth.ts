import client from './client';
import type { AuthResponse, LoginCredentials, RegisterCredentials } from '../types';

export async function login(credentials: LoginCredentials): Promise<AuthResponse> {
  const { data } = await client.post<AuthResponse>('/auth/login', credentials);
  localStorage.setItem('token', data.token);
  return data;
}

export async function register(credentials: RegisterCredentials): Promise<AuthResponse> {
  const { data } = await client.post<AuthResponse>('/auth/register', credentials);
  localStorage.setItem('token', data.token);
  return data;
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  await client.post('/auth/change-password', { currentPassword, newPassword });
}

export async function getProfile(): Promise<AuthResponse> {
  const { data } = await client.get<AuthResponse>('/auth/me');
  return data;
}

export async function updateUsername(username: string): Promise<void> {
  await client.put('/auth/username', { username });
}

export async function uploadProfileImage(file: File): Promise<string> {
  const form = new FormData();
  form.append('file', file);
  const { data } = await client.post<{ profileImage: string }>('/auth/profile-image', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data.profileImage;
}

export async function deleteProfileImage(): Promise<void> {
  await client.delete('/auth/profile-image');
}
