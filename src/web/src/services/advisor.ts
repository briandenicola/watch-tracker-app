import { api } from './api'
import type {
  AdvisorChatState,
  AdvisorFeedbackKind,
  AdvisorRecommendationFeedback,
  AdvisorWishlistActionResult,
} from '@/types'

export async function getAdvisorChat(): Promise<AdvisorChatState> {
  const { data } = await api.get<AdvisorChatState>('/api/advisor')
  return data
}

export async function startAdvisorSession(): Promise<AdvisorChatState> {
  const { data } = await api.post<AdvisorChatState>('/api/advisor/sessions')
  return data
}

export async function sendAdvisorMessage(
  sessionId: number,
  message: string,
): Promise<AdvisorChatState> {
  const { data } = await api.post<AdvisorChatState>(
    `/api/advisor/sessions/${sessionId}/messages`,
    { message },
  )
  return data
}

export async function saveAdvisorFeedback(
  messageId: number,
  provider: string,
  providerItemId: string,
  kind: AdvisorFeedbackKind,
  notes?: string,
): Promise<AdvisorRecommendationFeedback> {
  const { data } = await api.put<AdvisorRecommendationFeedback>(
    `/api/advisor/messages/${messageId}/feedback`,
    { provider, providerItemId, kind, notes: notes || null },
  )
  return data
}

export async function removeAdvisorFeedback(feedbackId: number): Promise<void> {
  await api.delete(`/api/advisor/feedback/${feedbackId}`)
}

export async function addAdvisorRecommendationToWishlist(
  messageId: number,
  provider: string,
  providerItemId: string,
): Promise<AdvisorWishlistActionResult> {
  const { data } = await api.post<AdvisorWishlistActionResult>(
    `/api/advisor/messages/${messageId}/wishlist`,
    { provider, providerItemId },
  )
  return data
}
