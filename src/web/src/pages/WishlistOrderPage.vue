<template>
  <div class="max-w-2xl mx-auto">
    <div class="flex items-start justify-between gap-4 mb-6">
      <div>
        <h1 class="font-display text-2xl font-semibold text-text">Arrange Wish List</h1>
        <p class="text-sm text-text-muted mt-1">Move watches into priority order, highest first.</p>
      </div>
      <RouterLink to="/?tab=wishlist" class="text-sm text-accent hover:underline">Cancel</RouterLink>
    </div>

    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="(watch, index) in watches"
        :key="watch.id"
        class="flex items-center gap-3 bg-bg-card border border-border rounded-xl p-3"
      >
        <span class="w-7 text-center font-display text-lg text-accent">{{ index + 1 }}</span>
        <div class="w-14 h-14 flex-shrink-0 rounded-lg bg-bg-surface overflow-hidden">
          <img
            v-if="watch.imageUrls.length"
            :src="imageUrl(watch.imageUrls[0].url)"
            :alt="`${watch.brand} ${watch.model}`"
            class="w-full h-full object-contain"
          />
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-text truncate">{{ watch.brand }}</p>
          <p class="text-xs text-text-muted truncate">{{ watch.model }}</p>
        </div>
        <div class="flex gap-1">
          <button
            type="button"
            :disabled="index === 0"
            class="move-button"
            :aria-label="`Move ${watch.brand} ${watch.model} up`"
            @click="move(index, -1)"
          >
            <span aria-hidden="true">↑</span>
          </button>
          <button
            type="button"
            :disabled="index === watches.length - 1"
            class="move-button"
            :aria-label="`Move ${watch.brand} ${watch.model} down`"
            @click="move(index, 1)"
          >
            <span aria-hidden="true">↓</span>
          </button>
        </div>
      </div>

      <p v-if="error" class="text-sm text-danger">{{ error }}</p>
      <button
        type="button"
        :disabled="saving || !changed"
        class="w-full min-h-12 mt-3 bg-accent hover:bg-accent-hover text-bg font-semibold rounded-lg disabled:opacity-50"
        @click="save"
      >
        {{ saving ? 'Saving...' : 'Save Priority Order' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import type { Watch } from '@/types'
import { getWatches, imageUrl, reorderWishlist } from '@/services/watches'

const router = useRouter()
const watches = ref<Watch[]>([])
const initialOrder = ref<number[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const changed = computed(() =>
  watches.value.some((watch, index) => watch.id !== initialOrder.value[index]),
)

function move(index: number, direction: -1 | 1) {
  const target = index + direction
  if (target < 0 || target >= watches.value.length) return
  const reordered = [...watches.value]
  ;[reordered[index], reordered[target]] = [reordered[target], reordered[index]]
  watches.value = reordered
}

async function save() {
  saving.value = true
  error.value = ''
  try {
    await reorderWishlist(watches.value.map(watch => watch.id))
    await router.push('/?tab=wishlist')
  } catch (requestError: any) {
    error.value = requestError?.response?.data?.error || 'Could not save the priority order.'
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  try {
    watches.value = (await getWatches())
      .filter(watch => watch.isWishList)
      .sort((a, b) =>
        (a.wishlistPriority ?? Number.MAX_SAFE_INTEGER) - (b.wishlistPriority ?? Number.MAX_SAFE_INTEGER)
        || new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    initialOrder.value = watches.value.map(watch => watch.id)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.move-button {
  display: inline-flex;
  width: 2.75rem;
  height: 2.75rem;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-bg-surface);
  color: var(--color-text);
  font-size: 1.25rem;
}

.move-button:disabled {
  opacity: 0.3;
}
</style>
