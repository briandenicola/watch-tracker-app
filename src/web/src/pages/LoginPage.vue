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
      <p class="mt-6 text-center text-sm text-text-muted">
        No account?
        <RouterLink to="/register" class="text-accent hover:underline">Create one</RouterLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')

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
</script>
