<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-[80] flex items-end sm:items-center justify-center bg-black/60 p-0 sm:p-4"
      @click.self="emit('close')"
    >
      <div
        ref="dialogEl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="share-watch-title"
        tabindex="-1"
        class="w-full sm:max-w-lg max-h-[92vh] overflow-y-auto bg-bg-card border border-border rounded-t-2xl sm:rounded-2xl shadow-xl focus:outline-none"
        @keydown.esc="emit('close')"
      >
        <div class="p-5 space-y-4">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <p class="text-[0.65rem] uppercase tracking-[0.24em] text-accent mb-1">Share</p>
              <h2 id="share-watch-title" class="font-display text-xl font-semibold text-text truncate">
                {{ watchName }}
              </h2>
            </div>
            <button
              type="button"
              class="w-11 h-11 inline-flex items-center justify-center text-text-muted hover:text-text flex-shrink-0"
              aria-label="Close"
              @click="emit('close')"
            >
              <AppIcon name="close" :size="20" :stroke-width="2" />
            </button>
          </div>

          <div v-if="loading" class="flex justify-center py-10">
            <div class="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
          </div>

          <template v-else>
            <p v-if="error" class="text-sm text-danger">{{ error }}</p>

            <!-- Live link -->
            <template v-if="share">
              <p class="text-sm text-text-secondary">
                Anyone with this link can see the watch — no account needed.
              </p>

              <div class="flex gap-2">
                <input
                  ref="urlEl"
                  :value="url"
                  readonly
                  class="form-control flex-1 text-sm"
                  aria-label="Share link"
                  @focus="selectAll"
                />
                <button type="button" class="btn-accent flex-shrink-0" @click="copy">
                  {{ copied ? 'Copied' : 'Copy' }}
                </button>
              </div>
              <p v-if="copyHint" class="text-xs text-text-muted">{{ copyHint }}</p>

              <dl class="grid grid-cols-3 gap-2 text-center">
                <div class="rounded-lg border border-border bg-bg-surface p-2">
                  <dt class="text-[0.65rem] uppercase tracking-wide text-text-muted">Views</dt>
                  <dd class="text-sm text-text mt-0.5">{{ share.viewCount }}</dd>
                </div>
                <div class="rounded-lg border border-border bg-bg-surface p-2">
                  <dt class="text-[0.65rem] uppercase tracking-wide text-text-muted">Last viewed</dt>
                  <dd class="text-sm text-text mt-0.5">{{ share.lastViewedAt ? formatDate(share.lastViewedAt) : 'Never' }}</dd>
                </div>
                <div class="rounded-lg border border-border bg-bg-surface p-2">
                  <dt class="text-[0.65rem] uppercase tracking-wide text-text-muted">Created</dt>
                  <dd class="text-sm text-text mt-0.5">{{ formatDate(share.createdAt) }}</dd>
                </div>
              </dl>

              <div class="flex flex-wrap items-center gap-3">
                <a :href="url" target="_blank" rel="noopener" class="text-sm text-accent hover:underline">
                  Open the shared page
                </a>
                <button
                  type="button"
                  class="ml-auto text-sm text-danger hover:underline disabled:opacity-50"
                  :disabled="working"
                  @click="revoke"
                >
                  {{ working ? 'Revoking…' : 'Revoke link' }}
                </button>
              </div>
            </template>

            <!-- No link yet -->
            <template v-else>
              <p class="text-sm text-text-secondary">
                Create a link that shows this watch to anyone you send it to, whether or not they have an account.
              </p>
              <button type="button" class="btn-accent w-full" :disabled="working" @click="create">
                {{ working ? 'Creating…' : 'Create share link' }}
              </button>
            </template>

            <!-- What a visitor sees, either way -->
            <div class="rounded-lg border border-border bg-bg-surface p-3 space-y-2">
              <p class="text-xs text-text-secondary">
                <span class="text-text">Shared:</span> photos, brand and model, reference, case, dial, strap,
                movement, water resistance, and any product link.
              </p>
              <p class="text-xs text-text-secondary">
                <span class="text-text">Never shared:</span> what you paid, where you bought it, serial number,
                notes, AI analysis, resale values, storage location, wear history, and your account details.
              </p>
            </div>
          </template>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { WatchShare } from '@/types'
import AppIcon from '@/components/icons/AppIcon.vue'
import { createWatchShare, getWatchShare, revokeWatchShare, shareUrl } from '@/services/sharing'

const props = defineProps<{ watchId: number, watchName: string }>()
const emit = defineEmits<{ close: [] }>()

const share = ref<WatchShare | null>(null)
const loading = ref(true)
const working = ref(false)
const error = ref('')
const copied = ref(false)
const copyHint = ref('')
const dialogEl = ref<HTMLElement | null>(null)
const urlEl = ref<HTMLInputElement | null>(null)

const url = computed(() => (share.value ? shareUrl(share.value) : ''))

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

function selectAll() {
  urlEl.value?.select()
}

onMounted(async () => {
  dialogEl.value?.focus()
  try {
    share.value = await getWatchShare(props.watchId)
  } catch {
    error.value = 'Could not check whether this watch is already shared.'
  } finally {
    loading.value = false
  }
})

async function create() {
  working.value = true
  error.value = ''
  try {
    share.value = await createWatchShare(props.watchId)
  } catch {
    error.value = 'Could not create a share link.'
  } finally {
    working.value = false
  }
}

async function revoke() {
  if (!confirm('Revoke this link? Anyone who already has it will stop being able to open it.')) return
  working.value = true
  error.value = ''
  try {
    await revokeWatchShare(props.watchId)
    share.value = null
    copied.value = false
    copyHint.value = ''
  } catch {
    error.value = 'Could not revoke the link.'
  } finally {
    working.value = false
  }
}

async function copy() {
  copyHint.value = ''
  try {
    // Only available over HTTPS or on localhost, so this can genuinely fail.
    await navigator.clipboard.writeText(url.value)
    copied.value = true
    setTimeout(() => { copied.value = false }, 2000)
  } catch {
    selectAll()
    copyHint.value = 'Copying was blocked — the link is selected, so copy it with your keyboard.'
  }
}
</script>

<style scoped>
.form-control {
  width: 100%;
  padding: 0.6rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-bg-surface);
  color: var(--color-text);
}

.form-control:focus {
  border-color: var(--color-accent);
  outline: none;
}

.btn-accent {
  padding: 0.6rem 1rem;
  border-radius: 0.5rem;
  background: var(--color-accent);
  color: var(--color-bg);
  font-size: 0.875rem;
  font-weight: 500;
  transition: background-color 0.15s ease;
}

.btn-accent:hover:not(:disabled) {
  background: var(--color-accent-hover);
}

.btn-accent:disabled {
  opacity: 0.5;
}
</style>
