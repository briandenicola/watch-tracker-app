<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Add Watch</h2>
    <WatchForm @submit="handleSubmit" :loading="loading" :existing-brands="brands" />
    <p v-if="error" class="text-danger text-sm mt-4">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { createWatch, getWatches } from '@/services/watches'
import WatchForm from '@/components/common/WatchForm.vue'
import type { CreateWatch } from '@/types'

const router = useRouter()
const loading = ref(false)
const error = ref('')
const brands = ref<string[]>([])

onMounted(async () => {
  try {
    const watches = await getWatches()
    brands.value = [...new Set(watches.map(w => w.brand))].sort()
  } catch { /* non-critical */ }
})

async function handleSubmit(data: CreateWatch) {
  loading.value = true
  error.value = ''
  try {
    const watch = await createWatch(data)
    router.push(`/watches/${watch.id}`)
  } catch (e: any) {
    error.value = e.response?.data?.error || 'Failed to create watch'
  } finally {
    loading.value = false
  }
}
</script>
