import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import WearLogPage from './WearLogPage.vue'
import { currentDateKey, zonedDateTimeToUtc } from '@/utils/dateTime'
import type { Watch } from '@/types'

const watchApi = vi.hoisted(() => ({
  deleteWearLog: vi.fn(),
  getWatches: vi.fn(),
  getWearLogs: vi.fn(),
  imageUrl: (path: string) => path,
  recordWear: vi.fn(),
  updateWearLogDate: vi.fn(),
}))

vi.mock('@/services/watches', () => watchApi)

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

describe('logging a wear from the calendar', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    watchApi.getWearLogs.mockResolvedValue([])
    watchApi.recordWear.mockResolvedValue(undefined)
    watchApi.getWatches.mockResolvedValue([
      watch({ id: 1, brand: 'Seiko', model: 'SPB143' }),
      watch({ id: 2, brand: 'Omega', model: 'Speedmaster' }),
      watch({ id: 3, brand: 'Grand Seiko', model: 'SBGA211', isWishList: true }),
      watch({ id: 4, brand: 'Tissot', model: 'PRX', isRetired: true }),
    ])
  })

  it('logs the picked watch on the selected day and reloads the calendar', async () => {
    const wrapper = await openCalendar()

    await wrapper.get('[data-testid="add-worn-watch"]').trigger('click')
    await flushPromises()

    // Wish list and retired watches are not wearable, so they stay out.
    const offered = wrapper.findAll('ul button').map(button => button.text())
    expect(offered).toHaveLength(2)
    expect(offered[0]).toContain('Omega')
    expect(offered[1]).toContain('Seiko')

    await pick(wrapper, 'Seiko')
    await flushPromises()

    expect(watchApi.recordWear).toHaveBeenCalledWith(1, {
      wornDate: zonedDateTimeToUtc(currentDateKey(), '12:00'),
    })
    // The day's list and dot come from a reload: once on mount, once after.
    expect(watchApi.getWearLogs).toHaveBeenCalledTimes(2)
    expect(wrapper.find('[data-testid="add-worn-watch"]').exists()).toBe(true)
  })

  it('filters the picker by brand and model', async () => {
    const wrapper = await openCalendar()

    await wrapper.get('[data-testid="add-worn-watch"]').trigger('click')
    await flushPromises()
    await wrapper.get('input[type="search"]').setValue('speed')

    const offered = wrapper.findAll('ul button').map(button => button.text())
    expect(offered).toHaveLength(1)
    expect(offered[0]).toContain('Speedmaster')
  })

  it('surfaces the reason the server refused a wear', async () => {
    watchApi.recordWear.mockRejectedValue({
      response: { data: { error: 'Wear cannot be recorded for a former watch.' } },
    })
    const wrapper = await openCalendar()

    await wrapper.get('[data-testid="add-worn-watch"]').trigger('click')
    await flushPromises()
    await pick(wrapper, 'Seiko')
    await flushPromises()

    expect(wrapper.text()).toContain('Wear cannot be recorded for a former watch.')
    expect(watchApi.getWearLogs).toHaveBeenCalledTimes(1)
  })
})

async function openCalendar() {
  const wrapper = mount(WearLogPage)
  await flushPromises()
  const calendar = wrapper.findAll('button').find(button => button.text() === 'Calendar')
  if (!calendar) throw new Error('Calendar tab not found')
  await calendar.trigger('click')
  return wrapper
}

async function pick(wrapper: ReturnType<typeof mount>, brand: string) {
  const candidate = wrapper.findAll('ul button').find(button => button.text().includes(brand))
  if (!candidate) throw new Error(`Watch not offered: ${brand}`)
  await candidate.trigger('click')
}
