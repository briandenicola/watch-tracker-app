<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Add to Wish List</h2>
    <WatchForm mode="wishlist" @submit="handleSubmit" :loading="loading" />
    <p v-if="error" class="text-danger text-sm mt-4">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { createWatch } from '@/services/watches'
import WatchForm from '@/components/common/WatchForm.vue'
import type { CreateWatch } from '@/types'

const router = useRouter()
const loading = ref(false)
const error = ref('')

async function handleSubmit(data: CreateWatch) {
  loading.value = true
  error.value = ''
  try {
    await createWatch({ ...data, isWishList: true })
    router.push('/')
  } catch (e: any) {
    error.value = e.response?.data?.error || 'Failed to add'
  } finally {
    loading.value = false
  }
}
</script>
