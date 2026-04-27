<template>
  <div>
    <!-- Tab Toggle + Filter Row -->
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
      <span class="text-sm text-text-muted">{{ filteredWatches.length }} watches</span>
    </div>

    <!-- Filter & Sort Controls -->
    <div class="flex flex-wrap items-center gap-2 mb-4">
      <select
        v-model="filterBrand"
        class="px-3 py-2 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
      >
        <option value="">All Brands</option>
        <option v-for="brand in brands" :key="brand" :value="brand">{{ brand }}</option>
      </select>
      <select
        v-model="filterMovement"
        class="px-3 py-2 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
      >
        <option value="">All Movements</option>
        <option value="Automatic">Automatic</option>
        <option value="Manual">Manual</option>
        <option value="Quartz">Quartz</option>
        <option value="Digital">Digital</option>
      </select>
      <select
        v-model="sortBy"
        class="px-3 py-2 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
      >
        <option value="dateAdded">Date Added</option>
        <option value="brand">Brand</option>
        <option value="lastWorn">Last Worn</option>
        <option value="timesWorn">Most Worn</option>
      </select>
    </div>

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
          class="flex transition-transform duration-300 ease-out"
          :style="{ transform: `translateX(-${currentIndex * 100}%)` }"
          @touchstart="onTouchStart"
          @touchmove="onTouchMove"
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
          <p class="text-xs text-text-muted mt-1">{{ watch.movementType }} · Worn {{ watch.timesWorn }}×</p>
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
import type { Watch } from '@/types'
import { getWatches, imageUrl } from '@/services/watches'

const allWatches = ref<Watch[]>([])
const loading = ref(true)
const tab = ref<'collection' | 'wishlist'>('collection')
const currentIndex = ref(0)
const windowWidth = ref(window.innerWidth)
const isDesktop = computed(() => windowWidth.value >= 1024)

// Filter & Sort state
const filterBrand = ref('')
const filterMovement = ref('')
const sortBy = ref('dateAdded')

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

function clearFilters() {
  filterBrand.value = ''
  filterMovement.value = ''
  sortBy.value = 'dateAdded'
}

// Reset carousel index when tab or filters change
vueWatch([tab, filterBrand, filterMovement, sortBy], () => { currentIndex.value = 0 })

// Touch handling for swipe
let touchStartX = 0
let touchDeltaX = 0

function onTouchStart(e: TouchEvent) {
  touchStartX = e.touches[0].clientX
}

function onTouchMove(e: TouchEvent) {
  touchDeltaX = e.touches[0].clientX - touchStartX
}

function onTouchEnd() {
  if (touchDeltaX < -50) next()
  else if (touchDeltaX > 50) prev()
  touchDeltaX = 0
}

function next() {
  if (currentIndex.value < filteredWatches.value.length - 1) currentIndex.value++
}

function prev() {
  if (currentIndex.value > 0) currentIndex.value--
}

function onResize() { windowWidth.value = window.innerWidth }

onMounted(async () => {
  window.addEventListener('resize', onResize)
  try {
    allWatches.value = await getWatches()
  } finally {
    loading.value = false
  }
})

onUnmounted(() => window.removeEventListener('resize', onResize))
</script>
