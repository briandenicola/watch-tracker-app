import { api } from './api'
import type { WatchRecommendation, WatchRecommendationRequest } from '@/types'

export async function recommendWatch(
  outfit: WatchRecommendationRequest,
): Promise<WatchRecommendation> {
  const { data } = await api.post<WatchRecommendation>('/api/recommendations/outfit', outfit)
  return data
}
