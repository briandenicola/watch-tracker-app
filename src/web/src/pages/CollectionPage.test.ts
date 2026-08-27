import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CollectionPage from './CollectionPage.vue'
import type { Watch } from '@/types'

const watchApi = vi.hoisted(() => ({
  getWatches: vi.fn(),
  imageUrl: (path: string) => path,
  reorderWishlist: vi.fn(),
}))

vi.mock('@/services/watches', () => watchApi)
vi.mock('vue-router', () => ({
  useRoute: () => ({ query: {} }),
  RouterLink: { template: '<a><slot /></a>' },
}))

const STORAGE_KEY = 'watch-tracker-preferences'

function mountPage() {
  return mount(CollectionPage, {
    global: { stubs: { RouterLink: { template: '<a><slot /></a>' } } },
  })
}

function watch(overrides: Partial<Watch> & Pick<Watch, 'id' | 'brand' | 'model'>): Watch {
  return {
    movementType: 'Automatic',
    acquisitionType: 'New',
    timesWorn: 0,
    imageUrls: [],
    isWishList: false,
    priceAlertEnabled: false,
    isRetired: false,
    createdAt: '2026-08-01T12:00:00Z',
    updatedAt: '2026-08-01T12:00:00Z',
    ...overrides,
  }
}

describe('collection view mode', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    watchApi.getWatches.mockResolvedValue([
      watch({ id: 1, brand: 'Seiko', model: 'SPB143', imageUrls: [{ id: 9, url: '/uploads/a.jpg' }] }),
      watch({ id: 2, brand: 'Omega', model: 'Speedmaster' }),
    ])
  })

  it('switches to the compact grid and remembers the choice', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="view-cards"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.findAll('[data-testid="compact-tile"]')).toHaveLength(0)

    await wrapper.get('[data-testid="view-compact"]').trigger('click')
    await flushPromises()

    const tiles = wrapper.findAll('[data-testid="compact-tile"]')
    expect(tiles).toHaveLength(2)
    expect(tiles[0].text()).toContain('Seiko')
    // Thumbnails stay lazy, and a watch with no image keeps the placeholder.
    expect(tiles[0].get('img').attributes('loading')).toBe('lazy')
    expect(tiles[1].find('img').exists()).toBe(false)
    expect(tiles[1].text()).toContain('⌚')

    expect(wrapper.get('[data-testid="view-compact"]').attributes('aria-pressed')).toBe('true')
    expect(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '{}').collectionViewMode)
      .toBe('compact')

    // Back to cards, and that choice is remembered too.
    await wrapper.get('[data-testid="view-cards"]').trigger('click')
    await flushPromises()

    expect(wrapper.findAll('[data-testid="compact-tile"]')).toHaveLength(0)
    expect(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '{}').collectionViewMode)
      .toBe('cards')
  })

  it('applies the active filters to the compact grid', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.get('[data-testid="view-compact"]').trigger('click')
    await flushPromises()

    const filterToggle = wrapper.findAll('button').find(button => button.text() === '2')
    if (!filterToggle) throw new Error('Filter toggle not found')
    await filterToggle.trigger('click')

    const brandFilter = wrapper.findAll('select')
      .find(select => select.findAll('option').some(option => option.text() === 'Omega'))
    if (!brandFilter) throw new Error('Brand filter not found')
    await brandFilter.setValue('Omega')

    const tiles = wrapper.findAll('[data-testid="compact-tile"]')
    expect(tiles).toHaveLength(1)
    expect(tiles[0].text()).toContain('Omega')
  })
})
