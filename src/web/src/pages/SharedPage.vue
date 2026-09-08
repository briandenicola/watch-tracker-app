<template>
  <div class="max-w-5xl mx-auto px-4 py-6 sm:px-6">
    <header class="mb-6">
      <p class="text-xs uppercase tracking-[0.24em] text-accent mb-2">Shared with you</p>
      <h1 class="font-display text-3xl font-semibold text-text">Shared wish lists</h1>
      <p class="text-sm text-text-muted mt-2">Live, view-only wish lists other WatchTracker users shared with you.</p>
    </header>

    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-7 h-7 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>
    <p v-else-if="error" class="rounded-lg border border-danger/40 bg-danger/10 p-4 text-sm text-danger">{{ error }}</p>

    <template v-else-if="selected">
      <button type="button" class="text-sm text-accent hover:underline mb-5" @click="back">← All shared lists</button>
      <header class="mb-6">
        <h2 class="font-display text-2xl font-semibold text-text">{{ selected.ownerName }}'s wish list</h2>
        <p class="text-sm text-text-muted mt-1">{{ selected.items.length }} {{ selected.items.length === 1 ? 'watch' : 'watches' }}</p>
      </header>
      <p v-if="!selected.items.length" class="text-center py-12 text-text-muted">This wish list is empty.</p>
      <ol v-else class="space-y-4">
        <li v-for="(item, index) in selected.items" :key="`${item.brand}-${item.model}-${index}`" class="detail-card flex flex-col sm:flex-row gap-4">
          <div class="flex-shrink-0 w-full sm:w-36 h-36 rounded-lg bg-bg-surface flex items-center justify-center overflow-hidden">
            <img v-if="item.imageUrls.length" :src="imageUrl(item.imageUrls[0].url)" :alt="`${item.brand} ${item.model}`" class="max-w-full max-h-full object-contain" />
            <span v-else class="text-xs text-text-muted">No photo</span>
          </div>
          <div class="min-w-0 flex-1">
            <div class="flex items-baseline gap-2">
              <span class="text-xs text-text-muted">{{ index + 1 }}</span>
              <h3 class="font-display text-xl font-semibold text-text">{{ item.brand }} {{ item.model }}</h3>
            </div>
            <p v-if="item.targetPrice != null" class="text-sm text-accent font-medium mt-1">${{ item.targetPrice.toFixed(2) }}</p>
            <p class="text-sm text-text-secondary mt-2">{{ describe(item) }}</p>
            <a v-if="item.linkUrl" :href="item.linkUrl" target="_blank" rel="noopener noreferrer" class="inline-block text-sm text-accent hover:underline mt-2">
              {{ item.linkText || 'Product page' }}
            </a>
          </div>
        </li>
      </ol>
    </template>

    <p v-else-if="!shares.length" class="text-center py-16 text-text-muted">No one has shared a wish list with you yet.</p>
    <div v-else class="grid gap-4 sm:grid-cols-2">
      <button
        v-for="share in shares"
        :key="share.id"
        type="button"
        class="detail-card text-left hover:border-accent/60 transition-colors"
        @click="open(share.id)"
      >
        <p class="font-display text-xl font-semibold text-text">{{ share.ownerName }}'s wish list</p>
        <p class="text-sm text-text-muted mt-1">{{ share.itemCount }} {{ share.itemCount === 1 ? 'watch' : 'watches' }}</p>
        <p class="text-xs text-text-muted mt-3">Shared {{ formatDate(share.sharedAt) }} · {{ share.includesPrices ? 'Prices included' : 'Prices hidden' }}</p>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { ReceivedWishlistShare, SharedWishlist, SharedWishlistItem } from '@/types'
import { getReceivedWishlistShare, getReceivedWishlistShares } from '@/services/sharing'
import { imageUrl } from '@/services/watches'

const route = useRoute()
const router = useRouter()
const shares = ref<ReceivedWishlistShare[]>([])
const selected = ref<SharedWishlist | null>(null)
const loading = ref(true)
const error = ref('')

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

function describe(item: SharedWishlistItem) {
  return [
    item.caseSizeMm ? `${item.caseSizeMm} mm` : null,
    item.caseShape,
    item.dialColor ? `${item.dialColor} dial` : null,
    [item.bandColor, item.bandType].filter(Boolean).join(' ') || null,
    item.movementType,
  ].filter(Boolean).join(' · ') || 'No details recorded.'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const id = Number(route.params.id)
    if (id > 0) {
      selected.value = await getReceivedWishlistShare(id)
    } else {
      selected.value = null
      shares.value = await getReceivedWishlistShares()
    }
  } catch {
    error.value = 'This shared wish list is no longer available.'
  } finally {
    loading.value = false
  }
}

function open(id: number) {
  void router.push({ name: 'shared', params: { id } })
}

function back() {
  void router.push({ name: 'shared' })
}

onMounted(load)
watch(() => route.params.id, load)
</script>

<style scoped>
.detail-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  padding: 1rem;
}
</style>
