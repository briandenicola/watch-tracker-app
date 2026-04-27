<template>
  <div>
    <PullToRefresh :pulling="pulling" :refreshing="refreshing" :pull-distance="pullDistance" />
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Retired Watches</h2>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else-if="error" class="text-center py-20">
      <p class="text-danger mb-2">Failed to load retired watches</p>
      <button @click="load" class="text-accent text-sm hover:underline">Retry</button>
    </div>

    <div v-else-if="watches.length === 0" class="text-center py-20">
      <p class="text-5xl mb-4">🎉</p>
      <p class="text-text-secondary">No retired watches</p>
      <p class="text-sm text-text-muted mt-1">All your watches are still in active rotation.</p>
    </div>

    <div v-else class="grid grid-cols-[repeat(auto-fill,minmax(260px,1fr))] gap-6">
      <div
        v-for="watch in watches"
        :key="watch.id"
        class="bg-bg-card border border-border rounded-xl overflow-hidden"
      >
        <div class="aspect-square bg-bg-surface overflow-hidden">
          <img
            v-if="watch.imageUrls.length > 0"
            :src="imageUrl(watch.imageUrls[0].url)"
            :alt="`${watch.brand} ${watch.model}`"
            class="w-full h-full object-contain p-4"
            loading="lazy"
          />
          <div v-else class="w-full h-full flex items-center justify-center text-5xl text-text-muted">⌚</div>
        </div>
        <div class="p-4">
          <p class="font-display text-lg font-medium text-text">{{ watch.brand }}</p>
          <p class="text-sm text-text-secondary">{{ watch.model }}</p>
          <p v-if="watch.retiredAt" class="text-xs text-text-muted mt-1">
            Retired {{ new Date(watch.retiredAt).toLocaleDateString() }}
          </p>
          <button
            @click="handleUnretire(watch.id)"
            :disabled="unretiring === watch.id"
            class="mt-3 w-full px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ unretiring === watch.id ? 'Restoring...' : 'Unretire' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { Watch } from '@/types'
import { imageUrl, unretireWatch } from '@/services/watches'
import { api } from '@/services/api'
import { usePullToRefresh } from '@/composables/usePullToRefresh'
import PullToRefresh from '@/components/common/PullToRefresh.vue'

const { refreshing, pullDistance, pulling } = usePullToRefresh(load)

const watches = ref<Watch[]>([])
const loading = ref(true)
const error = ref(false)
const unretiring = ref<number | null>(null)

async function load() {
  loading.value = true
  error.value = false
  try {
    const { data } = await api.get<Watch[]>('/api/watches?includeRetired=true')
    watches.value = data.filter(w => w.isRetired)
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

async function handleUnretire(id: number) {
  if (!confirm('Restore this watch to your active collection?')) return
  unretiring.value = id
  try {
    await unretireWatch(id)
    watches.value = watches.value.filter(w => w.id !== id)
  } finally {
    unretiring.value = null
  }
}

onMounted(load)
</script>
