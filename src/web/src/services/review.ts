import { api } from './api'
import type { CollectionReviewState } from '@/types'

export async function getCollectionReview(): Promise<CollectionReviewState> {
  const { data } = await api.get<CollectionReviewState>('/api/collection/review')
  return data
}

export async function generateCollectionReview(): Promise<CollectionReviewState> {
  const { data } = await api.post<CollectionReviewState>('/api/collection/review')
  return data
}
