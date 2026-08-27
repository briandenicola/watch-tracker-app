import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AppLayout from './AppLayout.vue'

const auth = vi.hoisted(() => ({
  isAdmin: false,
  isAuthenticated: true,
  user: { username: 'owner', email: 'owner@example.test' },
  logout: vi.fn(),
}))
const theme = vi.hoisted(() => ({
  mode: { value: 'dark' },
  getEffectiveTheme: vi.fn(() => 'dark'),
  setTheme: vi.fn(),
}))
const application = vi.hoisted(() => ({ ready: true, load: vi.fn() }))
const notifications = vi.hoisted(() => ({
  unreadCount: 2,
  refreshUnreadCount: vi.fn(),
  setUnreadCount: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({ path: '/' }),
  RouterLink: { props: ['to'], template: '<a :href="to"><slot /></a>' },
  RouterView: { template: '<div />' },
}))
vi.mock('@/stores/auth', () => ({ useAuthStore: () => auth }))
vi.mock('@/stores/theme', () => ({ useTheme: () => theme }))
vi.mock('@/stores/application', () => ({ useApplicationSettings: () => application }))
vi.mock('@/stores/notifications', () => ({ useNotificationsStore: () => notifications }))

function mountLayout() {
  return mount(AppLayout, {
    global: {
      stubs: {
        RouterLink: { props: ['to'], template: '<a :href="to"><slot /></a>' },
        RouterView: { template: '<div />' },
      },
    },
  })
}

describe('AppLayout notifications link', () => {
  const originalWidth = window.innerWidth

  beforeEach(() => {
    vi.clearAllMocks()
    notifications.unreadCount = 2
  })

  afterEach(() => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: originalWidth })
  })

  it('keeps mobile navigation, branding, and theme controls alongside the notification badge', () => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 375 })
    const wrapper = mountLayout()

    expect(wrapper.get('h1').text()).toBe('Watch Tracker')
    expect(wrapper.find('button[aria-label="Open navigation"]').exists()).toBe(true)
    expect(wrapper.find('button[aria-label="Change theme"]').exists()).toBe(true)
    expect(wrapper.get('a[href="/notifications"]').attributes('aria-label')).toBe('2 unread notifications')
    expect(wrapper.get('a[href="/notifications"]').text()).toContain('2')
    expect(notifications.refreshUnreadCount).toHaveBeenCalledTimes(1)
  })

  it('shows a notifications link in the desktop sidebar header', () => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1024 })
    const wrapper = mountLayout()

    expect(wrapper.get('a[href="/notifications"]').attributes('aria-label')).toBe('2 unread notifications')
  })
})
