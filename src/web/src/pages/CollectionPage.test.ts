import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CollectionPage from './CollectionPage.vue'
import type { Label, SavedView, Watch } from '@/types'
import { createPinia, setActivePinia } from 'pinia'

const watchApi = vi.hoisted(() => ({
  getWatches: vi.fn(),
  imageUrl: (path: string) => path,
  reorderWishlist: vi.fn(),
}))
const notifications = vi.hoisted(() => ({
  unreadCount: 2,
}))

const labelApi = vi.hoisted(() => ({
  getLabels: vi.fn(),
  getSavedViews: vi.fn(),
  createSavedView: vi.fn(),
}))
const route = vi.hoisted(() => ({ query: {} as Record<string, string> }))
const router = vi.hoisted(() => ({ replace: vi.fn() }))

vi.mock('@/services/watches', () => watchApi)
vi.mock('@/services/labels', () => labelApi)
vi.mock('@/stores/notifications', () => ({ useNotificationsStore: () => notifications }))
vi.mock('vue-router', () => ({
  useRoute: () => route,
  useRouter: () => router,
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
    setActivePinia(createPinia())
    route.query = {}
    labelApi.getLabels.mockResolvedValue([])
    labelApi.getSavedViews.mockResolvedValue([])
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

    const filterToggle = wrapper.get('button[aria-label="Show filters"]')
    await filterToggle.trigger('click')

    const brandFilter = wrapper.findAll('select')
      .find(select => select.findAll('option').some(option => option.text() === 'Omega'))
    if (!brandFilter) throw new Error('Brand filter not found')
    await brandFilter.setValue('Omega')

    const tiles = wrapper.findAll('[data-testid="compact-tile"]')
    expect(tiles).toHaveLength(1)
    expect(tiles[0].text()).toContain('Omega')
  })

  it('keeps the page-level notifications bell on desktop only', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[role="group"]').attributes('aria-label')).toBe('Collection display controls')
    expect(wrapper.find('button[aria-label="Show filters"]').text()).toBe('')
    const notificationsLink = wrapper.get('a[title="Notifications"]')
    expect(notificationsLink.attributes('aria-label')).toBe('2 unread notifications')
    expect(notificationsLink.classes()).toContain('hidden')
    expect(notificationsLink.classes()).toContain('lg:inline-flex')
  })
})

describe('labels and saved views', () => {
  const diver: Label = { id: 1, name: 'Diver', color: '#38bdf8', watchCount: 2 }
  const grail: Label = { id: 2, name: 'Grail', color: '#c4a35a', watchCount: 2 }

  function view(overrides: Partial<SavedView>): SavedView {
    return { id: 5, name: 'Divers', scope: 'Collection', labelIds: [], labelMatch: 'Any', pinned: false, ...overrides }
  }

  beforeEach(() => {
    vi.clearAllMocks()
    setActivePinia(createPinia())
    localStorage.clear()
    route.query = {}
    labelApi.getLabels.mockResolvedValue([diver, grail])
    labelApi.getSavedViews.mockResolvedValue([])
    watchApi.getWatches.mockResolvedValue([
      watch({ id: 1, brand: 'Seiko', model: 'SKX007', labelIds: [1] }),
      watch({ id: 2, brand: 'Omega', model: 'Seamaster', labelIds: [1, 2] }),
      watch({ id: 3, brand: 'Cartier', model: 'Tank' }),
      watch({ id: 4, brand: 'Rolex', model: 'Submariner', labelIds: [1, 2], isWishList: true, wishlistPriority: 1 }),
      watch({ id: 5, brand: 'Tudor', model: 'Pelagos', labelIds: [1], isWishList: true, wishlistPriority: 0 }),
    ])
  })

  async function mountCards() {
    const wrapper = mountPage()
    await flushPromises()
    // The view mode preference outlives a test, so pick the card view explicitly.
    await wrapper.get('[data-testid="view-cards"]').trigger('click')
    return wrapper
  }

  const models = (wrapper: ReturnType<typeof mountPage>) =>
    wrapper.findAll('p.font-display').map(p => p.text())

  it('shows label chips on cards and filters by label with Any or All', async () => {
    route.query = { label: '1' }
    const wrapper = await mountCards()

    expect(models(wrapper)).toEqual(['Seiko', 'Omega'])
    expect(wrapper.text()).toContain('Grail')

    await wrapper.get('button[aria-label="Show filters"]').trigger('click')
    await wrapper.findAll('[aria-label="Filter by label"] button').find(b => b.text() === 'Grail')!.trigger('click')
    expect(models(wrapper)).toEqual(['Seiko', 'Omega'])

    await wrapper.findAll('[aria-label="Label match"] button').find(b => b.text() === 'All')!.trigger('click')
    expect(models(wrapper)).toEqual(['Omega'])
  })

  it('opens a saved view from the address, including its list and sort', async () => {
    route.query = { view: '5' }
    labelApi.getSavedViews.mockResolvedValue([
      view({ scope: 'Wishlist', labelIds: [1], sortBy: 'priority', pinned: true }),
    ])
    const wrapper = await mountCards()

    expect(wrapper.get('[data-testid="active-view"]').text()).toContain('Divers')
    expect(wrapper.get('[data-testid="active-view"]').text()).not.toContain('changed')
    expect(models(wrapper)).toEqual(['Tudor', 'Rolex'])
  })

  it('saves the current filters as a view and opens it', async () => {
    labelApi.createSavedView.mockResolvedValue(view({ id: 9, name: 'Grails', labelIds: [2] }))
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.get('button[aria-label="Show filters"]').trigger('click')
    await wrapper.findAll('[aria-label="Filter by label"] button').find(b => b.text() === 'Grail')!.trigger('click')
    await wrapper.findAll('button').find(b => b.text() === 'Save as view…')!.trigger('click')
    await wrapper.get('input[aria-label="View name"]').setValue('Grails')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(labelApi.createSavedView).toHaveBeenCalledWith({
      name: 'Grails', pinned: false, scope: 'Collection', labelIds: [2], labelMatch: 'Any',
      brand: null, movementType: null, sortBy: 'dateAdded',
    })
    expect(router.replace).toHaveBeenCalledWith({ path: '/', query: { view: '9' } })
  })
})

