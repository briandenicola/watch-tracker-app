<template>
  <section class="mb-4 rounded-xl border border-border bg-bg-card px-4 py-3" aria-label="WatchPatrol connection">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <p class="text-sm font-medium text-text">
          WatchPatrol
          <span class="text-text-muted">
            {{ statusFailed ? 'Status unavailable' : busy ? 'Checking...' : connected ? 'Connected' : 'Not connected' }}
          </span>
        </p>
        <p class="mt-1 text-xs text-text-muted">
          Free, interactive research using your account. No scheduled scans, alerts or starred-listing changes.
          Lookup details are sent to WatchPatrol only when the Advisor uses its tools.
        </p>
      </div>
      <button
        type="button"
        class="min-h-11 rounded-lg border border-border px-3 py-2 text-xs font-medium text-accent disabled:opacity-50"
        :disabled="busy || statusFailed"
        @click="connected ? disconnect() : connect()"
      >
        {{ busy ? 'Please wait...' : connected ? 'Disconnect WatchPatrol' : 'Connect WatchPatrol' }}
      </button>
    </div>
    <p v-if="message" class="mt-2 text-xs" :class="failed ? 'text-danger' : 'text-text-muted'" role="status">
      {{ message }}
    </p>
    <button v-if="statusFailed" type="button" class="mt-2 min-h-11 text-xs text-accent" :disabled="busy" @click="loadStatus">
      Retry connection status
    </button>
  </section>
</template>

<script setup lang="ts">
import axios from 'axios'
import { onMounted, ref } from 'vue'
import {
  completeWatchPatrol,
  connectWatchPatrol,
  disconnectWatchPatrol,
  getWatchPatrolStatus,
} from '@/services/watchPatrol'

const stateKey = 'watchtracker.watchpatrol.oauth-state'
const connected = ref(false)
const busy = ref(true)
const failed = ref(false)
const statusFailed = ref(false)
const message = ref('')

function showError(error: unknown, fallback: string) {
  failed.value = true
  message.value = axios.isAxiosError(error) && typeof error.response?.data?.error === 'string'
    ? error.response.data.error : fallback
}

async function loadStatus() {
  busy.value = true
  if (statusFailed.value) {
    failed.value = false
    message.value = ''
  }
  statusFailed.value = false
  try {
    connected.value = (await getWatchPatrolStatus()).connected
  } catch (error: unknown) {
    statusFailed.value = true
    showError(error, 'Unable to check your WatchPatrol connection.')
  } finally {
    busy.value = false
  }
}

async function connect() {
  busy.value = true
  failed.value = false
  message.value = ''
  try {
    const url = new URL(await connectWatchPatrol(`${window.location.origin}/advisor`))
    if (url.origin !== 'https://www.watchpatrol.net' || url.pathname !== '/oauth/authorize/')
      throw new Error('Unexpected authorization URL')
    const state = url.searchParams.get('state')
    if (!state) throw new Error('Missing authorization state')
    sessionStorage.setItem(stateKey, state)
    window.location.assign(url.href)
  } catch (error: unknown) {
    showError(error, 'Unable to start WatchPatrol sign-in. Try again.')
    busy.value = false
  }
}

async function disconnect() {
  busy.value = true
  failed.value = false
  message.value = ''
  try {
    await disconnectWatchPatrol()
    sessionStorage.removeItem(stateKey)
    connected.value = false
    message.value = 'Disconnected from this app. You can also revoke access in your WatchPatrol account.'
  } catch (error: unknown) {
    showError(error, 'Unable to disconnect WatchPatrol.')
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  const url = new URL(window.location.href)
  const state = url.searchParams.get('state')
  const code = url.searchParams.get('code')
  const authorizationError = url.searchParams.get('error')
  if (code || authorizationError || state) {
    for (const key of ['code', 'state', 'error', 'error_description'])
      url.searchParams.delete(key)
    window.history.replaceState(window.history.state, '', `${url.pathname}${url.search}${url.hash}`)
    try {
      const expected = sessionStorage.getItem(stateKey)
      sessionStorage.removeItem(stateKey)
      if (!expected || state !== expected)
        throw new Error('Authorization state mismatch')
      if (authorizationError || !code) {
        failed.value = true
        message.value = 'WatchPatrol sign-in was cancelled or denied. Connect again when ready.'
      } else {
        await completeWatchPatrol(code, expected)
        message.value = 'WatchPatrol connected. Ask the Advisor to research pricing, listings or sellers.'
      }
    } catch (error: unknown) {
      showError(error, 'Unable to complete WatchPatrol sign-in. Connect again.')
    }
  }
  await loadStatus()
})
</script>
