<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Admin</h2>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else-if="error" class="text-center py-20">
      <p class="text-danger mb-2">Failed to load admin data</p>
      <button @click="load" class="text-accent text-sm hover:underline">Retry</button>
    </div>

    <div v-else class="space-y-8">
      <!-- User Management -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Users</h3>

        <div v-if="users.length === 0" class="text-sm text-text-muted">No users found.</div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-border text-left">
                <th class="pb-2 pr-4 text-text-secondary font-medium">Username</th>
                <th class="pb-2 pr-4 text-text-secondary font-medium">Email</th>
                <th class="pb-2 pr-4 text-text-secondary font-medium">Role</th>
                <th class="pb-2 pr-4 text-text-secondary font-medium">Status</th>
                <th class="pb-2 text-text-secondary font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="user in users" :key="user.id" class="border-b border-border-light">
                <td class="py-3 pr-4 text-text">{{ user.username }}</td>
                <td class="py-3 pr-4 text-text-secondary">{{ user.email }}</td>
                <td class="py-3 pr-4">
                  <span class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">{{ user.role }}</span>
                </td>
                <td class="py-3 pr-4">
                  <span v-if="user.isLockedOut" class="text-danger text-xs">Locked ({{ user.failedLoginAttempts }} attempts)</span>
                  <span v-else class="text-success text-xs">Active</span>
                </td>
                <td class="py-3">
                  <div class="flex gap-2">
                    <button
                      v-if="user.isLockedOut"
                      @click="handleUnlock(user.id)"
                      class="px-3 py-1.5 bg-bg-surface border border-border text-xs text-text rounded-lg hover:border-accent/50 transition-colors"
                    >
                      Unlock
                    </button>
                    <button
                      @click="handleResetPassword(user.id, user.username)"
                      class="px-3 py-1.5 bg-bg-surface border border-border text-xs text-text rounded-lg hover:border-accent/50 transition-colors"
                    >
                      Reset PW
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-if="userMsg" class="text-sm mt-3" :class="userMsg.includes('Error') ? 'text-danger' : 'text-success'">{{ userMsg }}</p>
      </section>

      <!-- App Settings -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">App Settings</h3>

        <div class="space-y-3">
          <div v-for="setting in settings" :key="setting.key" class="flex flex-col sm:flex-row gap-2">
            <label class="text-sm text-text-secondary w-48 flex-shrink-0 pt-3">{{ setting.key }}</label>
            <input
              v-model="setting.value"
              class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
            />
          </div>
        </div>
        <div class="flex items-center gap-3 mt-4">
          <button
            @click="handleSaveSettings"
            :disabled="savingSettings"
            class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ savingSettings ? 'Saving...' : 'Save Settings' }}
          </button>
          <p v-if="settingsMsg" class="text-sm" :class="settingsMsg.includes('Error') ? 'text-danger' : 'text-success'">{{ settingsMsg }}</p>
        </div>
      </section>

      <!-- Ollama Test -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Ollama Connection</h3>
        <div class="flex flex-wrap items-center gap-3">
          <input
            v-model="ollamaUrl"
            placeholder="Ollama URL (e.g., http://localhost:11434)"
            class="flex-1 min-w-[250px] px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
          />
          <button
            @click="handleTestOllama"
            :disabled="testingOllama"
            class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ testingOllama ? 'Testing...' : 'Test Connection' }}
          </button>
        </div>
        <div v-if="ollamaModels.length" class="mt-3">
          <p class="text-sm text-success mb-2">Connected — {{ ollamaModels.length }} model(s) available:</p>
          <div class="flex flex-wrap gap-2">
            <span v-for="m in ollamaModels" :key="m" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">{{ m }}</span>
          </div>
        </div>
        <p v-if="ollamaError" class="text-sm text-danger mt-2">{{ ollamaError }}</p>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { UserDto, AppSettingDto } from '@/types'
import { api } from '@/services/api'

const loading = ref(true)
const error = ref(false)
const users = ref<UserDto[]>([])
const settings = ref<AppSettingDto[]>([])
const userMsg = ref('')
const settingsMsg = ref('')
const savingSettings = ref(false)

// Ollama
const ollamaUrl = ref('')
const testingOllama = ref(false)
const ollamaModels = ref<string[]>([])
const ollamaError = ref('')

async function load() {
  loading.value = true
  error.value = false
  try {
    const [usersResp, settingsResp] = await Promise.all([
      api.get<UserDto[]>('/api/admin/users'),
      api.get<AppSettingDto[]>('/api/admin/settings'),
    ])
    users.value = usersResp.data
    settings.value = Array.isArray(settingsResp.data)
      ? settingsResp.data
      : Object.entries(settingsResp.data).map(([key, value]) => ({ key, value: String(value) }))
    const ollamaSetting = settings.value.find(s => s.key.toLowerCase().includes('ollama') && s.key.toLowerCase().includes('url'))
    if (ollamaSetting) ollamaUrl.value = ollamaSetting.value
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

async function handleUnlock(userId: number) {
  userMsg.value = ''
  try {
    await api.post(`/api/admin/users/${userId}/unlock`)
    const u = users.value.find(x => x.id === userId)
    if (u) { u.isLockedOut = false; u.failedLoginAttempts = 0 }
    userMsg.value = 'User unlocked'
  } catch {
    userMsg.value = 'Error unlocking user'
  }
}

async function handleResetPassword(userId: number, username: string) {
  const newPassword = prompt(`Enter new password for ${username} (min 8 characters):`)
  if (!newPassword || newPassword.length < 8) return
  userMsg.value = ''
  try {
    await api.post(`/api/admin/users/${userId}/reset-password`, { newPassword })
    userMsg.value = `Password reset for ${username}`
  } catch {
    userMsg.value = 'Error resetting password'
  }
}

async function handleSaveSettings() {
  savingSettings.value = true
  settingsMsg.value = ''
  try {
    await api.put('/api/admin/settings', settings.value)
    settingsMsg.value = 'Settings saved'
  } catch {
    settingsMsg.value = 'Error saving settings'
  } finally {
    savingSettings.value = false
  }
}

async function handleTestOllama() {
  testingOllama.value = true
  ollamaModels.value = []
  ollamaError.value = ''
  try {
    const { data } = await api.post<string[]>('/api/admin/ollama-models', { url: ollamaUrl.value })
    ollamaModels.value = data
  } catch {
    ollamaError.value = 'Failed to connect to Ollama'
  } finally {
    testingOllama.value = false
  }
}

onMounted(load)
</script>
