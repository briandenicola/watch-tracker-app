import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import NotificationsPage from './NotificationsPage.vue'

const watchesApi = vi.hoisted(() => ({
  getPriceAlerts: vi.fn(),
  markAllPriceAlertsRead: vi.fn(),
  markPriceAlertRead: vi.fn(),
}))

vi.mock('@/services/watches', () => watchesApi)

function mountPage() {
  const pinia = createPinia()
  setActivePinia(pinia)
  return mount(NotificationsPage, {
    global: {
      plugins: [pinia],
      stubs: { RouterLink: { template: '<a><slot /></a>' } },
    },
  })
}

describe('NotificationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    watchesApi.getPriceAlerts.mockResolvedValue([
      {
        id: 9,
        watchId: 22,
        watchBrand: 'Omega',
        watchModel: 'Speedmaster',
        trigger: 'BelowTarget',
        isRead: false,
        createdAt: '2026-08-27T12:00:00Z',
        observation: {
          id: 3,
          source: 'Ashford',
          listingUrl: 'https://ashford.example.test/speedmaster',
          listingTitle: 'Omega Speedmaster',
          price: 4500,
          currency: 'USD',
          kind: 'New',
          matchConfidence: 'High',
          observedAt: '2026-08-27T12:00:00Z',
        },
      },
      {
        id: 10,
        watchId: 23,
        watchBrand: 'Seiko',
        watchModel: 'Alpinist',
        trigger: 'NewBest',
        isRead: true,
        readAt: '2026-08-27T12:05:00Z',
        createdAt: '2026-08-27T12:00:00Z',
        observation: {
          id: 4,
          source: 'eBay',
          listingUrl: 'https://ebay.example.test/alpinist',
          listingTitle: 'Seiko Alpinist',
          price: 600,
          currency: 'USD',
          kind: 'Preowned',
          matchConfidence: 'High',
          observedAt: '2026-08-27T12:00:00Z',
        },
      },
    ])
    watchesApi.markPriceAlertRead.mockResolvedValue(undefined)
    watchesApi.markAllPriceAlertsRead.mockResolvedValue(undefined)
  })

  it('renders price alerts and acknowledges one unread notification', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('h2').text()).toBe('Notifications')
    expect(wrapper.findAll('[data-testid="notification-card"]')).toHaveLength(2)
    expect(wrapper.text()).toContain('Price below your target')
    expect(wrapper.text()).toContain('Omega Speedmaster is available for $4,500.00 at Ashford.')
    expect(wrapper.find('[data-testid="mark-all-read"]').exists()).toBe(true)

    await wrapper.get('[data-testid="mark-read-9"]').trigger('click')
    await flushPromises()

    expect(watchesApi.markPriceAlertRead).toHaveBeenCalledWith(9)
    expect(wrapper.find('[data-testid="mark-read-9"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('Read')
  })

  it('marks all unread notifications read with the bulk endpoint', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.get('[data-testid="mark-all-read"]').trigger('click')
    await flushPromises()

    expect(watchesApi.markAllPriceAlertsRead).toHaveBeenCalledTimes(1)
    expect(wrapper.find('[data-testid="mark-all-read"]').exists()).toBe(false)
    expect(wrapper.findAll('[data-testid^="mark-read-"]')).toHaveLength(0)
  })
})
