<template>
  <div>
    <div class="flex items-start justify-between gap-4 mb-6">
      <div>
        <h2 class="font-display text-2xl font-semibold text-text">Storage</h2>
        <p class="text-sm text-text-muted mt-1">Watches grouped by where they are stored.</p>
      </div>
      <RouterLink to="/settings" class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors whitespace-nowrap">
        Manage
      </RouterLink>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else-if="collectionWatches.length === 0" class="text-center py-20">
      <p class="text-5xl mb-4">⌚</p>
      <p class="text-text-secondary mb-4">Your collection is empty</p>
      <RouterLink to="/watches/new" class="inline-block px-5 py-2.5 bg-accent hover:bg-accent-hover text-bg font-medium rounded-lg transition-colors">
        Add Your First Watch
      </RouterLink>
    </div>

    <div v-else class="space-y-8">
      <section
        v-for="group in groupedWatches"
        :key="group.location"
        class="bg-bg-card border border-border rounded-2xl overflow-hidden"
      >
        <div class="flex items-center justify-between gap-3 px-4 py-3 border-b border-border">
          <div>
            <h3 class="font-display text-lg font-semibold text-text">{{ group.location }}</h3>
            <p class="text-xs text-text-muted">{{ group.watches.length }} {{ group.watches.length === 1 ? 'watch' : 'watches' }}</p>
          </div>
          <RouterLink
            v-if="group.location === unassignedLabel"
            to="/settings"
            class="text-xs text-accent hover:underline whitespace-nowrap"
          >
            Define locations
          </RouterLink>
        </div>

        <div class="storage-shelf">
          <RouterLink
            v-for="(watch, index) in group.watches"
            :key="watch.id"
            :to="`/watches/${watch.id}`"
            class="watch-token group"
            :style="{ '--watch-size': `${watchSize(index)}px` }"
            :title="`${watch.brand} ${watch.model}`"
          >
            <img
              v-if="watch.imageUrls.length > 0"
              :src="imageUrl(watch.imageUrls[0].url)"
              :alt="`${watch.brand} ${watch.model}`"
              class="watch-image"
              loading="lazy"
            />
            <div v-else class="watch-placeholder">⌚</div>
            <span class="watch-label">
              <span class="font-medium">{{ watch.brand }}</span>
              <span class="text-white/70">{{ watch.model }}</span>
            </span>
          </RouterLink>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { AuthResponse, Watch } from '@/types'
import { api } from '@/services/api'
import { getWatches, imageUrl } from '@/services/watches'

const unassignedLabel = 'Unassigned'
const loading = ref(true)
const allWatches = ref<Watch[]>([])
const storageLocations = ref<string[]>([])

const collectionWatches = computed(() =>
  allWatches.value.filter(w => !w.isWishList && !w.isRetired)
)

const groupedWatches = computed(() => {
  const groups = new Map<string, Watch[]>()

  for (const location of storageLocations.value) {
    groups.set(location, [])
  }

  for (const watch of collectionWatches.value) {
    const location = watch.storageLocation || unassignedLabel
    groups.set(location, [...(groups.get(location) || []), watch])
  }

  return [...groups.entries()]
    .filter(([, watches]) => watches.length > 0)
    .map(([location, watches]) => ({
      location,
      watches: [...watches].sort((a, b) => `${a.brand} ${a.model}`.localeCompare(`${b.brand} ${b.model}`)),
    }))
})

function watchSize(index: number): number {
  const sizes = [124, 104, 138, 96, 112, 100, 130, 108]
  return sizes[index % sizes.length]
}

onMounted(async () => {
  try {
    const [watches, profileResp] = await Promise.all([
      getWatches(),
      api.get<AuthResponse>('/api/auth/me'),
    ])
    allWatches.value = watches
    storageLocations.value = profileResp.data.storageLocations || []
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.storage-shelf {
  min-height: 22rem;
  padding: 4rem 2.5rem;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
  align-items: center;
  justify-items: center;
  gap: 4rem 4.5rem;
  background:
    radial-gradient(circle at 20% 15%, rgba(255, 255, 255, 0.08), transparent 22rem),
    linear-gradient(180deg, rgba(30, 48, 82, 0.95), rgba(9, 24, 47, 0.98));
}

.watch-token {
  --watch-size: 80px;
  width: var(--watch-size);
  min-height: calc(var(--watch-size) + 2.25rem);
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 9999px;
  filter: drop-shadow(0 12px 12px rgba(0, 0, 0, 0.45));
}

.watch-image,
.watch-placeholder {
  width: var(--watch-size);
  height: var(--watch-size);
  border-radius: 9999px;
  object-fit: contain;
  padding: 0.2rem;
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.16);
  transition: transform 0.25s ease, border-color 0.25s ease;
}

.watch-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  color: rgba(255, 255, 255, 0.65);
  font-size: calc(var(--watch-size) * 0.48);
}

.watch-token:hover .watch-image,
.watch-token:hover .watch-placeholder {
  transform: translateY(-0.35rem) scale(1.08);
  border-color: rgba(212, 175, 55, 0.65);
}

.watch-label {
  position: absolute;
  top: calc(100% + 0.45rem);
  left: 50%;
  transform: translateX(-50%);
  width: max-content;
  max-width: 10rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.1rem;
  padding: 0.35rem 0.55rem;
  border-radius: 0.6rem;
  background: rgba(0, 0, 0, 0.52);
  color: white;
  font-size: 0.68rem;
  line-height: 1.1;
  text-align: center;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.2s ease;
}

.watch-token:hover .watch-label {
  opacity: 1;
}

@media (max-width: 640px) {
  .storage-shelf {
    min-height: 16rem;
    padding: 2.5rem 1rem;
    grid-template-columns: repeat(auto-fit, minmax(6.5rem, 1fr));
    gap: 2.75rem 1.75rem;
  }
}
</style>
