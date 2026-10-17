import { api } from './api'

export interface WatchPatrolStatus {
  connected: boolean
}

export async function getWatchPatrolStatus(): Promise<WatchPatrolStatus> {
  const { data } = await api.get<WatchPatrolStatus>('/api/advisor/watchpatrol')
  return data
}

export async function connectWatchPatrol(redirectUri: string): Promise<string> {
  const { data } = await api.post<{ url: string }>('/api/advisor/watchpatrol/connect', { redirectUri })
  return data.url
}

export async function completeWatchPatrol(code: string, state: string): Promise<WatchPatrolStatus> {
  const { data } = await api.post<WatchPatrolStatus>('/api/advisor/watchpatrol/complete', { code, state })
  return data
}

export async function disconnectWatchPatrol(): Promise<void> {
  await api.delete('/api/advisor/watchpatrol')
}
