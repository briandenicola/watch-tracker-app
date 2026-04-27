<template>
  <div class="layout-shell">
    <!-- Desktop Sidebar -->
    <aside v-if="isDesktop" class="fixed inset-y-0 left-0 w-64 bg-sidebar-bg border-r border-border flex flex-col z-40">
      <div class="p-6 border-b border-border flex items-center gap-3">
        <AppIcon name="watch" :size="22" class="text-accent" />
        <h1 class="font-display text-xl font-semibold text-accent tracking-wide">Watch Tracker</h1>
      </div>
      <nav class="flex-1 py-4 overflow-y-auto">
        <RouterLink
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="flex items-center gap-3 px-6 py-3 text-text-secondary hover:text-text hover:bg-bg-elevated transition-colors"
          active-class="!text-accent !bg-bg-elevated border-r-2 border-accent"
        >
          <AppIcon :name="item.icon" :size="20" :stroke-width="1.5" />
          <span class="text-sm font-medium">{{ item.label }}</span>
        </RouterLink>
      </nav>
      <div class="p-4 border-t border-border space-y-2">
        <button
          @click="cycleTheme"
          class="flex items-center gap-3 w-full px-4 py-2 text-sm text-text-muted hover:text-text transition-colors"
        >
          <AppIcon :name="theme.getEffectiveTheme() === 'dark' ? 'moon' : 'sun'" :size="18" />
          <span class="capitalize">{{ theme.mode.value }}</span>
        </button>
        <button
          @click="auth.logout(); $router.push('/login')"
          class="flex items-center gap-3 w-full px-4 py-2 text-sm text-text-muted hover:text-danger transition-colors"
        >
          <AppIcon name="sign-out" :size="18" />
          <span>Sign Out</span>
        </button>
      </div>
    </aside>

    <!-- Mobile Header -->
    <header v-if="!isDesktop" class="mobile-header">
      <div class="flex items-center justify-between px-4 h-12">
        <button @click="sidebarOpen = true" class="p-2 -ml-2 text-text-secondary hover:text-text">
          <AppIcon name="menu" :size="22" :stroke-width="1.5" />
        </button>
        <div class="flex items-center gap-2">
          <AppIcon name="watch" :size="18" class="text-accent" />
          <h1 class="font-display text-lg font-semibold text-accent tracking-wide">Watch Tracker</h1>
        </div>
        <button @click="cycleTheme" class="p-2 -mr-2 text-text-muted hover:text-text">
          <AppIcon :name="theme.getEffectiveTheme() === 'dark' ? 'moon' : 'sun'" :size="18" />
        </button>
      </div>
    </header>

    <!-- Mobile Slide-in Sidebar -->
    <Teleport to="body">
      <Transition name="sidebar">
        <div v-if="sidebarOpen && !isDesktop" class="fixed inset-0 z-[60]" @click="sidebarOpen = false">
          <div class="absolute inset-0 bg-black/60" />
          <aside
            class="absolute inset-y-0 left-0 w-72 bg-nav-bg border-r border-border flex flex-col"
            @click.stop
          >
            <div class="p-6 border-b border-border flex items-center justify-between" style="padding-top: max(1.5rem, env(safe-area-inset-top));">
              <div class="flex items-center gap-2">
                <AppIcon name="watch" :size="20" class="text-accent" />
                <h2 class="font-display text-lg font-semibold text-accent">Watch Tracker</h2>
              </div>
              <button @click="sidebarOpen = false" class="p-1 text-text-muted hover:text-text">
                <AppIcon name="close" :size="20" />
              </button>
            </div>
            <nav class="flex-1 py-4 overflow-y-auto">
              <RouterLink
                v-for="item in navItems"
                :key="item.to"
                :to="item.to"
                class="flex items-center gap-3 px-6 py-3.5 text-text-secondary hover:text-text hover:bg-bg-elevated transition-colors"
                active-class="!text-accent !bg-bg-elevated"
                @click="sidebarOpen = false"
              >
                <AppIcon :name="item.icon" :size="20" :stroke-width="1.5" />
                <span class="text-sm font-medium">{{ item.label }}</span>
              </RouterLink>
            </nav>
            <div class="p-4 border-t border-border" style="padding-bottom: max(1rem, env(safe-area-inset-bottom));">
              <div class="flex items-center gap-3 px-2 py-2 mb-3">
                <div class="w-8 h-8 rounded-full bg-bg-elevated flex items-center justify-center text-xs text-accent font-semibold">
                  {{ auth.user?.username?.charAt(0).toUpperCase() }}
                </div>
                <div class="text-sm">
                  <p class="text-text font-medium">{{ auth.user?.username }}</p>
                  <p class="text-text-muted text-xs">{{ auth.user?.email }}</p>
                </div>
              </div>
              <button
                @click="cycleTheme"
                class="flex items-center gap-3 w-full px-4 py-2 text-sm text-text-muted hover:text-text transition-colors"
              >
                <AppIcon :name="theme.getEffectiveTheme() === 'dark' ? 'moon' : 'sun'" :size="16" />
                <span class="capitalize">Theme: {{ theme.mode.value }}</span>
              </button>
              <button
                @click="auth.logout(); $router.push('/login'); sidebarOpen = false"
                class="flex items-center gap-3 w-full px-4 py-2 text-sm text-text-muted hover:text-danger transition-colors"
              >
                <AppIcon name="sign-out" :size="16" />
                <span>Sign Out</span>
              </button>
            </div>
          </aside>
        </div>
      </Transition>
    </Teleport>

    <!-- Main Content -->
    <main class="mobile-main lg:ml-64 lg:p-6">
      <RouterView />
    </main>

    <!-- Mobile Bottom Nav -->
    <nav v-if="!isDesktop" class="mobile-bottom-nav">
      <div class="flex justify-around items-end pt-2 pb-1">
        <RouterLink
          v-for="tab in bottomTabs"
          :key="tab.to"
          :to="tab.to"
          class="flex flex-col items-center gap-0.5 min-w-[48px] py-1 text-text-muted transition-colors"
          :class="{ '!text-accent': isActiveTab(tab.to) }"
        >
          <AppIcon :name="tab.icon" :size="22" :stroke-width="isActiveTab(tab.to) ? 1.75 : 1.25" />
          <span class="text-[10px] font-medium">{{ tab.label }}</span>
        </RouterLink>
        <!-- Raised FAB for Add -->
        <RouterLink
          to="/watches/new"
          class="flex items-center justify-center w-14 h-14 -mt-6 rounded-full bg-accent text-bg shadow-lg shadow-accent/30 hover:bg-accent-hover transition-colors"
        >
          <AppIcon name="plus" :size="28" :stroke-width="1.75" />
        </RouterLink>
        <RouterLink
          v-for="tab in bottomTabsRight"
          :key="tab.to"
          :to="tab.to"
          class="flex flex-col items-center gap-0.5 min-w-[48px] py-1 text-text-muted transition-colors"
          :class="{ '!text-accent': isActiveTab(tab.to) }"
        >
          <AppIcon :name="tab.icon" :size="22" :stroke-width="isActiveTab(tab.to) ? 1.75 : 1.25" />
          <span class="text-[10px] font-medium">{{ tab.label }}</span>
        </RouterLink>
      </div>
    </nav>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTheme } from '@/stores/theme'
