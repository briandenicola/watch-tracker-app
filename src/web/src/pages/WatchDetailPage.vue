<template>
  <div>
    <RouterLink to="/" class="text-accent text-sm hover:underline mb-4 inline-block">← Back</RouterLink>
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>
    <div v-else-if="watch">
      <!-- Image Gallery -->
      <div v-if="watch.imageUrls.length > 0" class="relative rounded-xl overflow-hidden bg-bg-surface mb-6">
        <div class="h-[300px] lg:h-[400px] flex items-center justify-center">
          <img
            :src="imageUrl(watch.imageUrls[imageIndex].url)"
            :alt="`${watch.brand} ${watch.model}`"
            class="max-w-full max-h-full object-contain"
          />
        </div>
        <div v-if="watch.imageUrls.length > 1" class="absolute bottom-3 inset-x-0 flex justify-center gap-1.5">
          <button
            v-for="(_, i) in watch.imageUrls"
            :key="i"
            @click="imageIndex = i"
            class="w-2 h-2 rounded-full transition-colors"
            :class="i === imageIndex ? 'bg-accent' : 'bg-white/40'"
          />
        </div>
      </div>

      <!-- Header -->
      <div class="mb-4">
        <h1 class="font-display text-2xl font-semibold text-text">{{ watch.brand }} {{ watch.model }}</h1>
        <p class="text-sm text-text-muted mt-1">{{ watch.movementType }} · Worn {{ watch.timesWorn }} times</p>
      </div>

      <!-- Actions -->
      <div class="flex flex-wrap gap-2 mb-6">
        <button @click="handleWear" :disabled="wearLoading" class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50">
          {{ wearLoading ? 'Recording...' : '⌚ Wore Today' }}
        </button>
        <RouterLink :to="`/watches/${watch.id}/edit`" class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors">
          Edit
        </RouterLink>
        <button @click="handleRetire" class="px-4 py-2 bg-bg-surface border border-border text-text-secondary text-sm rounded-lg hover:border-accent/50 transition-colors">
          Retire
        </button>
        <button @click="handleDelete" class="px-4 py-2 bg-bg-surface border border-danger/50 text-danger text-sm rounded-lg hover:bg-danger/10 transition-colors">
          Delete
        </button>
      </div>

      <!-- Details Chips -->
      <div class="flex flex-wrap gap-2 mb-6">
        <span v-if="watch.caseSizeMm" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.caseSizeMm }}mm case
        </span>
        <span v-if="watch.bandType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.bandType }} band
        </span>
        <span v-if="watch.dialColor" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.dialColor }} dial
        </span>
        <span v-if="watch.waterResistance" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.waterResistance }}
        </span>
        <span v-if="watch.purchasePrice" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          ${{ watch.purchasePrice.toFixed(2) }}
        </span>
      </div>

      <!-- Notes -->
      <div v-if="watch.notes" class="bg-bg-surface border border-border rounded-xl p-4">
        <h3 class="text-sm font-medium text-text-secondary mb-2">Notes</h3>
        <p class="text-sm text-text whitespace-pre-wrap">{{ watch.notes }}</p>
      </div>
    </div>
    <div v-else class="text-center py-20 text-text-muted">Watch not found.</div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { Watch } from '@/types'
import { getWatch, imageUrl, recordWear, retireWatch, deleteWatch } from '@/services/watches'

const route = useRoute()
const router = useRouter()
const watch = ref<Watch | null>(null)
const loading = ref(true)
const imageIndex = ref(0)
const wearLoading = ref(false)

onMounted(async () => {
  try {
    watch.value = await getWatch(Number(route.params.id))
  } finally {
    loading.value = false
  }
})

async function handleWear() {
  if (!watch.value) return
  wearLoading.value = true
  try {
    await recordWear(watch.value.id)
    watch.value.timesWorn++
  } finally {
    wearLoading.value = false
  }
}

async function handleRetire() {
  if (!watch.value || !confirm('Retire this watch?')) return
  await retireWatch(watch.value.id)
  router.push('/')
}

async function handleDelete() {
  if (!watch.value || !confirm('Delete this watch permanently?')) return
  await deleteWatch(watch.value.id)
  router.push('/')
}
</script>
