import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AdvisorPage from './AdvisorPage.vue'
import type { AdvisorChatState } from '@/types'

const advisorApi = vi.hoisted(() => ({
  getAdvisorChat: vi.fn(),
  startAdvisorSession: vi.fn(),
  sendAdvisorMessage: vi.fn(),
  saveAdvisorFeedback: vi.fn(),
  removeAdvisorFeedback: vi.fn(),
  addAdvisorRecommendationToWishlist: vi.fn(),
}))

vi.mock('@/services/advisor', () => advisorApi)
vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({ isAdmin: true }),
}))

const chatState: AdvisorChatState = {
  configured: true,
  session: {
    id: 3,
    createdAt: '2026-08-21T12:00:00Z',
    updatedAt: '2026-08-21T12:00:00Z',
    messages: [
      {
        id: 9,
        role: 'Assistant',
        content: 'A grounded recommendation.',
        citations: [],
        followUps: [],
        toolActivity: [],
        createdAt: '2026-08-21T12:00:00Z',
        recommendationCards: [
          {
            provider: 'eBay',
            providerItemId: 'item-1',
            title: 'Hamilton Khaki Field',
            itemUrl: 'https://example.test/item-1',
            price: 995,
            currency: 'USD',
            reasons: ['Fills a field-watch gap.'],
          },
        ],
      },
    ],
  },
}

describe('Advisor recommendation actions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    advisorApi.getAdvisorChat.mockResolvedValue(structuredClone(chatState))
  })

  it('adds the persisted recommendation identity to the wishlist', async () => {
    advisorApi.addAdvisorRecommendationToWishlist.mockResolvedValue({
      added: true,
      watchId: 12,
      message: 'Added to your wishlist.',
    })
    const wrapper = mount(AdvisorPage)
    await flushPromises()

    await findButton(wrapper, 'Add to wishlist').trigger('click')
    await flushPromises()

    expect(advisorApi.addAdvisorRecommendationToWishlist)
      .toHaveBeenCalledWith(9, 'eBay', 'item-1')
    expect(wrapper.text()).toContain('Added to your wishlist.')
  })

  it('saves, updates, and clears recommendation feedback', async () => {
    advisorApi.saveAdvisorFeedback.mockResolvedValue({
      id: 44,
      kind: 'Helpful',
      notes: null,
      updatedAt: '2026-08-21T12:01:00Z',
    })
    advisorApi.removeAdvisorFeedback.mockResolvedValue(undefined)
    const wrapper = mount(AdvisorPage)
    await flushPromises()

    await findButton(wrapper, 'Helpful').trigger('click')
    await flushPromises()

    expect(advisorApi.saveAdvisorFeedback)
      .toHaveBeenCalledWith(9, 'eBay', 'item-1', 'Helpful', '')
    expect(wrapper.text()).toContain('Feedback saved for future recommendations.')

    await findButton(wrapper, 'Clear').trigger('click')
    await flushPromises()

    expect(advisorApi.removeAdvisorFeedback).toHaveBeenCalledWith(44)
    expect(wrapper.text()).toContain('Feedback cleared.')
  })
})

function findButton(
  wrapper: ReturnType<typeof mount>,
  label: string,
) {
  const button = wrapper.findAll('button').find(candidate => candidate.text() === label)
  if (!button) throw new Error(`Button not found: ${label}`)
  return button
}