import AppIcon from '@/components/icons/AppIcon.vue'

const auth = useAuthStore()
const route = useRoute()
const theme = useTheme()
const sidebarOpen = ref(false)
const windowWidth = ref(window.innerWidth)

const isDesktop = computed(() => windowWidth.value >= 1024)

function onResize() {
  windowWidth.value = window.innerWidth
}

onMounted(() => window.addEventListener('resize', onResize))
onUnmounted(() => window.removeEventListener('resize', onResize))

function cycleTheme() {
  const modes = ['dark', 'light', 'system'] as const
  const idx = modes.indexOf(theme.mode.value)
  theme.setTheme(modes[(idx + 1) % modes.length])
}

const navItems = computed(() => [
  { to: '/', icon: 'collection', label: 'Collection' },
  { to: '/stats', icon: 'stats', label: 'Statistics' },
  { to: '/retired', icon: 'retired', label: 'Retired' },
  { to: '/settings', icon: 'settings', label: 'Settings' },
  ...(auth.isAdmin ? [{ to: '/admin', icon: 'admin', label: 'Admin' }] : []),
])

const bottomTabs = [
  { to: '/', icon: 'collection', label: 'Home' },
  { to: '/stats', icon: 'stats', label: 'Stats' },
]

const bottomTabsRight = [
  { to: '/retired', icon: 'retired', label: 'Retired' },
  { to: '/settings', icon: 'settings', label: 'Settings' },
]

function isActiveTab(path: string): boolean {
  if (path === '/') return route.path === '/'
  return route.path.startsWith(path)
}
</script>

<style scoped>
.layout-shell {
  min-height: 100dvh;
}

/* Mobile header: fixed to top, below the PWA title bar */
.mobile-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 50;
  background-color: var(--color-nav-bg);
  border-bottom: 1px solid var(--color-border);
  padding-top: env(safe-area-inset-top, 0px);
}

/* Mobile main content: scrollable area between header and bottom nav */
.mobile-main {
  padding-top: calc(env(safe-area-inset-top, 0px) + 3rem + 1rem);
  padding-bottom: calc(env(safe-area-inset-bottom, 0px) + 4.5rem);
  padding-left: 1rem;
  padding-right: 1rem;
  min-height: 100dvh;
}

/* Mobile bottom nav: truly fixed to viewport bottom */
.mobile-bottom-nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 50;
  background-color: var(--color-nav-bg);
  border-top: 1px solid var(--color-border);
  padding-bottom: env(safe-area-inset-bottom, 0px);
}

/* Extend nav background below the safe area to fill any gap on iOS PWA */
.mobile-bottom-nav::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  bottom: -100px;
  height: 100px;
  background-color: var(--color-nav-bg);
}

@media (min-width: 1024px) {
  .mobile-main {
    padding-top: 0;
    padding-bottom: 0;
    padding-left: 1.5rem;
    padding-right: 1.5rem;
    min-height: auto;
  }
}

.sidebar-enter-active,
.sidebar-leave-active {
  transition: opacity 0.2s ease;
}
.sidebar-enter-active aside,
.sidebar-leave-active aside {
  transition: transform 0.3s ease;
}
.sidebar-enter-from,
.sidebar-leave-to {
  opacity: 0;
}
.sidebar-enter-from aside,
.sidebar-leave-to aside {
  transform: translateX(-100%);
}
</style>
