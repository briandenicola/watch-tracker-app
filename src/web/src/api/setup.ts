import client from './client';
import type { AuthResponse } from '../types';

export interface SetupStatus {
  needsSetup: boolean;
}

export interface SetupData {
  username: string;
  email: string;
  password: string;
  aiProvider?: string;
  anthropicApiKey?: string;
  ollamaUrl?: string;
  ollamaModel?: string;
  aiAnalysisPrompt?: string;
}

export async function getSetupStatus(): Promise<SetupStatus> {
  const { data } = await client.get<SetupStatus>('/setup/status');
  return data;
}

export async function completeSetup(setup: SetupData): Promise<AuthResponse> {
  const { data } = await client.post<AuthResponse>('/setup', setup);
  localStorage.setItem('token', data.token);
  return data;
}
