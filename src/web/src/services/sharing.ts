import { api } from './api'
import type { SharedWatch, SharedWishlist, WatchShare, WishlistShare } from '@/types'

/**
 * The absolute link to hand out. Prefers the public address an admin configured,
 * since the host the owner administers from may be one their friends cannot
 * reach; falls back to the current origin when nothing is set.
 */
export function shareUrl(share: WatchShare | WishlistShare): string {
  return share.url || `${window.location.origin}${share.path}`
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

// --- Wish list sharing ------------------------------------------------------

/** The user's existing wish list link, or null when they have never shared it. */
export async function getWishlistShare(): Promise<WishlistShare | null> {
  try {
    const { data } = await api.get<WishlistShare>('/api/wishlist/share')
    return data
  } catch (error) {
    if ((error as { response?: { status?: number } })?.response?.status === 404) return null
    throw error
  }
}

export async function createWishlistShare(includePrices: boolean): Promise<WishlistShare> {
  const { data } = await api.post<WishlistShare>('/api/wishlist/share', { includePrices })
  return data
}

/** Changes what the link exposes, without reissuing it. */
export async function updateWishlistShare(includePrices: boolean): Promise<WishlistShare> {
  const { data } = await api.put<WishlistShare>('/api/wishlist/share', { includePrices })
  return data
}

export async function revokeWishlistShare(): Promise<void> {
  await api.delete('/api/wishlist/share')
}

/** The public read. Needs no account, and 404s once the link is revoked. */
export async function getSharedWishlist(token: string): Promise<SharedWishlist> {
  const { data } = await api.get<SharedWishlist>(`/api/shared/wishlist/${encodeURIComponent(token)}`)
  return data
}
