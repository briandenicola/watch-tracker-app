import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import WatchDetailPage from './WatchDetailPage.vue'
import type { Watch } from '@/types'

const router = vi.hoisted(() => ({ push: vi.fn() }))
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
  useRouter: () => router,
  RouterLink: { template: '<a><slot /></a>' },
}))

const currentWatch: Watch = {
  id: 12,
  brand: 'Hamilton',
  model: 'Khaki Field',
  movementType: 'Automatic',
  acquisitionType: 'New',
  timesWorn: 2,
  imageUrls: [{ id: 5, url: '/uploads/watch.jpg' }],
  purchasePrice: 500,
  isWishList: false,
  priceAlertEnabled: false,
  isRetired: false,
  createdAt: '2026-08-26T12:00:00Z',
  updatedAt: '2026-08-26T12:00:00Z',
}

const disposition = {
  type: 'Sold' as const,
  dispositionDate: '2026-08-27',
  soldTo: 'Collector',
  salePrice: 650,
}

function mountPage() {
  return mount(WatchDetailPage, {
    global: {
      stubs: {
        RouterLink: { template: '<a><slot /></a>' },
        DispositionModal: {
          emits: ['save'],
          setup(_, { emit }) {
            return { save: () => emit('save', disposition) }
          },
          template: '<button data-test="save-disposition" @click="save">Save disposition</button>',
        },
      },
    },
  })
}

async function openActions(wrapper: ReturnType<typeof mount>) {
  await wrapper.get('button[aria-label="Watch actions"]').trigger('click')
}

function action(wrapper: ReturnType<typeof mount>, label: string) {
  const button = wrapper.findAll('button').find(candidate => candidate.text() === label)
  if (!button) throw new Error(`Could not find ${label} action`)
  return button
}

describe('WatchDetailPage characteristic workflows', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.stubGlobal('confirm', vi.fn(() => true))
    vi.stubGlobal('ResizeObserver', class {
      observe() {}
      disconnect() {}
    })
    watchApi.getWatch.mockResolvedValue(structuredClone(currentWatch))
    watchApi.getWatches.mockResolvedValue([structuredClone(currentWatch)])
    watchApi.getResaleHistory.mockResolvedValue([])
    watchApi.toUpdatePayload.mockReturnValue({ saved: true })
    watchApi.updateWatch.mockResolvedValue(structuredClone(currentWatch))
    watchApi.analyzeWatch.mockResolvedValue({
      summary: 'A field watch.',
      suggestions: [],
      sources: [],
    })
    watchApi.setWatchDisposition.mockResolvedValue({
      ...structuredClone(currentWatch),
      disposition,
    })
    watchApi.addManualResaleValue.mockResolvedValue(structuredClone(currentWatch))
  })

  it('saves an inline edit through the complete update-payload guard', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.get('button[aria-label="Edit watch"]').trigger('click')
    await flushPromises()
    await action(wrapper, 'Hamilton').trigger('click')
    await wrapper.get('input.edit-control').setValue('Hamilton Updated')
    await wrapper.get('input.edit-control').trigger('keydown.enter')
    await wrapper.get('button[aria-label="Save edits"]').trigger('click')
    await flushPromises()

    expect(watchApi.toUpdatePayload).toHaveBeenCalledWith(currentWatch, { brand: 'Hamilton Updated' })
    expect(watchApi.updateWatch).toHaveBeenCalledWith(12, { saved: true })
  })

  it('uploads selected images then reloads the watch', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await openActions(wrapper)

    const input = wrapper.get('input[type="file"]')
    const file = new File(['image'], 'watch.png', { type: 'image/png' })
    Object.defineProperty(input.element, 'files', { configurable: true, value: [file] })
    await input.trigger('change')
    await flushPromises()

    expect(watchApi.uploadImage).toHaveBeenCalledWith(12, file)
    expect(watchApi.getWatch).toHaveBeenCalledTimes(2)
  })

  it('removes the selected image only after confirmation and reloads', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await action(wrapper, 'Delete Image').trigger('click')
    await flushPromises()

    expect(confirm).toHaveBeenCalledWith('Delete this image?')
    expect(watchApi.deleteImage).toHaveBeenCalledWith(12, 5)
    expect(watchApi.getWatch).toHaveBeenCalledTimes(2)
  })

  it('sends the selected image to background removal and reloads', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await action(wrapper, 'Remove Background').trigger('click')
    await flushPromises()

    expect(watchApi.removeBackground).toHaveBeenCalledWith(12, 5)
    expect(watchApi.getWatch).toHaveBeenCalledTimes(2)
  })

  it('saves a disposition supplied by its modal', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await openActions(wrapper)
    await action(wrapper, 'Remove from collection').trigger('click')
    await wrapper.get('[data-test="save-disposition"]').trigger('click')
    await flushPromises()

    expect(watchApi.setWatchDisposition).toHaveBeenCalledWith(12, disposition)
  })

  it('adds a manual resale value and reloads its history', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await action(wrapper, '+ Log Value').trigger('click')

    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('725.50')
    await inputs[1].setValue('2026-08-27')
    await inputs[2].setValue('Dealer estimate')
    await action(wrapper, 'Save').trigger('click')
    await flushPromises()

    expect(watchApi.addManualResaleValue).toHaveBeenCalledWith(12, {
      value: 725.5,
      recordedAt: '2026-08-27',
      notes: 'Dealer estimate',
    })
    expect(watchApi.getResaleHistory).toHaveBeenCalledTimes(2)
  })

  it('deletes the watch after confirmation and returns to collection', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await openActions(wrapper)
    await action(wrapper, 'Delete').trigger('click')
    await flushPromises()

    expect(confirm).toHaveBeenCalledWith('Delete this watch permanently?')
    expect(watchApi.deleteWatch).toHaveBeenCalledWith(12)
    expect(router.push).toHaveBeenCalledWith({ path: '/' })
  })

  it('offers and runs AI Analyze for a watch with an image', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await openActions(wrapper)

    await action(wrapper, 'AI Analyze').trigger('click')
    await flushPromises()

    expect(watchApi.analyzeWatch).toHaveBeenCalledWith(12)
    expect(watchApi.getWatch).toHaveBeenCalledTimes(2)
  })
})
