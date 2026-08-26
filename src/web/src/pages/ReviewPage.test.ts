import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReviewPage from './ReviewPage.vue'
import type { CollectionReviewState, CollectionSetStats } from '@/types'

const reviewApi = vi.hoisted(() => ({
  getCollectionReview: vi.fn(),
  generateCollectionReview: vi.fn(),
}))

vi.mock('@/services/review', () => reviewApi)
vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({ isAdmin: true }),
}))

function set(label: string, watchCount: number): CollectionSetStats {
  return {
    label,
    watchCount,
    dataCompletenessPercent: 60,
    coverage: [
      {
        dimension: 'Movement',
        values: [{ value: 'Automatic', count: watchCount, watchIds: [1] }],
      },
    ],
    redundancies: [],
    gaps: [],
  }
}

const reviewed: CollectionReviewState = {
  configured: true,
  review: {
    summary: 'A focused collection with one clear gap.',
    strengths: [
      { summary: 'Genuine tool diver', detail: 'The SKX earns its place.', watchIds: [1] },
    ],
    weaknesses: [],
    recommendations: [
      { summary: 'Nothing dressy', detail: 'No leather under 38mm.', watchIds: [] },
    ],
    facts: {
      collection: set('Collection', 2),
      wishlist: set('Wish list', 1),
      combined: set('Combined', 3),
      dataQuality: [],
      wishlistOverlaps: [],
      wishlistFit: [
        { watchId: 3, totalScore: 79, collectionFitScore: 75, reasons: ['Adds a dress watch.'] },
        { watchId: 4, totalScore: 91, collectionFitScore: 88, reasons: ['Fills the 36mm gap.'] },
      ],
      collectionWatches: [
        {
          id: 1,
          brand: 'Seiko',
          model: 'SKX007',
          movementType: 'Automatic',
        },
      ],
      wishlistWatches: [
        { id: 3, brand: 'Omega', model: 'Speedmaster', movementType: 'Manual' },
        { id: 4, brand: 'Tudor', model: 'Black Bay 36', movementType: 'Automatic' },
      ],
      underusedWatchIds: [],
    },
    generatedAt: '2026-08-26T12:00:00Z',
    isStale: false,
  },
}

describe('ReviewPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    reviewApi.getCollectionReview.mockResolvedValue({ configured: true, review: null })
    reviewApi.generateCollectionReview.mockResolvedValue(reviewed)
  })

  it('offers to generate when no review has been run', async () => {
    const wrapper = mount(ReviewPage, { global: { stubs: { RouterLink: true, AppIcon: true } } })
    await flushPromises()

    expect(wrapper.text()).toContain('No review yet')
    expect(wrapper.find('button').attributes('disabled')).toBeUndefined()
  })

  it('renders findings, the watches they cite, and the counted facts', async () => {
    reviewApi.getCollectionReview.mockResolvedValue(reviewed)
    const wrapper = mount(ReviewPage, {
      global: { stubs: { RouterLink: { template: '<a><slot /></a>' }, AppIcon: true } },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('A focused collection with one clear gap.')
    expect(wrapper.text()).toContain('Genuine tool diver')
    // Cited ids render as the watch they name, not as a bare number.
    expect(wrapper.text()).toContain('Seiko SKX007')
    expect(wrapper.text()).toContain('Nothing to report here.')
    expect(wrapper.text()).toContain('Collection breakdown')
  })

  it('ranks wanted watches by fit, best first', async () => {
    reviewApi.getCollectionReview.mockResolvedValue(reviewed)
    const wrapper = mount(ReviewPage, {
      global: { stubs: { RouterLink: { template: '<a><slot /></a>' }, AppIcon: true } },
    })
    await flushPromises()

    const fitText = wrapper.text()
    expect(fitText.indexOf('Tudor Black Bay 36')).toBeLessThan(fitText.indexOf('Omega Speedmaster'))
  })

  it('explains an unconfigured Ollama before anything is clicked', async () => {
    reviewApi.getCollectionReview.mockResolvedValue({
      configured: false,
      configurationHint: 'The collection review needs Ollama. Set the Ollama URL and model under Admin -> Settings.',
      review: null,
    })
    const wrapper = mount(ReviewPage, { global: { stubs: { RouterLink: true, AppIcon: true } } })
    await flushPromises()

    expect(wrapper.text()).toContain('Set the Ollama URL and model')
    expect(wrapper.find('button').attributes('disabled')).toBeDefined()
    expect(reviewApi.generateCollectionReview).not.toHaveBeenCalled()
  })

  it('surfaces the server message when a generate is refused', async () => {
    reviewApi.generateCollectionReview.mockRejectedValue({
      isAxiosError: true,
      response: { status: 400, data: { error: 'Add at least 2 watches to your collection or wish list before running a review.' } },
    })
    const wrapper = mount(ReviewPage, { global: { stubs: { RouterLink: true, AppIcon: true } } })
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Add at least 2 watches')
  })
})
