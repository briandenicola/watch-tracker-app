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
