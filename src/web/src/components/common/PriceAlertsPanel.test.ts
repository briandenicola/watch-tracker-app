import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import PriceAlertsPanel from './PriceAlertsPanel.vue'

const watchesApi = vi.hoisted(() => ({
  getPriceAlerts: vi.fn(),
  markPriceAlertRead: vi.fn(),
}))

vi.mock('@/services/watches', () => watchesApi)

describe('PriceAlertsPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    watchesApi.getPriceAlerts.mockResolvedValue([{
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
    }])
    watchesApi.markPriceAlertRead.mockResolvedValue(undefined)
  })

  it('shows the unread badge and acknowledges an alert accessibly', async () => {
    const wrapper = mount(PriceAlertsPanel, {
      global: { stubs: { RouterLink: { template: '<a><slot /></a>' } } },
    })
    await flushPromises()

    expect(wrapper.get('[data-testid="unread-alert-badge"]').text()).toBe('1')
    expect(wrapper.text()).toContain('Below your target: $4,500.00 at Ashford')

    await wrapper.findAll('button').find(button => button.text() === 'Mark as read')!.trigger('click')
    await flushPromises()

    expect(watchesApi.markPriceAlertRead).toHaveBeenCalledWith(9)
    expect(wrapper.find('[data-testid="unread-alert-badge"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('Read')
  })
})
