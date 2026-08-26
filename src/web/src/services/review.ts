import { api } from './api'
import type {
  AdvisorWishlistActionResult,
  CollectionReviewCandidates,
  CollectionReviewState,
} from '@/types'

export async function getCollectionReview(): Promise<CollectionReviewState> {
  const { data } = await api.get<CollectionReviewState>('/api/collection/review')
  return data
}

export async function generateCollectionReview(): Promise<CollectionReviewState> {
  const { data } = await api.post<CollectionReviewState>('/api/collection/review')
  return data
}

export async function generateCandidates(
  budget?: number,
  currency?: string,
): Promise<CollectionReviewCandidates> {
  const { data } = await api.post<CollectionReviewCandidates>(
    '/api/collection/review/candidates',
    { budget: budget ?? null, currency: currency ?? null },
  )
  return data
}

export async function addCandidateToWishlist(
  provider: string,
  providerItemId: string,
): Promise<AdvisorWishlistActionResult> {
  const { data } = await api.post<AdvisorWishlistActionResult>(
    '/api/collection/review/candidates/wishlist',
    { provider, providerItemId },
  )
  return data
}
