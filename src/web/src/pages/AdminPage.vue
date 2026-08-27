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

        <div class="space-y-6">
          <div v-for="group in groupedSettings" :key="group.label">
            <h4 class="text-xs font-semibold uppercase tracking-wide text-text-muted mb-2">{{ group.label }}</h4>
            <div class="space-y-3">
              <div v-for="setting in group.settings" :key="setting.key">
                <div class="flex flex-col sm:flex-row gap-2">
                  <label class="text-sm text-text-secondary w-48 flex-shrink-0 pt-3">{{ setting.key }}</label>
                  <select
                    v-if="setting.key === 'WebSearchProvider'"
                    v-model="setting.value"
                    class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors"
                  >
                    <option value="Brave">Brave</option>
                    <option value="SearXNG">SearXNG</option>
                  </select>
                  <template v-else-if="setting.key === 'ApplicationTimeZone'">
                    <input
                      v-model="setting.value"
                      list="application-timezones"
                      class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
                    />
                    <datalist id="application-timezones">
                      <option v-for="timeZone in commonTimeZones" :key="timeZone" :value="timeZone" />
                    </datalist>
                  </template>
                  <template v-else-if="setting.key === 'SearXngUrl'">
                    <input
                      v-model="setting.value"
                      class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
                    />
                    <button
                      type="button"
                      @click="handleTestSearXng(setting.value)"
                      :disabled="testingSearXng"
                      class="px-4 py-2 bg-bg-surface border border-border text-text text-sm rounded-lg hover:border-accent/50 transition-colors disabled:opacity-50 flex-shrink-0"
                    >
                      {{ testingSearXng ? 'Testing...' : 'Test Connection' }}
                    </button>
                  </template>
                  <template v-else-if="setting.key === 'OllamaUrl'">
                    <input
                      v-model="setting.value"
                      class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
                    />
                    <button
                      type="button"
                      @click="handleTestOllama(setting.value)"
                      :disabled="testingOllama || !setting.value.trim()"
                      class="px-4 py-2 bg-bg-surface border border-border text-text text-sm rounded-lg hover:border-accent/50 transition-colors disabled:opacity-50 flex-shrink-0"
                    >
                      {{ testingOllama ? 'Testing...' : 'Test' }}
                    </button>
                  </template>
                  <input
                    v-else
                    v-model="setting.value"
                    :placeholder="setting.key === 'ShareLinkBaseUrl' ? 'https://watches.example.com' : ''"
                    :type="setting.key === 'BraveSearchApiKey' || setting.key === 'EbayClientSecret' ? 'password' : 'text'"
                    class="flex-1 px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
                  />
                </div>
                <p v-if="setting.key === 'ShareLinkBaseUrl'" class="text-xs text-text-muted mt-1 sm:ml-[13rem]">
                  The address share links should use, for people outside your network. Leave blank to build links from
                  whichever address you are viewing the app on.
                </p>
                <p
                  v-if="setting.key === 'SearXngUrl' && searXngTestMsg"
                  class="text-xs mt-1 sm:ml-[13rem]"
                  :class="searXngTestSuccess ? 'text-success' : 'text-danger'"
                >
                  {{ searXngTestMsg }}
                </p>
                <div v-if="setting.key === 'OllamaUrl' && ollamaModels.length" class="mt-2 sm:ml-[13rem]">
                  <p class="text-sm text-success mb-2">Connected — {{ ollamaModels.length }} model(s) available:</p>
                  <div class="flex flex-wrap gap-2">
                    <span v-for="m in ollamaModels" :key="m" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">{{ m }}</span>
                  </div>
                </div>
                <p
                  v-if="setting.key === 'OllamaUrl' && ollamaError"
                  class="text-xs text-danger mt-1 sm:ml-[13rem]"
                >
                  {{ ollamaError }}
                </p>
              </div>
            </div>
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

      <!-- OIDC Providers -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">OIDC Providers</h3>

        <div class="space-y-5">
          <div v-for="provider in oidcProviders" :key="provider.provider" class="p-4 bg-bg-surface border border-border rounded-lg space-y-3">
            <div class="flex items-center justify-between gap-3">
              <div>
                <h4 class="text-text font-medium">{{ provider.displayName || provider.provider }}</h4>
                <p class="text-xs text-text-muted">{{ provider.provider }} · Secret {{ provider.hasClientSecret ? 'configured' : 'not configured' }}</p>
              </div>
              <label class="flex items-center gap-2 text-sm text-text-secondary">
                <input v-model="provider.enabled" type="checkbox" />
                Enabled
              </label>
            </div>

            <div class="grid gap-3 md:grid-cols-2">
              <input
                v-model="provider.displayName"
                placeholder="Display name"
                class="px-4 py-3 bg-bg-card border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
              />
              <input
                v-model="provider.clientId"
                placeholder="Client ID"
                class="px-4 py-3 bg-bg-card border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
              />
              <input
                v-model="provider.authority"
                placeholder="Authority URL"
                class="px-4 py-3 bg-bg-card border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors md:col-span-2"
              />
              <input
                v-model="provider.scopes"
                placeholder="Scopes"
                class="px-4 py-3 bg-bg-card border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
              />
              <input
                v-model="oidcSecrets[provider.provider]"
                type="password"
                placeholder="New client secret (leave blank to keep current)"
                class="px-4 py-3 bg-bg-card border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
              />
            </div>

            <div class="p-3 bg-bg-card border border-border rounded-lg">
              <label class="block text-xs font-medium text-text-secondary mb-2">Redirect URI to register with the provider</label>
              <code class="block text-sm text-text break-all">{{ oidcRedirectUrl(provider.provider) }}</code>
              <p class="text-xs text-text-muted mt-2">Use this as a Web redirect URI in Entra or Pocket ID.</p>
            </div>

            <div class="flex flex-wrap items-center gap-3">
              <button
                @click="handleSaveOidcProvider(provider)"
                :disabled="savingOidc === provider.provider"
                class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
              >
                {{ savingOidc === provider.provider ? 'Saving...' : 'Save Provider' }}
              </button>
              <button
                @click="handleTestOidcProvider(provider.provider)"
                :disabled="testingOidc === provider.provider"
                class="px-4 py-2 bg-bg-card border border-border text-text text-sm rounded-lg hover:border-accent/50 transition-colors disabled:opacity-50"
              >
                {{ testingOidc === provider.provider ? 'Testing...' : 'Test' }}
              </button>
              <p v-if="oidcMessages[provider.provider]" class="text-sm" :class="(oidcMessages[provider.provider] || '').includes('Error') ? 'text-danger' : 'text-success'">
                {{ oidcMessages[provider.provider] }}
              </p>
            </div>
          </div>
        </div>
      </section>

      <!-- Resale Values -->
      <section class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Resale Values</h3>
        <p class="text-xs text-text-muted mb-3">
          Runs a resale value refresh for every watch now, ignoring the scheduled interval. Requires at least
          one of the Web Search or eBay Pricing settings above, plus Ollama, to be configured.
        </p>
        <button
          @click="handleRefreshAllResale"
          :disabled="refreshingAllResale"
          class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
        >
          {{ refreshingAllResale ? 'Refreshing…' : 'Run Resale Refresh For All Watches' }}
        </button>
        <p v-if="resaleSummaryMsg" class="text-sm text-text-secondary mt-2">{{ resaleSummaryMsg }}</p>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { UserDto, AppSettingDto, OidcProvider, OidcProviderSettings, OidcProviderTestResult } from '@/types'
