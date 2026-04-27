<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Edit Watch</h2>
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>
    <WatchForm v-else-if="watch" :initial="watch" @submit="handleSubmit" :loading="saving" :existing-brands="brands" />
    <p v-if="error" class="text-danger text-sm mt-4">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getWatch, getWatches, updateWatch } from '@/services/watches'
import WatchForm from '@/components/common/WatchForm.vue'
import type { Watch, UpdateWatch } from '@/types'

const route = useRoute()
const router = useRouter()
const watch = ref<Watch | null>(null)
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const brands = ref<string[]>([])

onMounted(async () => {
  try {
    const [w, allWatches] = await Promise.all([
      getWatch(Number(route.params.id)),
      getWatches(),
    ])
    watch.value = w
    brands.value = [...new Set(allWatches.map(w => w.brand))].sort()
  } finally {
    loading.value = false
  }
})

async function handleSubmit(data: UpdateWatch) {
  if (!watch.value) return
  saving.value = true
  error.value = ''
  try {
    await updateWatch(watch.value.id, data)
    router.push(`/watches/${watch.value.id}`)
  } catch (e: any) {
    error.value = e.response?.data?.error || 'Failed to update watch'
  } finally {
    saving.value = false
  }
}
</script>
