import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import WatchDetailPage from './WatchDetailPage.vue'
import type { Watch } from '@/types'

const watchApi = vi.hoisted(() => ({
  getWatch: vi.fn(),
  getWatches: vi.fn(),
  imageUrl: (path: string) => path,
  recordWear: vi.fn(),
  deleteWatch: vi.fn(),
  uploadImage: vi.fn(),
  deleteImage: vi.fn(),
  removeBackground: vi.fn(),
  analyzeWatch: vi.fn(),
  updateWatch: vi.fn(),
  toUpdatePayload: vi.fn(),
  getResaleHistory: vi.fn(),
  addManualResaleValue: vi.fn(),
  deleteResaleValueEntry: vi.fn(),
  refreshResaleValue: vi.fn(),
  setWatchDisposition: vi.fn(),
  clearWatchDisposition: vi.fn(),
}))

vi.mock('@/services/watches', () => watchApi)
vi.mock('@/services/api', () => ({ api: { get: vi.fn() } }))
vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: '12' }, query: {}, path: '/watches/12' }),
  useRouter: () => ({ push: vi.fn() }),
  RouterLink: { template: '<a><slot /></a>' },
}))

const wishlistWatch: Watch = {
  id: 12,
  brand: 'Hamilton',
  model: 'Khaki Field',
  movementType: 'Unknown',
  acquisitionType: 'New',
  timesWorn: 0,
  imageUrls: [{ id: 5, url: '/uploads/watch.jpg' }],
  isWishList: true,
  isRetired: false,
  createdAt: '2026-08-26T12:00:00Z',
  updatedAt: '2026-08-26T12:00:00Z',
}

describe('wishlist AI analysis', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.stubGlobal('ResizeObserver', class {
      observe() {}
      disconnect() {}
    })
    watchApi.getWatch.mockResolvedValue(structuredClone(wishlistWatch))
    watchApi.getWatches.mockResolvedValue([structuredClone(wishlistWatch)])
    watchApi.analyzeWatch.mockResolvedValue({
      summary: 'A field watch.',
      suggestions: [],
      sources: [],
    })
  })

  it('offers and runs AI Analyze for a wishlist item with an image', async () => {
    const wrapper = mount(WatchDetailPage, {
      global: {
        stubs: {
          RouterLink: { template: '<a><slot /></a>' },
        },
      },
    })
    await flushPromises()

    await wrapper.get('button[aria-label="Watch actions"]').trigger('click')
    const analyze = wrapper.findAll('button')
      .find(button => button.text() === 'AI Analyze')
    expect(analyze).toBeDefined()

    await analyze!.trigger('click')
    await flushPromises()

    expect(watchApi.analyzeWatch).toHaveBeenCalledWith(12)
  })
})