import { api } from '@/services/api'
import { setApplicationTimeZone } from '@/utils/dateTime'

const SETTING_GROUPS: { label: string; keys: string[] }[] = [
  { label: 'Regional Settings', keys: ['ApplicationTimeZone'] },
  { label: 'Sharing', keys: ['ShareLinkBaseUrl'] },
  { label: 'Ollama Configuration', keys: ['OllamaUrl', 'OllamaModel'] },
  { label: 'Web Search Configuration', keys: ['WebSearchProvider', 'BraveSearchApiKey', 'SearXngUrl'] },
  { label: 'eBay Pricing', keys: ['EbayClientId', 'EbayClientSecret'] },
  { label: 'Resale Configuration', keys: ['ResaleValueRefreshIntervalDays'] },
  { label: 'Price Monitoring', keys: ['PriceAlertScanIntervalHours'] },
  {
    label: 'Prompts',
    keys: [
      'ResaleValuePrompt',
      'CollectionAdvisorPrompt',
      'WatchRecommendationPrompt',
      'StyleAgentPrompt',
      'AiAnalysisPrompt',
    ],
  },
  { label: 'Security', keys: ['MaxFailedAttempts', 'LockoutDurationMinutes'] },
  { label: 'Logging', keys: ['LogLevel'] },
]

const commonTimeZones = [
  'UTC',
  'America/New_York',
  'America/Chicago',
  'America/Denver',
  'America/Phoenix',
  'America/Los_Angeles',
  'America/Anchorage',
  'Pacific/Honolulu',
  'Europe/London',
  'Europe/Paris',
  'Asia/Tokyo',
  'Australia/Sydney',
]

const loading = ref(true)
const error = ref(false)
const users = ref<UserDto[]>([])
const settings = ref<AppSettingDto[]>([])
const userMsg = ref('')
const settingsMsg = ref('')
const savingSettings = ref(false)
const oidcProviders = ref<OidcProviderSettings[]>([])
const oidcSecrets = ref<Partial<Record<OidcProvider, string>>>({})
const oidcMessages = ref<Partial<Record<OidcProvider, string>>>({})
const savingOidc = ref<OidcProvider | ''>('')
const testingOidc = ref<OidcProvider | ''>('')

