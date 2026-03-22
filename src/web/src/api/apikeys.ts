import client from './client';

export interface ApiKeyDto {
  id: number;
  name: string;
  createdAt: string;
  lastUsedAt: string | null;
}

export interface ApiKeyCreatedDto {
  id: number;
  name: string;
  key: string;
  createdAt: string;
}

export async function getApiKeys(): Promise<ApiKeyDto[]> {
  const { data } = await client.get<ApiKeyDto[]>('/apikeys');
  return data;
}

export async function createApiKey(name: string): Promise<ApiKeyCreatedDto> {
  const { data } = await client.post<ApiKeyCreatedDto>('/apikeys', { name });
  return data;
}

export async function deleteApiKey(id: number): Promise<void> {
  await client.delete(`/apikeys/${id}`);
}
