<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Add to Wish List</h2>
    <WatchForm mode="wishlist" @submit="handleSubmit" :loading="loading" :existing-brands="brands" />
    <p v-if="error" class="text-danger text-sm mt-4">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { createWatch, getWatches, uploadImage, importImageFromUrl } from '@/services/watches'
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

async function handleSubmit(data: CreateWatch, photo?: File, imageUrl?: string) {
  loading.value = true
  error.value = ''
  try {
    const watch = await createWatch({ ...data, isWishList: true })
    if (photo) {
      await uploadImage(watch.id, photo)
    } else if (imageUrl) {
      await importImageFromUrl(watch.id, imageUrl)
    }
    router.push('/?tab=wishlist')
  } catch (e: any) {
    error.value = e.response?.data?.error || 'Failed to add'
  } finally {
    loading.value = false
  }
}
</script>
