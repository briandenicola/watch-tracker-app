<template>
  <div>
    <!-- Pull to Refresh indicator -->
    <PullToRefresh :pulling="pulling" :refreshing="refreshing" :pull-distance="pullDistance" />

    <!-- Tab Toggle + Filter Menu -->
    <div class="flex items-center justify-between mb-4">
      <div class="flex gap-1 bg-bg-surface border border-border rounded-lg p-1">
        <button
          @click="tab = 'collection'"
          class="px-4 py-2 text-sm font-medium rounded-md transition-colors"
          :class="tab === 'collection' ? 'bg-accent text-bg' : 'text-text-secondary hover:text-text'"
        >
          Collection
        </button>
        <button
          @click="tab = 'wishlist'"
          class="px-4 py-2 text-sm font-medium rounded-md transition-colors"
          :class="tab === 'wishlist' ? 'bg-accent text-bg' : 'text-text-secondary hover:text-text'"
        >
          Wish List
        </button>
      </div>

      <!-- Filter toggle + count -->
      <button
        @click="showFilters = !showFilters"
        class="flex items-center gap-2 px-3 py-2 text-sm text-text-muted hover:text-text transition-colors"
        :class="{ '!text-accent': hasActiveFilters }"
      >
        <span>{{ filteredWatches.length }}</span>
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3 4h18M6 8h12M9 12h6M11 16h2" />
        </svg>
      </button>
    </div>

    <!-- Collapsible Filter Panel -->
    <Transition name="filter">
      <div v-if="showFilters" class="mb-4 p-3 bg-bg-surface border border-border rounded-xl space-y-3">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-text-secondary uppercase tracking-wider">Filter & Sort</span>
          <button
            v-if="hasActiveFilters"
            @click="clearFilters"
            class="text-xs text-accent hover:text-accent-hover transition-colors"
          >
            Clear All
          </button>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
          <select
            v-model="filterBrand"
            class="px-3 py-2.5 bg-bg border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
          >
            <option value="">All Brands</option>
            <option v-for="brand in brands" :key="brand" :value="brand">{{ brand }}</option>
          </select>
          <select
            v-model="filterMovement"
            class="px-3 py-2.5 bg-bg border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
          >
            <option value="">All Movements</option>
            <option value="Automatic">Automatic</option>
            <option value="Manual">Manual</option>
            <option value="Quartz">Quartz</option>
            <option value="Digital">Digital</option>
          </select>
          <select
            v-model="sortBy"
            class="px-3 py-2.5 bg-bg border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
          >
            <option value="dateAdded">Date Added</option>
            <option value="brand">Brand</option>
            <option value="lastWorn">Last Worn</option>
            <option value="timesWorn">Most Worn</option>
          </select>
        </div>
      </div>
    </Transition>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <!-- Empty State: Collection -->
    <div v-else-if="tab === 'collection' && filteredWatches.length === 0" class="text-center py-20">
      <p class="text-5xl mb-4">⌚</p>
      <p class="text-text-secondary mb-4">{{ allWatches.length === 0 ? 'Your collection is empty' : 'No watches match filters' }}</p>
      <RouterLink v-if="allWatches.length === 0" to="/watches/new" class="inline-block px-5 py-2.5 bg-accent hover:bg-accent-hover text-bg font-medium rounded-lg transition-colors">
        Add Your First Watch
      </RouterLink>
      <button v-else @click="clearFilters" class="px-5 py-2.5 bg-bg-surface border border-border text-text-secondary font-medium rounded-lg hover:border-accent transition-colors">
        Clear Filters
      </button>
    </div>

    <!-- Empty State: Wish List -->
    <div v-else-if="tab === 'wishlist' && filteredWatches.length === 0" class="text-center py-20">
      <p class="text-5xl mb-4">✨</p>
      <p class="text-text-secondary mb-4">Your wish list is empty</p>
      <RouterLink to="/wishlist/new" class="inline-block px-5 py-2.5 bg-accent hover:bg-accent-hover text-bg font-medium rounded-lg transition-colors">
        Add a Watch to Your Wish List
      </RouterLink>
    </div>

    <!-- Swipe Gallery (Mobile) -->
    <div v-else-if="!isDesktop" class="relative">
      <div class="overflow-hidden rounded-xl">
        <div
          ref="swipeEl"
          class="flex transition-transform duration-300 ease-out"
          :style="{ transform: `translateX(-${currentIndex * 100}%)` }"
          @touchstart="onTouchStart"
          @touchend="onTouchEnd"
        >
          <div
            v-for="watch in filteredWatches"
            :key="watch.id"
            class="w-full flex-shrink-0"
          >
            <RouterLink :to="`/watches/${watch.id}`" class="block">
              <div class="relative aspect-[3/4] bg-bg-surface rounded-xl overflow-hidden">
                <img
                  v-if="watch.imageUrls.length > 0"
                  :src="imageUrl(watch.imageUrls[0].url)"
                  :alt="`${watch.brand} ${watch.model}`"
                  class="absolute inset-0 w-full h-full object-contain p-4"
                  loading="lazy"
                />
                <div v-else class="absolute inset-0 flex items-center justify-center text-6xl text-text-muted">
                  ⌚
                </div>
                <div class="absolute bottom-0 inset-x-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent p-5 pt-16">
                  <p class="font-display text-xl font-semibold text-white">{{ watch.brand }}</p>
                  <p class="text-sm text-white/80">{{ watch.model }}</p>
                  <p v-if="tab === 'wishlist' && watch.purchasePrice" class="text-sm text-accent mt-1 font-medium">${{ watch.purchasePrice.toFixed(2) }}</p>
                </div>
              </div>
            </RouterLink>
          </div>
        </div>
      </div>

      <!-- Swipe Controls -->
      <div class="flex items-center justify-between mt-4 px-2">
        <button
          @click="prev"
          :disabled="currentIndex === 0"
          class="p-2 text-text-secondary hover:text-accent disabled:opacity-30 transition-colors"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <span class="text-sm text-text-muted">{{ currentIndex + 1 }} / {{ filteredWatches.length }}</span>
        <button
          @click="next"
          :disabled="currentIndex === filteredWatches.length - 1"
          class="p-2 text-text-secondary hover:text-accent disabled:opacity-30 transition-colors"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Grid (Desktop) -->
    <div v-else class="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-6">
      <!-- Add New Card -->
      <RouterLink
        :to="tab === 'wishlist' ? '/wishlist/new' : '/watches/new'"
        class="group bg-bg-card border-2 border-dashed border-border rounded-xl overflow-hidden hover:border-accent/50 transition-colors flex flex-col items-center justify-center min-h-[320px]"
      >
        <div class="text-4xl text-text-muted group-hover:text-accent transition-colors mb-2">+</div>
        <p class="text-sm text-text-muted group-hover:text-accent transition-colors">{{ tab === 'wishlist' ? 'Add to Wish List' : 'Add Watch' }}</p>
      </RouterLink>
      <RouterLink
        v-for="watch in filteredWatches"
        :key="watch.id"
        :to="`/watches/${watch.id}`"
        class="group bg-bg-card border border-border rounded-xl overflow-hidden hover:border-accent/50 transition-colors"
      >
        <div class="aspect-square bg-bg-surface overflow-hidden">
          <img
            v-if="watch.imageUrls.length > 0"
            :src="imageUrl(watch.imageUrls[0].url)"
            :alt="`${watch.brand} ${watch.model}`"
            class="w-full h-full object-contain p-4 group-hover:scale-105 transition-transform duration-300"
            loading="lazy"
          />
          <div v-else class="w-full h-full flex items-center justify-center text-5xl text-text-muted">⌚</div>
        </div>
        <div class="p-4">
          <p class="font-display text-lg font-medium text-text">{{ watch.brand }}</p>
          <p class="text-sm text-text-secondary">{{ watch.model }}</p>
          <p v-if="tab === 'wishlist' && watch.purchasePrice" class="text-sm text-accent font-medium mt-1">${{ watch.purchasePrice.toFixed(2) }}</p>
          <p v-else-if="tab === 'collection'" class="text-xs text-text-muted mt-1">{{ watch.movementType }} · Worn {{ watch.timesWorn }}×</p>
        </div>
      </RouterLink>
    </div>

    <!-- Wish list add link -->
    <div v-if="tab === 'wishlist' && filteredWatches.length > 0" class="mt-6 text-center">
      <RouterLink to="/wishlist/new" class="inline-block px-5 py-2.5 bg-accent hover:bg-accent-hover text-bg font-medium rounded-lg transition-colors">
        + Add to Wish List
      </RouterLink>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch as vueWatch } from 'vue'
