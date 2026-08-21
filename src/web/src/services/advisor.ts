import { api } from './api'
import type { AdvisorChatState } from '@/types'

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
