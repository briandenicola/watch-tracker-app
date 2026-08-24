<template>
  <div class="min-h-screen bg-bg text-text">
    <div class="max-w-4xl mx-auto px-4 py-8 sm:py-12">
      <div v-if="loading" class="flex justify-center py-24">
        <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
      </div>

      <div v-else-if="!wishlist" class="text-center py-24">
        <p class="font-display text-2xl font-semibold text-text mb-2">This link isn't available</p>
        <p class="text-sm text-text-muted">
          It may have been revoked by its owner, or the address may be incomplete.
        </p>
      </div>

      <template v-else>
        <header class="mb-8">
          <p class="text-xs uppercase tracking-[0.24em] text-accent mb-2">Wish List</p>
          <h1 class="font-display text-3xl sm:text-4xl font-semibold leading-tight">
            {{ wishlist.ownerName }}'s wish list
          </h1>
          <p class="text-sm text-text-muted mt-2">
            {{ wishlist.items.length }} {{ wishlist.items.length === 1 ? 'watch' : 'watches' }}, most wanted first.
          </p>
        </header>

        <p v-if="!wishlist.items.length" class="text-center py-16 text-text-muted">
          There is nothing on this list at the moment.
        </p>

        <ol v-else class="space-y-4">
          <li
            v-for="(item, index) in wishlist.items"
            :key="`${item.brand}-${item.model}-${index}`"
            class="detail-card flex flex-col sm:flex-row gap-4"
          >
            <div class="flex-shrink-0 w-full sm:w-40 h-40 rounded-lg bg-bg-surface flex items-center justify-center overflow-hidden">
              <img
                v-if="item.imageUrls.length"
                :src="imageUrl(item.imageUrls[0].url)"
                :alt="`${item.brand} ${item.model}`"
                class="max-w-full max-h-full object-contain"
              />
              <span v-else class="text-xs text-text-muted">No photo</span>
            </div>

            <div class="min-w-0 flex-1">
              <div class="flex items-baseline gap-2">
                <span class="text-xs text-text-muted tabular-nums">{{ index + 1 }}</span>
                <h2 class="font-display text-xl font-semibold text-text">{{ item.brand }} {{ item.model }}</h2>
              </div>

              <p v-if="item.targetPrice != null" class="text-sm text-accent font-medium mt-1">
                ${{ item.targetPrice.toFixed(2) }}
              </p>

              <p class="text-sm text-text-secondary mt-2">{{ describe(item) }}</p>

              <a
                v-if="item.linkUrl"
                :href="item.linkUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="inline-block text-sm text-accent hover:underline mt-2"
              >
                {{ item.linkText || 'Product page' }}
              </a>
            </div>
          </li>
        </ol>

        <footer class="mt-10 pt-5 border-t border-border text-center space-y-1">
          <p class="text-xs text-text-muted">
            Shared from a private WatchTracker collection. This page is read-only, and shows only what the owner chose
            to publish.
          </p>
          <RouterLink to="/" class="text-xs text-accent hover:underline">Open WatchTracker</RouterLink>
        </footer>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import type { SharedWishlist, SharedWishlistItem } from '@/types'
import { getSharedWishlist } from '@/services/sharing'
import { imageUrl } from '@/services/watches'

const route = useRoute()

const wishlist = ref<SharedWishlist | null>(null)
const loading = ref(true)

/** The spec line under each item, built from whatever that watch actually has. */
function describe(item: SharedWishlistItem): string {
  const parts = [
    item.caseSizeMm ? `${item.caseSizeMm} mm` : null,
    item.caseShape,
    item.dialColor ? `${item.dialColor} dial` : null,
    [item.bandColor, item.bandType].filter(Boolean).join(' ') || null,
    item.movementType,
    item.waterResistance,
    item.countryOfOrigin,
  ].filter(Boolean)

  return parts.length ? parts.join(' · ') : 'No details recorded.'
}

// A list shared with a person is not shared with a search engine.
const previousTitle = document.title
let robotsTag: HTMLMetaElement | null = null

function blockIndexing() {
  if (document.querySelector('meta[name="robots"]')) return
  robotsTag = document.createElement('meta')
  robotsTag.name = 'robots'
  robotsTag.content = 'noindex, nofollow'
  document.head.appendChild(robotsTag)
}

onMounted(async () => {
  blockIndexing()
  try {
    wishlist.value = await getSharedWishlist(String(route.params.token))
    if (wishlist.value) document.title = `${wishlist.value.ownerName}'s wish list`
  } catch {
    // Revoked, mistyped, or never existed — all the same to a visitor.
    wishlist.value = null
  } finally {
    loading.value = false
  }
})

onBeforeUnmount(() => {
  robotsTag?.remove()
  robotsTag = null
  document.title = previousTitle
})
</script>

<style scoped>
.detail-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  padding: 1rem;
}

@media (min-width: 768px) {
  .detail-card {
    padding: 1.25rem;
  }
}
</style>
