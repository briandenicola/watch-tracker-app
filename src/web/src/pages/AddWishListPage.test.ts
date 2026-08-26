import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AddWishListPage from './AddWishListPage.vue'

const watchApi = vi.hoisted(() => ({
  createWatch: vi.fn(),
  extractWishlistUrl: vi.fn(),
  getWatches: vi.fn(),
  uploadImage: vi.fn(),
  importImageFromUrl: vi.fn(),
}))
const apiGet = vi.hoisted(() => vi.fn())
const push = vi.hoisted(() => vi.fn())

vi.mock('@/services/watches', () => watchApi)
vi.mock('@/services/api', () => ({ api: { get: apiGet } }))
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }))

describe('wishlist URL import', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    watchApi.getWatches.mockResolvedValue([])
    apiGet.mockResolvedValue({ data: { storageLocations: [] } })
    watchApi.createWatch.mockResolvedValue({ id: 42 })
    watchApi.importImageFromUrl.mockResolvedValue(undefined)
  })

  it('prefills the review form and saves only after confirmation', async () => {
    watchApi.extractWishlistUrl.mockResolvedValue({
      brand: 'Seiko',
      model: 'Prospex SPB143',
      purchasePrice: 1295,
      linkUrl: 'https://shop.example.test/watch',
      linkText: 'Example Store',
      imageUrl: 'https://shop.example.test/watch.jpg',
      warnings: [],
    })
    const wrapper = mount(AddWishListPage)
    await flushPromises()

    await wrapper.get('[data-testid="wishlist-source-url"]')
      .setValue('https://shop.example.test/watch')
    await findButton(wrapper, 'Extract details').trigger('click')
    await flushPromises()

    expect(input(wrapper, 'e.g. Omega, Rolex, Seiko').element.value).toBe('Seiko')
    expect(input(wrapper, 'e.g. Speedmaster Professional').element.value)
      .toBe('Prospex SPB143')
    expect(wrapper.text()).toContain('Review them before saving')
    expect(watchApi.createWatch).not.toHaveBeenCalled()

    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(watchApi.createWatch).toHaveBeenCalledWith(expect.objectContaining({
      brand: 'Seiko',
      model: 'Prospex SPB143',
      purchasePrice: 1295,
      linkUrl: 'https://shop.example.test/watch',
      linkText: 'Example Store',
      isWishList: true,
    }))
    expect(watchApi.importImageFromUrl)
      .toHaveBeenCalledWith(42, 'https://shop.example.test/watch.jpg')
    expect(push).toHaveBeenCalledWith('/?tab=wishlist')
  })

  it('leaves manually entered values intact when extraction fails', async () => {
    watchApi.extractWishlistUrl.mockRejectedValue({
      response: { data: { error: 'Store blocked the request.' } },
    })
    const wrapper = mount(AddWishListPage)
    await flushPromises()

    const brand = input(wrapper, 'e.g. Omega, Rolex, Seiko')
    await brand.setValue('My manual brand')
    await wrapper.get('[data-testid="wishlist-source-url"]')
      .setValue('https://shop.example.test/watch')
    await findButton(wrapper, 'Extract details').trigger('click')
    await flushPromises()

    expect(brand.element.value).toBe('My manual brand')
    expect(wrapper.text()).toContain('Store blocked the request.')
  })
})

function input(
  wrapper: ReturnType<typeof mount>,
  placeholder: string,
) {
  return wrapper.get<HTMLInputElement>(`input[placeholder="${placeholder}"]`)
}

function findButton(
  wrapper: ReturnType<typeof mount>,
  label: string,
) {
  const button = wrapper.findAll('button').find(candidate => candidate.text() === label)
  if (!button) throw new Error(`Button not found: ${label}`)
  return button
}
