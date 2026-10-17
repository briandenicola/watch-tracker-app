import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import WatchPatrolConnectionPanel from './WatchPatrolConnectionPanel.vue'

const api = vi.hoisted(() => ({
  getWatchPatrolStatus: vi.fn(),
  connectWatchPatrol: vi.fn(),
  completeWatchPatrol: vi.fn(),
  disconnectWatchPatrol: vi.fn(),
}))
vi.mock('@/services/watchPatrol', () => api)

const stateKey = 'watchtracker.watchpatrol.oauth-state'

describe('WatchPatrol connection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    sessionStorage.clear()
    window.history.replaceState({}, '', '/advisor')
    api.getWatchPatrolStatus.mockResolvedValue({ connected: false })
  })

  it('shows disconnected status and reports a failed sign-in start', async () => {
    api.connectWatchPatrol.mockRejectedValue(new Error('offline'))
    const wrapper = mount(WatchPatrolConnectionPanel)
    await flushPromises()
    expect(wrapper.text()).toContain('Not connected')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(api.connectWatchPatrol).toHaveBeenCalledWith(`${window.location.origin}/advisor`)
    expect(wrapper.text()).toContain('Unable to start WatchPatrol sign-in')
    expect(wrapper.get('button').attributes('disabled')).toBeUndefined()
  })

  it('rejects an unexpected authorization host before navigating or storing state', async () => {
    api.connectWatchPatrol.mockResolvedValue('https://untrusted.test/oauth/authorize/?state=bad')
    const wrapper = mount(WatchPatrolConnectionPanel)
    await flushPromises()
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Unable to start WatchPatrol sign-in')
    expect(sessionStorage.getItem(stateKey)).toBeNull()
  })

  it('completes a matching OAuth callback and removes authorization parameters', async () => {
    sessionStorage.setItem(stateKey, 'expected-state')
    window.history.replaceState({}, '', '/advisor?code=one-time-code&state=expected-state')
    api.completeWatchPatrol.mockResolvedValue({ connected: true })
    api.getWatchPatrolStatus.mockResolvedValue({ connected: true })
    const wrapper = mount(WatchPatrolConnectionPanel)
    await flushPromises()
    expect(api.completeWatchPatrol).toHaveBeenCalledWith('one-time-code', 'expected-state')
    expect(window.location.search).toBe('')
    expect(sessionStorage.getItem(stateKey)).toBeNull()
    expect(wrapper.text()).toContain('WatchPatrol connected')
    expect(wrapper.text()).toContain('Disconnect WatchPatrol')
  })

  it('never exchanges a callback with mismatched browser state', async () => {
    sessionStorage.setItem(stateKey, 'expected-state')
    window.history.replaceState({}, '', '/advisor?code=foreign-code&state=other-state')
    const wrapper = mount(WatchPatrolConnectionPanel)
    await flushPromises()
    expect(api.completeWatchPatrol).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Unable to complete WatchPatrol sign-in')
    expect(window.location.search).toBe('')
    expect(sessionStorage.getItem(stateKey)).toBeNull()
  })

  it('reports denied consent without exposing provider error descriptions', async () => {
    sessionStorage.setItem(stateKey, 'expected-state')
    window.history.replaceState({}, '',
      '/advisor?error=access_denied&state=expected-state&error_description=private-details')
    const wrapper = mount(WatchPatrolConnectionPanel)
    await flushPromises()
    expect(api.completeWatchPatrol).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('cancelled or denied')
    expect(wrapper.text()).not.toContain('private-details')
    expect(window.location.search).toBe('')
  })

  it('disconnects only on click and retains status when disconnect fails', async () => {
    api.getWatchPatrolStatus.mockResolvedValue({ connected: true })
    api.disconnectWatchPatrol.mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce(undefined)
    const wrapper = mount(WatchPatrolConnectionPanel)
    await flushPromises()
    expect(api.disconnectWatchPatrol).not.toHaveBeenCalled()
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Unable to disconnect')
    expect(wrapper.text()).toContain('Disconnect WatchPatrol')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Disconnected from this app')
    expect(wrapper.text()).toContain('Not connected')
  })

  it('disables connect when status is unknown and allows an explicit retry', async () => {
    api.getWatchPatrolStatus.mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce({ connected: true })
    const wrapper = mount(WatchPatrolConnectionPanel)
    await flushPromises()
    expect(wrapper.get('button').attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Unable to check')
    await wrapper.findAll('button')[1]!.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Disconnect WatchPatrol')
    expect(wrapper.text()).not.toContain('Unable to check')
  })
})
