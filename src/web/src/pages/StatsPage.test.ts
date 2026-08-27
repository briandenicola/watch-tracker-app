import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import StatsPage from './StatsPage.vue'
import type { Watch } from '@/types'

const watchApi = vi.hoisted(() => ({
  getWatches: vi.fn(),
  getWearLogs: vi.fn(),
  imageUrl: (path: string) => path,
}))

vi.mock('@/services/watches', () => watchApi)
vi.mock('vue-router', () => ({
  RouterLink: { template: '<a><slot /></a>' },
}))

function watch(overrides: Partial<Watch> & Pick<Watch, 'id' | 'brand' | 'model'>): Watch {
  return {
    movementType: 'Automatic',
    acquisitionType: 'New',
    timesWorn: 0,
    imageUrls: [],
    isWishList: false,
    isRetired: false,
    createdAt: '2026-08-01T12:00:00Z',
    updatedAt: '2026-08-01T12:00:00Z',
    ...overrides,
  }
}

describe('statistics', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    watchApi.getWatches.mockResolvedValue([
      watch({ id: 1, brand: 'Seiko', model: 'SPB143', purchasePrice: 100, timesWorn: 1, imageUrls: [{ id: 1, url: '/uploads/seiko.jpg' }] }),
      watch({ id: 2, brand: 'Omega', model: 'Speedmaster', purchasePrice: 100, timesWorn: 100 }),
    ])
    watchApi.getWearLogs.mockResolvedValue([])
  })

  it('uses lifetime totals for cost per wear and describes watch photos', async () => {
    const wrapper = mount(StatsPage, {
      global: {
        stubs: {
          RouterLink: { template: '<a><slot /></a>' },
        },
      },
    })
    await flushPromises()

    const costLabel = wrapper.findAll('p').find(element => element.text() === 'Lifetime Avg Cost / Wear')
    expect(costLabel?.element.previousElementSibling?.textContent).toBe('$2')
    expect(wrapper.get('img').attributes('alt')).toBe('Seiko SPB143')
  })
})
