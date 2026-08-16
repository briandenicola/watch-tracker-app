import { api } from './api'
import type { SendStyleMessage, StyleChatState, StyleRecommendation } from '@/types'

export async function getStyleChat(watchId: number): Promise<StyleChatState> {
  const { data } = await api.get<StyleChatState>(`/api/watches/${watchId}/style`)
  return data
}

export async function sendStyleMessage(watchId: number, payload: SendStyleMessage): Promise<StyleChatState> {
  const { data } = await api.post<StyleChatState>(`/api/watches/${watchId}/style/messages`, payload)
  return data
}

/** Clears the transcript and starts over. Remembered outfits are kept. */
export async function startStyleSession(watchId: number): Promise<StyleChatState> {
  const { data } = await api.post<StyleChatState>(`/api/watches/${watchId}/style/sessions`)
  return data
}

export async function rateStyleRecommendation(
  watchId: number,
  recommendationId: number,
  wasHelpful: boolean,
  notes?: string,
): Promise<StyleRecommendation> {
  const { data } = await api.post<StyleRecommendation>(
    `/api/watches/${watchId}/style/recommendations/${recommendationId}/feedback`,
    { wasHelpful, notes },
  )
  return data
}

export async function forgetStyleRecommendation(watchId: number, recommendationId: number): Promise<void> {
  await api.delete(`/api/watches/${watchId}/style/recommendations/${recommendationId}`)
}
