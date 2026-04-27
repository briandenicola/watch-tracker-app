<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h2 class="font-display text-2xl font-semibold text-text">My Collection</h2>
      <span class="text-sm text-text-muted">{{ watches.length }} watches</span>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <!-- Empty State -->
    <div v-else-if="watches.length === 0" class="text-center py-20">
      <p class="text-5xl mb-4">⌚</p>
      <p class="text-text-secondary mb-4">Your collection is empty</p>
      <RouterLink to="/watches/new" class="inline-block px-5 py-2.5 bg-accent hover:bg-accent-hover text-bg font-medium rounded-lg transition-colors">
        Add Your First Watch
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
            v-for="watch in watches"
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
                <!-- Brand/Model overlay -->
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
        <span class="text-sm text-text-muted">{{ currentIndex + 1 }} / {{ watches.length }}</span>
        <button
          @click="next"
          :disabled="currentIndex === watches.length - 1"
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
        v-for="watch in watches"
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { Watch } from '@/types'
import { getWatches, imageUrl } from '@/services/watches'

const watches = ref<Watch[]>([])
const loading = ref(true)
const currentIndex = ref(0)
const windowWidth = ref(window.innerWidth)
const isDesktop = computed(() => windowWidth.value >= 1024)

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
  if (currentIndex.value < watches.value.length - 1) currentIndex.value++
}

function prev() {
  if (currentIndex.value > 0) currentIndex.value--
}

function onResize() { windowWidth.value = window.innerWidth }

onMounted(async () => {
  window.addEventListener('resize', onResize)
  try {
    const all = await getWatches()
    watches.value = all.filter(w => !w.isWishList && !w.isRetired)
  } finally {
    loading.value = false
  }
})

onUnmounted(() => window.removeEventListener('resize', onResize))
</script>
