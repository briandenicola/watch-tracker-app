import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ShareWishlistModal from './ShareWishlistModal.vue'

const sharing = vi.hoisted(() => ({
  getWishlistShare: vi.fn(),
  getWishlistUserShares: vi.fn(),
  searchWishlistShareUsers: vi.fn(),
  shareWishlistWithUser: vi.fn(),
  revokeWishlistUserShare: vi.fn(),
  createWishlistShare: vi.fn(),
  revokeWishlistShare: vi.fn(),
  updateWishlistShare: vi.fn(),
  shareUrl: vi.fn(),
}))

vi.mock('@/services/sharing', () => sharing)

describe('ShareWishlistModal direct sharing', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    sharing.getWishlistShare.mockResolvedValue(null)
    sharing.getWishlistUserShares.mockResolvedValue([])
    sharing.searchWishlistShareUsers.mockResolvedValue([{ id: 2, username: 'friend' }])
    sharing.shareWishlistWithUser.mockResolvedValue({
      id: 9,
      recipientUserId: 2,
      recipientUsername: 'friend',
      includePrices: false,
      createdAt: '2026-09-08T20:00:00Z',
      viewCount: 0,
    })
  })

  it('searches for a username and grants view-only access', async () => {
    const wrapper = mount(ShareWishlistModal, {
      global: { stubs: { Teleport: true } },
    })
    await flushPromises()

    const userTab = wrapper.findAll('button').find(button =>
      button.text().includes('Share with a user'))
    expect(userTab).toBeDefined()
    await userTab!.trigger('click')
    const input = wrapper.get('input[aria-label="Search by username"]')
    await input.setValue('fri')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(sharing.searchWishlistShareUsers).toHaveBeenCalledWith('fri')
    const shareButton = wrapper.findAll('button').find(button =>
      button.text().trim() === 'friendShare')
    expect(shareButton).toBeDefined()
    await shareButton!.trigger('click')
    await flushPromises()

    expect(sharing.shareWishlistWithUser).toHaveBeenCalledWith(2, false)
    expect(wrapper.text()).toContain('People with access')
    expect(wrapper.text()).toContain('friend')
  })
})
