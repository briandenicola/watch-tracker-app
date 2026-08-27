import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import PriceWatchPanel from './PriceWatchPanel.vue'
import type { Watch } from '@/types'

const watchesApi = vi.hoisted(() => ({
  getPriceObservations: vi.fn(),
  scanWishlistPrice: vi.fn(),
  updatePriceMonitoring: vi.fn(),
}))

vi.mock('@/services/watches', () => watchesApi)

const watch: Watch = {
  id: 22,
  brand: 'Omega',
  model: 'Speedmaster',
  movementType: 'Manual',
  acquisitionType: 'Used',
  timesWorn: 0,
  imageUrls: [],
  purchasePrice: 6000,
  isWishList: true,
  priceAlertEnabled: false,
  isRetired: false,
  createdAt: '2026-08-27T12:00:00Z',
  updatedAt: '2026-08-27T12:00:00Z',
}

describe('PriceWatchPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    watchesApi.getPriceObservations.mockResolvedValue([])
    watchesApi.updatePriceMonitoring.mockResolvedValue({
      priceAlertEnabled: true,
      priceAlertTarget: 5000,
      priceCheckedAt: undefined,
    })
  })

  it('saves opt-in monitoring and exposes honest per-source scan statuses', async () => {
    watchesApi.scanWishlistPrice.mockResolvedValue({
      watchId: 22,
      checkedAt: '2026-08-27T12:00:00Z',
      observationsAdded: 1,
      alertsCreated: 0,
      sources: [
        {
          source: 'Ashford',
          status: 'Found',
          listings: [{
            id: 3,
            source: 'Ashford',
            listingUrl: 'https://ashford.example.test/speedmaster',
            listingTitle: 'Omega Speedmaster',
            price: 4500,
            currency: 'USD',
            kind: 'New',
            matchConfidence: 'High',
            observedAt: '2026-08-27T12:00:00Z',
          }],
        },
        { source: 'Chrono24', status: 'NotConfigured', error: 'Search is not configured.', listings: [] },
        { source: 'Bezel', status: 'Blocked', error: 'Not scanned in v1.', listings: [] },
      ],
    })
    const wrapper = mount(PriceWatchPanel, { props: { watch } })
    await flushPromises()

    await wrapper.get('input[type="checkbox"]').setValue(true)
    await wrapper.get('input[type="number"]').setValue('5000')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(watchesApi.updatePriceMonitoring).toHaveBeenCalledWith(22, {
      priceAlertEnabled: true,
      priceAlertTarget: 5000,
    })
    expect(wrapper.emitted('updated')?.[0]).toEqual([{
      priceAlertEnabled: true,
      priceAlertTarget: 5000,
      priceCheckedAt: undefined,
    }])

    await wrapper.get('button').trigger('click')
    await flushPromises()

    expect(watchesApi.scanWishlistPrice).toHaveBeenCalledWith(22)
    expect(wrapper.text()).toContain('Ashford: Found')
    expect(wrapper.text()).toContain('Chrono24: NotConfigured')
    expect(wrapper.text()).toContain('Bezel: Blocked')
    expect(wrapper.text()).toContain('Omega Speedmaster — $4,500.00')
  })
})