import { useRoute } from 'vue-router'
import type { Watch } from '@/types'
import { getWatches, imageUrl } from '@/services/watches'
import { usePullToRefresh } from '@/composables/usePullToRefresh'
import { usePreferences } from '@/stores/preferences'
import PullToRefresh from '@/components/common/PullToRefresh.vue'

const route = useRoute()
const { prefs } = usePreferences()

const allWatches = ref<Watch[]>([])
const loading = ref(true)
const tab = ref<'collection' | 'wishlist'>(route.query.tab === 'wishlist' ? 'wishlist' : 'collection')
const currentIndex = ref(0)
const windowWidth = ref(window.innerWidth)
const isDesktop = computed(() => windowWidth.value >= 1024)
const swipeEl = ref<HTMLElement | null>(null)

// Filter & Sort state — default from preferences
const filterBrand = ref('')
const filterMovement = ref('')
const sortBy = ref(prefs.value.defaultSort)

const brands = computed(() => {
  const tabWatches = tab.value === 'wishlist'
    ? allWatches.value.filter(w => w.isWishList)
    : allWatches.value.filter(w => !w.isWishList && !w.isRetired)
  return [...new Set(tabWatches.map(w => w.brand))].sort()
})

const filteredWatches = computed(() => {
  let watches = tab.value === 'wishlist'
    ? allWatches.value.filter(w => w.isWishList)
    : allWatches.value.filter(w => !w.isWishList && !w.isRetired)

  if (filterBrand.value) {
    watches = watches.filter(w => w.brand === filterBrand.value)
  }
  if (filterMovement.value) {
    watches = watches.filter(w => w.movementType === filterMovement.value)
  }

  // Sort
  switch (sortBy.value) {
    case 'brand':
      watches = [...watches].sort((a, b) => a.brand.localeCompare(b.brand))
      break
    case 'lastWorn':
      watches = [...watches].sort((a, b) => {
        if (!a.lastWornDate) return 1
        if (!b.lastWornDate) return -1
        return new Date(b.lastWornDate).getTime() - new Date(a.lastWornDate).getTime()
      })
      break
    case 'timesWorn':
      watches = [...watches].sort((a, b) => b.timesWorn - a.timesWorn)
      break
    default: // dateAdded
      watches = [...watches].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
  }

  return watches
})

