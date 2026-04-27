<template>
  <div class="min-h-dvh flex flex-col lg:flex-row">
    <!-- Desktop Sidebar -->
    <aside v-if="isDesktop" class="fixed inset-y-0 left-0 w-64 bg-sidebar-bg border-r border-border flex flex-col z-40">
      <div class="p-6 border-b border-border">
        <h1 class="font-display text-xl font-semibold text-accent tracking-wide">⌚ Watch Tracker</h1>
      </div>
      <nav class="flex-1 py-4 overflow-y-auto">
        <RouterLink
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="flex items-center gap-3 px-6 py-3 text-text-secondary hover:text-text hover:bg-bg-elevated transition-colors"
          active-class="!text-accent !bg-bg-elevated border-r-2 border-accent"
        >
          <span class="text-lg">{{ item.icon }}</span>
          <span class="text-sm font-medium">{{ item.label }}</span>
        </RouterLink>
      </nav>
      <div class="p-4 border-t border-border">
        <button
          @click="auth.logout(); $router.push('/login')"
          class="w-full text-left px-4 py-2 text-sm text-text-muted hover:text-danger transition-colors"
        >
          Sign Out
        </button>
      </div>
    </aside>

    <!-- Mobile Header -->
    <header v-if="!isDesktop" class="fixed top-0 inset-x-0 bg-nav-bg border-b border-border z-50 safe-area-top">
      <div class="flex items-center justify-between px-4 py-3">
        <button @click="sidebarOpen = true" class="p-2 -ml-2 text-text-secondary hover:text-text">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <h1 class="font-display text-lg font-semibold text-accent tracking-wide">Watch Tracker</h1>
        <div class="w-8" />
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
            <div class="p-6 border-b border-border flex items-center justify-between safe-area-top">
              <h2 class="font-display text-lg font-semibold text-accent">⌚ Watch Tracker</h2>
              <button @click="sidebarOpen = false" class="p-1 text-text-muted hover:text-text">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
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
                <span class="text-lg">{{ item.icon }}</span>
                <span class="text-sm font-medium">{{ item.label }}</span>
              </RouterLink>
            </nav>
            <div class="p-4 border-t border-border safe-area-bottom">
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
                @click="auth.logout(); $router.push('/login'); sidebarOpen = false"
                class="w-full text-left px-4 py-2 text-sm text-text-muted hover:text-danger transition-colors"
              >
                Sign Out
              </button>
            </div>
          </aside>
        </div>
      </Transition>
    </Teleport>

    <!-- Main Content -->
    <main
      class="flex-1 lg:ml-64"
      :class="!isDesktop ? 'pt-14 pb-20' : ''"
    >
      <div class="safe-area-top lg:p-0" :class="!isDesktop ? 'px-4 py-4' : 'p-6'">
        <RouterView />
      </div>
    </main>

    <!-- Mobile Bottom Nav -->
    <nav v-if="!isDesktop" class="fixed bottom-0 inset-x-0 bg-nav-bg border-t border-border z-50 safe-area-bottom">
      <div class="flex justify-around items-end pt-2 pb-2">
        <RouterLink
          v-for="tab in bottomTabs"
          :key="tab.to"
          :to="tab.to"
          class="flex flex-col items-center gap-0.5 min-w-[48px] py-1 text-text-muted transition-colors"
          :class="{ '!text-accent': isActiveTab(tab.to) }"
        >
          <span class="text-xl">{{ tab.icon }}</span>
          <span class="text-[10px] font-medium">{{ tab.label }}</span>
        </RouterLink>
        <!-- Raised FAB for Add -->
        <RouterLink
          to="/watches/new"
          class="flex items-center justify-center w-14 h-14 -mt-6 rounded-full bg-accent text-bg shadow-lg shadow-accent/30 hover:bg-accent-hover transition-colors"
        >
          <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4" />
          </svg>
        </RouterLink>
        <RouterLink
          v-for="tab in bottomTabsRight"
          :key="tab.to"
          :to="tab.to"
          class="flex flex-col items-center gap-0.5 min-w-[48px] py-1 text-text-muted transition-colors"
          :class="{ '!text-accent': isActiveTab(tab.to) }"
        >
          <span class="text-xl">{{ tab.icon }}</span>
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

const auth = useAuthStore()
const route = useRoute()
const sidebarOpen = ref(false)
const windowWidth = ref(window.innerWidth)

const isDesktop = computed(() => windowWidth.value >= 1024)

function onResize() {
  windowWidth.value = window.innerWidth
}

onMounted(() => window.addEventListener('resize', onResize))
onUnmounted(() => window.removeEventListener('resize', onResize))

const navItems = [
  { to: '/', icon: '🏠', label: 'Collection' },
  { to: '/stats', icon: '📊', label: 'Statistics' },
  { to: '/retired', icon: '🗄️', label: 'Retired' },
  { to: '/settings', icon: '⚙️', label: 'Settings' },
  ...(auth.isAdmin ? [{ to: '/admin', icon: '🔧', label: 'Admin' }] : []),
]

const bottomTabs = [
  { to: '/', icon: '🏠', label: 'Home' },
  { to: '/stats', icon: '📊', label: 'Stats' },
]

const bottomTabsRight = [
  { to: '/retired', icon: '🗄️', label: 'Retired' },
  { to: '/settings', icon: '⚙️', label: 'Settings' },
]

function isActiveTab(path: string): boolean {
  if (path === '/') return route.path === '/'
  return route.path.startsWith(path)
}
</script>

<style scoped>
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
