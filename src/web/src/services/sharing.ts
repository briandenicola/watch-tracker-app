import { api } from './api'
import type { SharedWatch, WatchShare } from '@/types'

/** The absolute link to hand out, built from whatever origin the app is on. */
export function shareUrl(share: WatchShare): string {
  return `${window.location.origin}${share.path}`
}

/** The watch's existing link, or null when it has never been shared. */
export async function getWatchShare(watchId: number): Promise<WatchShare | null> {
  try {
    const { data } = await api.get<WatchShare>(`/api/watches/${watchId}/share`)
    return data
  } catch (error) {
    if ((error as { response?: { status?: number } })?.response?.status === 404) return null
    throw error
  }
}

export async function createWatchShare(watchId: number): Promise<WatchShare> {
  const { data } = await api.post<WatchShare>(`/api/watches/${watchId}/share`)
  return data
}

export async function revokeWatchShare(watchId: number): Promise<void> {
  await api.delete(`/api/watches/${watchId}/share`)
}

/** The public read. Needs no account, and 404s once the link is revoked. */
export async function getSharedWatch(token: string): Promise<SharedWatch> {
  const { data } = await api.get<SharedWatch>(`/api/shared/${encodeURIComponent(token)}`)
  return data
}