// Filter panel
const showFilters = ref(false)
const hasActiveFilters = computed(() => filterBrand.value !== '' || filterMovement.value !== '' || sortBy.value !== 'dateAdded')

function clearFilters() {
  filterBrand.value = ''
  filterMovement.value = ''
  sortBy.value = 'dateAdded'
}

// Reset carousel index when tab or filters change
vueWatch([tab, filterBrand, filterMovement, sortBy], () => { currentIndex.value = 0 })

// Touch handling for swipe — lock axis to prevent vertical shift
let touchStartX = 0
let touchStartY = 0
let touchDeltaX = 0
let isHorizontalSwipe: boolean | null = null

function onTouchStart(e: TouchEvent) {
  touchStartX = e.touches[0].clientX
  touchStartY = e.touches[0].clientY
  touchDeltaX = 0
  isHorizontalSwipe = null
}

function onTouchMove(e: TouchEvent) {
  const dx = e.touches[0].clientX - touchStartX
  const dy = e.touches[0].clientY - touchStartY

  // Determine swipe direction on first significant movement
  if (isHorizontalSwipe === null && (Math.abs(dx) > 8 || Math.abs(dy) > 8)) {
    isHorizontalSwipe = Math.abs(dx) > Math.abs(dy)
  }

  if (isHorizontalSwipe) {
    e.preventDefault() // Prevent vertical scroll during horizontal swipe
    touchDeltaX = dx
  }
}

function onTouchEnd() {
  if (isHorizontalSwipe) {
    if (touchDeltaX < -50) next()
    else if (touchDeltaX > 50) prev()
  }
  touchDeltaX = 0
  isHorizontalSwipe = null
}

function next() {
  if (currentIndex.value < filteredWatches.value.length - 1) currentIndex.value++
}

function prev() {
  if (currentIndex.value > 0) currentIndex.value--
}

function onResize() { windowWidth.value = window.innerWidth }

// Pull-to-refresh
async function reload() {
  allWatches.value = await getWatches()
}
const { refreshing, pullDistance, pulling } = usePullToRefresh(reload)

onMounted(async () => {
  window.addEventListener('resize', onResize)
  try {
    allWatches.value = await getWatches()
  } finally {
    loading.value = false
  }
})

// Register touchmove as non-passive once the swipe element renders
vueWatch(swipeEl, (el) => {
  if (el) {
    el.addEventListener('touchmove', onTouchMove, { passive: false })
  }
})

onUnmounted(() => {
  window.removeEventListener('resize', onResize)
  if (swipeEl.value) {
    swipeEl.value.removeEventListener('touchmove', onTouchMove)
  }
})
</script>

<style scoped>
.filter-enter-active,
.filter-leave-active {
  transition: all 0.2s ease;
}
.filter-enter-from,
.filter-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
