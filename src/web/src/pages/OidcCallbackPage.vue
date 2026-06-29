<template>
  <div class="min-h-dvh flex items-center justify-center px-6">
    <div class="w-full max-w-sm text-center">
      <h1 class="font-display text-2xl font-semibold text-accent mb-2">Signing you in</h1>
      <p class="text-text-secondary text-sm">{{ error || 'Completing OIDC login...' }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { AuthResponse } from '@/types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const error = ref('')

onMounted(async () => {
  const code = typeof route.query.code === 'string' ? route.query.code : ''
  const returnUrl = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : '/'

  if (!code) {
    error.value = 'OIDC login did not return a code.'
    return
  }

  try {
    const { data } = await api.post<AuthResponse>('/api/auth/oidc/exchange', { code })
    auth.setAuth(data)
    router.replace(returnUrl.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/')
  } catch {
    error.value = 'OIDC login failed. Please try again.'
  }
})
</script>
