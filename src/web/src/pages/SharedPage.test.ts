import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import SharedPage from './SharedPage.vue'

const route = vi.hoisted(() => ({ params: {} as { id?: string } }))
const push = vi.hoisted(() => vi.fn())
const sharing = vi.hoisted(() => ({
  getReceivedWishlistShares: vi.fn(),
  getReceivedWishlistShare: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => route,
  useRouter: () => ({ push }),
}))
vi.mock('@/services/sharing', () => sharing)
vi.mock('@/services/watches', () => ({ imageUrl: (url: string) => url }))

describe('SharedPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    route.params = {}
    sharing.getReceivedWishlistShares.mockResolvedValue([
      {
        id: 7,
        ownerName: 'collector',
        includesPrices: false,
        sharedAt: '2026-09-08T20:00:00Z',
        itemCount: 3,
      },
    ])
  })

  it('lists wish lists shared with the signed-in user', async () => {
    const wrapper = mount(SharedPage)
    await flushPromises()

    expect(wrapper.text()).toContain("collector's wish list")
    expect(wrapper.text()).toContain('3 watches')
    await wrapper.get('button.detail-card').trigger('click')
    expect(push).toHaveBeenCalledWith({ name: 'shared', params: { id: 7 } })
  })
})
