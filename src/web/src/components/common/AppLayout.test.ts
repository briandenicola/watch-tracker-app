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
  isLight: vi.fn(() => false),
  setTheme: vi.fn(),
}))
const application = vi.hoisted(() => ({ ready: true, load: vi.fn() }))
const labels = vi.hoisted(() => ({
  pinnedViews: [] as { id: number; name: string; scope: string }[],
  load: vi.fn(() => Promise.resolve()),
  clear: vi.fn(),
}))
const notifications = vi.hoisted(() => ({
  unreadCount: 2,
  refreshUnreadCount: vi.fn(),
  setUnreadCount: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({ path: '/', query: {} }),
  RouterLink: { props: ['to'], template: '<a :href="to"><slot /></a>' },
  RouterView: { template: '<div />' },
}))
vi.mock('@/stores/auth', () => ({ useAuthStore: () => auth }))
vi.mock('@/stores/theme', () => ({
  useTheme: () => theme,
  THEME_LABELS: { light: 'Light', dark: 'Dark', system: 'System' },
  BASE_THEMES: ['system', 'dark', 'light'],
  MUSEUM_THEMES: [],
}))
vi.mock('@/stores/application', () => ({ useApplicationSettings: () => application }))
vi.mock('@/stores/notifications', () => ({ useNotificationsStore: () => notifications }))
vi.mock('@/stores/labels', () => ({ useLabelsStore: () => labels }))

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
    labels.pinnedViews = []
  })

  afterEach(() => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: originalWidth })
  })

  it('keeps the mobile notification badge right-aligned without a theme control', () => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 375 })
    const wrapper = mountLayout()

    expect(wrapper.get('h1').text()).toBe('Watch Tracker')
    expect(wrapper.find('button[aria-label="Open navigation"]').exists()).toBe(true)
    expect(wrapper.find('button[aria-label="Change theme"]').exists()).toBe(false)
    expect(wrapper.get('a[href="/notifications"]').attributes('aria-label')).toBe('2 unread notifications')
    expect(wrapper.get('a[href="/notifications"]').text()).toContain('2')
    expect(notifications.refreshUnreadCount).toHaveBeenCalledTimes(1)
  })

  it('keeps notifications out of the desktop sidebar header', () => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1024 })
    const wrapper = mountLayout()

    expect(wrapper.find('a[href="/notifications"]').exists()).toBe(false)
    expect(wrapper.get('a[href="/shared"]').text()).toContain('Shared')
    const links = wrapper.findAll('nav a')
    const settingsIndex = links.findIndex(link => link.attributes('href') === '/settings')
    expect(links[settingsIndex + 1].attributes('href')).toBe('/help')
    expect(links[settingsIndex + 1].text()).toContain('Help')
  })

  it('lists pinned views in the desktop sidebar and loads them on start', () => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1024 })
    labels.pinnedViews = [
      { id: 3, name: 'Divers', scope: 'Collection' },
      { id: 7, name: 'Grails', scope: 'Wishlist' },
    ]
    const wrapper = mountLayout()

    expect(labels.load).toHaveBeenCalledTimes(1)
    expect(wrapper.get('[data-testid="pinned-views"]').findAll('a').map(link => link.text())).toEqual(['Divers', 'Grails'])
  })

  it('hides the views section when nothing is pinned', () => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1024 })
    expect(mountLayout().find('[data-testid="pinned-views"]').exists()).toBe(false)
  })
})