const groupedSettings = computed(() => {
  const remaining = new Map(settings.value.map(s => [s.key, s]))
  const groups = SETTING_GROUPS.map(group => {
    const groupSettings = group.keys
      .filter(key => remaining.has(key))
      .map(key => {
        const setting = remaining.get(key)!
        remaining.delete(key)
        return setting
      })
    return { label: group.label, settings: groupSettings }
  }).filter(group => group.settings.length > 0)

  if (remaining.size > 0) {
    groups.push({ label: 'Other', settings: Array.from(remaining.values()) })
  }
  return groups
})

// Ollama
const testingOllama = ref(false)
const ollamaModels = ref<string[]>([])
const ollamaError = ref('')

// SearXNG
const testingSearXng = ref(false)
const searXngTestMsg = ref('')
const searXngTestSuccess = ref(false)

// Resale values
const refreshingAllResale = ref(false)
const resaleSummaryMsg = ref('')

function oidcRedirectUrl(provider: OidcProvider) {
  return `${window.location.origin}/api/auth/oidc/${provider}/complete`
}

async function load() {
  loading.value = true
  error.value = false
  try {
    const [usersResp, settingsResp, oidcResp] = await Promise.all([
      api.get<UserDto[]>('/api/admin/users'),
      api.get<AppSettingDto[]>('/api/admin/settings'),
      api.get<OidcProviderSettings[]>('/api/admin/oidc/providers'),
    ])
    users.value = usersResp.data
    settings.value = Array.isArray(settingsResp.data)
      ? settingsResp.data
      : Object.entries(settingsResp.data).map(([key, value]) => ({ key, value: String(value) }))
    oidcProviders.value = oidcResp.data
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
    const timeZone = settings.value.find(setting => setting.key === 'ApplicationTimeZone')
    if (timeZone) setApplicationTimeZone(timeZone.value)
    settingsMsg.value = 'Settings saved'
  } catch {
    settingsMsg.value = 'Error saving settings'
  } finally {
    savingSettings.value = false
  }
}

async function handleSaveOidcProvider(provider: OidcProviderSettings) {
  savingOidc.value = provider.provider
  oidcMessages.value[provider.provider] = ''
  try {
    const { data } = await api.put<OidcProviderSettings>(`/api/admin/oidc/providers/${provider.provider}`, {
      enabled: provider.enabled,
      displayName: provider.displayName,
      authority: provider.authority,
      clientId: provider.clientId,
      scopes: provider.scopes,
    })
    Object.assign(provider, data)

    const secret = oidcSecrets.value[provider.provider]
    if (secret) {
      await api.put(`/api/admin/oidc/providers/${provider.provider}/secret`, { clientSecret: secret })
      provider.hasClientSecret = true
      oidcSecrets.value[provider.provider] = ''
    }

    oidcMessages.value[provider.provider] = 'Provider saved'
  } catch {
    oidcMessages.value[provider.provider] = 'Error saving provider'
  } finally {
    savingOidc.value = ''
  }
}

async function handleTestOidcProvider(provider: OidcProvider) {
  testingOidc.value = provider
  oidcMessages.value[provider] = ''
  try {
    const { data } = await api.post<OidcProviderTestResult>(`/api/admin/oidc/providers/${provider}/test`)
    oidcMessages.value[provider] = data.success ? data.message : `Error: ${data.message}`
  } catch {
    oidcMessages.value[provider] = 'Error testing provider'
  } finally {
    testingOidc.value = ''
  }
}

async function handleTestOllama(url: string) {
  testingOllama.value = true
  ollamaModels.value = []
  ollamaError.value = ''
  try {
    const { data } = await api.post<string[]>('/api/admin/ollama/models', { url: url.trim() })
    ollamaModels.value = data
  } catch {
    ollamaError.value = 'Failed to connect to Ollama'
  } finally {
    testingOllama.value = false
  }
}

async function handleTestSearXng(url: string) {
  testingSearXng.value = true
  searXngTestMsg.value = ''
  try {
    const { data } = await api.post<{ success: boolean; message: string }>('/api/admin/searxng/test', { url })
    searXngTestSuccess.value = data.success
    searXngTestMsg.value = data.message
  } catch {
    searXngTestSuccess.value = false
    searXngTestMsg.value = 'Failed to test SearXNG connection'
  } finally {
    testingSearXng.value = false
  }
}

async function handleRefreshAllResale() {
  refreshingAllResale.value = true
  resaleSummaryMsg.value = ''
  try {
    const { data } = await api.post<{ message: string }>('/api/admin/resale-values/refresh-all')
    resaleSummaryMsg.value = data.message
  } catch {
    resaleSummaryMsg.value = 'Error queuing resale refresh'
  } finally {
    refreshingAllResale.value = false
  }
}

onMounted(load)
</script>
