<template>
  <div class="min-h-dvh flex items-center justify-center px-6">
    <div class="w-full max-w-sm">
      <div class="text-center mb-8">
        <h1 class="font-display text-3xl font-semibold text-accent mb-2">Watch Tracker</h1>
        <p class="text-text-secondary text-sm">Sign in to your collection</p>
      </div>
      <form @submit.prevent="handleLogin" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-text-secondary mb-1">Email</label>
          <input
            v-model="email"
            type="email"
            required
            class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
            placeholder="your@email.com"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-text-secondary mb-1">Password</label>
          <input
            v-model="password"
            type="password"
            required
            class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
            placeholder="••••••••"
          />
        </div>
        <button
          type="submit"
          :disabled="loading"
          class="w-full py-3 bg-accent hover:bg-accent-hover text-bg font-semibold rounded-lg transition-colors disabled:opacity-50"
        >
          {{ loading ? 'Signing in...' : 'Sign In' }}
        </button>
        <p v-if="error" class="text-danger text-sm text-center">{{ error }}</p>
      </form>
      <div v-if="oidcProviders.length" class="mt-5 space-y-3">
        <div class="flex items-center gap-3">
          <div class="h-px bg-border flex-1" />
          <span class="text-xs text-text-muted">or</span>
          <div class="h-px bg-border flex-1" />
        </div>
        <button
          v-for="provider in oidcProviders"
          :key="provider.provider"
          type="button"
          @click="startOidcLogin(provider.provider)"
          class="w-full py-3 bg-bg-surface border border-border hover:border-accent/50 text-text font-medium rounded-lg transition-colors"
        >
          Continue with {{ provider.displayName }}
        </button>
      </div>
      <p class="mt-6 text-center text-sm text-text-muted">
        No account?
        <RouterLink to="/register" class="text-accent hover:underline">Create one</RouterLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { api } from '@/services/api'
import type { OidcProvider, OidcProviderPublic } from '@/types'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref(typeof route.query.oidcError === 'string' ? 'OIDC login failed. Please try again.' : '')
const oidcProviders = ref<OidcProviderPublic[]>([])

onMounted(async () => {
  try {
    const { data } = await api.get<OidcProviderPublic[]>('/api/auth/oidc/providers')
    oidcProviders.value = data
  } catch {
    oidcProviders.value = []
  }
})

async function handleLogin() {
  loading.value = true
  error.value = ''
  try {
    await auth.login({ email: email.value, password: password.value })
    router.push('/')
  } catch (e: any) {
    error.value = e.response?.data?.error || 'Login failed'
  } finally {
    loading.value = false
  }
}

async function startOidcLogin(provider: OidcProvider) {
  error.value = ''
  try {
    const { data } = await api.post<{ url: string }>(`/api/auth/oidc/${provider}/login-url?returnUrl=/`)
    window.location.href = data.url
  } catch {
    error.value = 'OIDC login failed to start'
  }
}
</script>
