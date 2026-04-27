<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Settings</h2>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else class="space-y-8">
      <!-- Profile Section -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Profile</h3>

        <!-- Avatar -->
        <div class="flex items-center gap-4 mb-4">
          <div class="w-16 h-16 rounded-full bg-bg-surface border border-border overflow-hidden flex items-center justify-center">
            <img v-if="profileImage" :src="profileImage" class="w-full h-full object-cover" />
            <span v-else class="text-2xl text-text-muted">👤</span>
          </div>
          <div class="flex gap-2">
            <label class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors cursor-pointer">
              Upload Photo
              <input type="file" accept="image/*" class="hidden" @change="handleAvatarUpload" />
            </label>
            <button
              v-if="profileImage"
              @click="handleAvatarDelete"
              class="px-4 py-2 bg-bg-surface border border-danger/50 text-danger text-sm rounded-lg hover:bg-danger/10 transition-colors"
            >
              Remove
            </button>
          </div>
        </div>
        <p v-if="avatarMsg" class="text-sm mb-3" :class="avatarMsg.includes('Error') ? 'text-danger' : 'text-success'">{{ avatarMsg }}</p>

        <!-- Username -->
        <form @submit.prevent="handleUsernameChange" class="flex gap-2">
          <input
            v-model="newUsername"
            placeholder="New username"
            class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
          />
          <button
            type="submit"
            :disabled="!newUsername || savingUsername"
            class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ savingUsername ? 'Saving...' : 'Update' }}
          </button>
        </form>
        <p v-if="usernameMsg" class="text-sm mt-2" :class="usernameMsg.includes('Error') ? 'text-danger' : 'text-success'">{{ usernameMsg }}</p>
      </section>

      <!-- Appearance Section -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Appearance</h3>
        <div class="flex gap-2">
          <button
            v-for="opt in themeOptions"
            :key="opt.value"
            @click="theme.setTheme(opt.value)"
            class="flex-1 px-4 py-3 border rounded-lg text-sm font-medium transition-colors text-center"
            :class="theme.mode.value === opt.value
              ? 'border-accent bg-accent/10 text-accent'
              : 'border-border bg-bg-surface text-text-secondary hover:border-accent/50'"
          >
            {{ opt.label }}
          </button>
        </div>
      </section>

      <!-- Password Section -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Change Password</h3>
        <form @submit.prevent="handlePasswordChange" class="space-y-3">
          <input
            v-model="currentPassword"
            type="password"
            placeholder="Current password"
            class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
          />
          <input
            v-model="newPassword"
            type="password"
            placeholder="New password (min 8 characters)"
            class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
          />
          <div class="flex items-center gap-3">
            <button
              type="submit"
              :disabled="!currentPassword || newPassword.length < 8 || savingPassword"
              class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
            >
              {{ savingPassword ? 'Changing...' : 'Change Password' }}
            </button>
            <p v-if="passwordMsg" class="text-sm" :class="passwordMsg.includes('Error') ? 'text-danger' : 'text-success'">{{ passwordMsg }}</p>
          </div>
        </form>
      </section>

      <!-- API Keys Section -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">API Keys</h3>

        <div v-if="newlyCreatedKey" class="mb-4 p-3 bg-success/10 border border-success/30 rounded-lg">
          <p class="text-sm text-success font-medium mb-1">Key created — copy it now, it won't be shown again:</p>
          <code class="text-sm text-text break-all">{{ newlyCreatedKey }}</code>
        </div>

        <div v-if="apiKeys.length === 0" class="text-sm text-text-muted mb-4">No API keys yet.</div>
        <div v-else class="space-y-2 mb-4">
          <div v-for="key in apiKeys" :key="key.id" class="flex items-center justify-between p-3 bg-bg-surface border border-border rounded-lg">
            <div>
              <p class="text-sm text-text">{{ key.name }}</p>
              <p class="text-xs text-text-muted">{{ key.prefix }}… · Created {{ new Date(key.createdAt).toLocaleDateString() }}</p>
            </div>
            <button
              @click="handleDeleteApiKey(key.id)"
              class="px-3 py-1.5 text-danger text-xs border border-danger/50 rounded-lg hover:bg-danger/10 transition-colors"
            >
              Revoke
            </button>
          </div>
        </div>

        <form @submit.prevent="handleCreateApiKey" class="flex gap-2">
          <input
            v-model="newKeyName"
            placeholder="Key name"
            class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
          />
          <button
            type="submit"
            :disabled="!newKeyName || creatingKey"
            class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ creatingKey ? 'Creating...' : 'Create Key' }}
          </button>
        </form>
      </section>

      <!-- Data Export/Import Section -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Data</h3>
        <div class="flex flex-wrap gap-3">
          <button
            @click="handleExport"
            :disabled="exporting"
            class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ exporting ? 'Exporting...' : 'Export Data (JSON)' }}
          </button>
          <label class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors cursor-pointer">
            {{ importing ? 'Importing...' : 'Import Data' }}
            <input type="file" accept=".json" class="hidden" @change="handleImport" :disabled="importing" />
          </label>
        </div>
        <p v-if="dataMsg" class="text-sm mt-3" :class="dataMsg.includes('Error') ? 'text-danger' : 'text-success'">{{ dataMsg }}</p>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { ApiKey } from '@/types'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useTheme, type ThemeMode } from '@/stores/theme'

