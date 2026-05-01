<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Edit Wish List Item</h2>
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>
    <WatchForm v-else-if="watch" :initial="watch" mode="wishlist" @submit="handleSubmit" :loading="saving" />
    <p v-if="error" class="text-danger text-sm mt-4">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getWatch, updateWatch } from '@/services/watches'
import WatchForm from '@/components/common/WatchForm.vue'
import type { Watch, UpdateWatch } from '@/types'

const route = useRoute()
const router = useRouter()
const watch = ref<Watch | null>(null)
const loading = ref(true)
const saving = ref(false)
const error = ref('')

onMounted(async () => {
  try {
    watch.value = await getWatch(Number(route.params.id))
  } finally {
    loading.value = false
  }
})

async function handleSubmit(data: UpdateWatch) {
  if (!watch.value) return
  saving.value = true
  error.value = ''
  try {
    await updateWatch(watch.value.id, { ...data, isWishList: true })
    router.push('/')
  } catch (e: any) {
    error.value = e.response?.data?.error || 'Failed to update'
  } finally {
    saving.value = false
  }
}
</script>
