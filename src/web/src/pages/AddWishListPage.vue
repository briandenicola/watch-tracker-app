<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Add to Wish List</h2>
    <WatchForm mode="wishlist" @submit="handleSubmit" :loading="loading" :existing-brands="brands" :storage-locations="storageLocations" />
    <p v-if="error" class="text-danger text-sm mt-4">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { createWatch, getWatches, uploadImage, importImageFromUrl } from '@/services/watches'
import { api } from '@/services/api'
import WatchForm from '@/components/common/WatchForm.vue'
import type { AuthResponse, CreateWatch } from '@/types'

const router = useRouter()
const loading = ref(false)
const error = ref('')
const brands = ref<string[]>([])
const storageLocations = ref<string[]>([])

onMounted(async () => {
  try {
    const [watches, profileResp] = await Promise.all([
      getWatches(),
      api.get<AuthResponse>('/api/auth/me'),
    ])
    brands.value = [...new Set(watches.map(w => w.brand))].sort()
    storageLocations.value = profileResp.data.storageLocations || []
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