const authStore = useAuthStore()
const theme = useTheme()
const loading = ref(true)
const profileImage = ref<string | null>(null)

const themeOptions: { value: ThemeMode; label: string }[] = [
  { value: 'light', label: 'Light' },
  { value: 'dark', label: 'Dark' },
  { value: 'system', label: 'System' },
]

// Profile
const newUsername = ref('')
const savingUsername = ref(false)
const usernameMsg = ref('')
const avatarMsg = ref('')

// Password
const currentPassword = ref('')
const newPassword = ref('')
const savingPassword = ref(false)
const passwordMsg = ref('')

// API Keys
const apiKeys = ref<ApiKey[]>([])
const newKeyName = ref('')
const creatingKey = ref(false)
const newlyCreatedKey = ref('')

// Data
const exporting = ref(false)
const importing = ref(false)
const dataMsg = ref('')

onMounted(async () => {
  try {
    const [meResp, keysResp] = await Promise.all([
      api.get<{ username: string; email: string; profileImage?: string }>('/api/auth/me'),
      api.get<ApiKey[]>('/api/apikeys'),
    ])
    newUsername.value = meResp.data.username
    profileImage.value = meResp.data.profileImage || null
    apiKeys.value = keysResp.data
  } finally {
    loading.value = false
  }
})

async function handleAvatarUpload(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  avatarMsg.value = ''
  const form = new FormData()
  form.append('file', file)
  try {
    const { data } = await api.post<{ profileImage: string }>('/api/auth/profile-image', form)
    profileImage.value = data.profileImage
    if (authStore.user) authStore.user.profileImage = data.profileImage
    avatarMsg.value = 'Photo updated'
  } catch {
    avatarMsg.value = 'Error uploading photo'
  }
}

async function handleAvatarDelete() {
  if (!confirm('Remove your profile photo?')) return
  avatarMsg.value = ''
  try {
    await api.delete('/api/auth/profile-image')
    profileImage.value = null
    if (authStore.user) authStore.user.profileImage = undefined
    avatarMsg.value = 'Photo removed'
  } catch {
    avatarMsg.value = 'Error removing photo'
  }
}

async function handleUsernameChange() {
  if (!newUsername.value) return
  savingUsername.value = true
  usernameMsg.value = ''
  try {
    await api.post('/api/auth/username', { username: newUsername.value })
    if (authStore.user) authStore.user.username = newUsername.value
    usernameMsg.value = 'Username updated'
  } catch {
    usernameMsg.value = 'Error updating username'
  } finally {
    savingUsername.value = false
  }
}

async function handlePasswordChange() {
  savingPassword.value = true
  passwordMsg.value = ''
  try {
    await api.post('/api/auth/change-password', { currentPassword: currentPassword.value, newPassword: newPassword.value })
    passwordMsg.value = 'Password changed'
    currentPassword.value = ''
    newPassword.value = ''
  } catch {
    passwordMsg.value = 'Error — check your current password'
  } finally {
    savingPassword.value = false
  }
}

async function handleCreateApiKey() {
  creatingKey.value = true
  newlyCreatedKey.value = ''
  try {
    const { data } = await api.post<{ id: number; name: string; key: string; prefix: string; createdAt: string }>('/api/apikeys', { name: newKeyName.value })
    newlyCreatedKey.value = data.key
    apiKeys.value.push({ id: data.id, name: data.name, prefix: data.prefix, createdAt: data.createdAt })
    newKeyName.value = ''
  } catch {
    /* silent */
  } finally {
    creatingKey.value = false
  }
}

async function handleDeleteApiKey(id: number) {
  if (!confirm('Revoke this API key? This cannot be undone.')) return
  try {
    await api.delete(`/api/apikeys/${id}`)
    apiKeys.value = apiKeys.value.filter(k => k.id !== id)
  } catch {
    /* silent */
  }
}

async function handleExport() {
  exporting.value = true
  dataMsg.value = ''
  try {
    const { data } = await api.get('/api/data/export', { responseType: 'blob' })
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `watch-collection-${new Date().toISOString().slice(0, 10)}.json`
    a.click()
    URL.revokeObjectURL(url)
    dataMsg.value = 'Export downloaded'
  } catch {
    dataMsg.value = 'Error exporting data'
  } finally {
    exporting.value = false
  }
}

async function handleImport(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file || !confirm('Import data? This may overwrite existing data.')) return
  importing.value = true
  dataMsg.value = ''
  const form = new FormData()
  form.append('file', file)
  try {
    await api.post('/api/data/import', form)
    dataMsg.value = 'Import successful'
  } catch {
    dataMsg.value = 'Error importing data'
  } finally {
    importing.value = false
    ;(e.target as HTMLInputElement).value = ''
  }
}
</script>
