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
        aria-labelledby="share-wishlist-title"
        tabindex="-1"
        class="w-full sm:max-w-lg max-h-[92vh] overflow-y-auto bg-bg-card border border-border rounded-t-2xl sm:rounded-2xl shadow-xl focus:outline-none"
        @keydown.esc="emit('close')"
      >
        <div class="p-5 space-y-4">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <p class="text-[0.65rem] uppercase tracking-[0.24em] text-accent mb-1">Share</p>
              <h2 id="share-wishlist-title" class="font-display text-xl font-semibold text-text">Your wish list</h2>
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

            <div class="grid grid-cols-2 rounded-lg border border-border bg-bg-surface p-1">
              <button type="button" class="min-h-11 rounded-md px-3 py-2 text-sm" :class="mode === 'link' ? 'bg-bg-elevated text-accent' : 'text-text-muted'" @click="mode = 'link'">
                Share a URL
              </button>
              <button type="button" class="min-h-11 rounded-md px-3 py-2 text-sm" :class="mode === 'user' ? 'bg-bg-elevated text-accent' : 'text-text-muted'" @click="mode = 'user'">
                Share with a user
              </button>
            </div>

            <template v-if="mode === 'link' && share">
              <p class="text-sm text-text-secondary">
                Anyone with this link can see your wish list — no account needed. It always shows the list as it stands,
                in your priority order.
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

              <label class="flex items-start gap-2.5 cursor-pointer rounded-lg border border-border bg-bg-surface p-3">
                <input
                  type="checkbox"
                  class="mt-0.5 accent-accent"
                  :checked="share.includePrices"
                  :disabled="working"
                  @change="setPrices(($event.target as HTMLInputElement).checked)"
                />
                <span class="min-w-0">
                  <span class="block text-sm text-text">Show target prices</span>
                  <span class="block text-xs text-text-muted mt-0.5">
                    Useful when you are hinting. Off by default, since it publishes what you plan to spend.
                  </span>
                </span>
              </label>

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
                  Open the shared list
                </a>
                <button
                  type="button"
                  class="ml-auto text-sm text-danger hover:underline disabled:opacity-50"
                  :disabled="working"
                  @click="revoke"
                >
                  {{ working ? 'Working…' : 'Revoke link' }}
                </button>
              </div>
            </template>

            <template v-else-if="mode === 'link'">
              <p class="text-sm text-text-secondary">
                Create a link that shows your whole wish list to anyone you send it to, whether or not they have an
                account. Handy when someone asks what you are after.
              </p>

              <label class="flex items-start gap-2.5 cursor-pointer rounded-lg border border-border bg-bg-surface p-3">
                <input v-model="includePrices" type="checkbox" class="mt-0.5 accent-accent" />
                <span class="min-w-0">
                  <span class="block text-sm text-text">Show target prices</span>
                  <span class="block text-xs text-text-muted mt-0.5">
                    Off by default, since it publishes what you plan to spend. You can change this later.
                  </span>
                </span>
              </label>

              <button type="button" class="btn-accent w-full" :disabled="working" @click="create">
                {{ working ? 'Creating…' : 'Create share link' }}
              </button>
            </template>

            <template v-else>
              <p class="text-sm text-text-secondary">
                Give another WatchTracker user view-only access. They will find your live wish list on their Shared page.
              </p>

              <label class="flex items-start gap-2.5 cursor-pointer rounded-lg border border-border bg-bg-surface p-3">
                <input v-model="includePrices" type="checkbox" class="mt-0.5 accent-accent" />
                <span class="min-w-0">
                  <span class="block text-sm text-text">Show target prices</span>
                  <span class="block text-xs text-text-muted mt-0.5">This choice applies to the user you share with.</span>
                </span>
              </label>

              <form class="flex gap-2" @submit.prevent="searchUsers">
                <input v-model="userQuery" class="form-control flex-1 text-sm" minlength="2" maxlength="100" placeholder="Search by username" aria-label="Search by username" />
                <button type="submit" class="btn-accent flex-shrink-0" :disabled="working || userQuery.trim().length < 2">
                  Search
                </button>
              </form>

              <div v-if="userResults.length" class="space-y-2">
                <p class="text-xs uppercase tracking-wide text-text-muted">Results</p>
                <button
                  v-for="user in userResults"
                  :key="user.id"
                  type="button"
                  class="flex min-h-11 w-full items-center justify-between rounded-lg border border-border bg-bg-surface px-3 py-2 text-left hover:border-accent/60"
                  :disabled="working || userShares.some(existing => existing.recipientUserId === user.id)"
                  @click="shareWith(user)"
                >
                  <span class="text-sm text-text">{{ user.username }}</span>
                  <span class="text-xs text-accent">
                    {{ userShares.some(existing => existing.recipientUserId === user.id) ? 'Already shared' : 'Share' }}
                  </span>
                </button>
              </div>

              <div class="space-y-2">
                <p class="text-xs uppercase tracking-wide text-text-muted">People with access</p>
                <p v-if="!userShares.length" class="text-sm text-text-muted">No direct user shares yet.</p>
                <div v-for="userShare in userShares" :key="userShare.id" class="flex items-center justify-between gap-3 rounded-lg border border-border bg-bg-surface p-3">
                  <div class="min-w-0">
                    <p class="truncate text-sm text-text">{{ userShare.recipientUsername }}</p>
                    <p class="text-xs text-text-muted">
                      {{ userShare.includePrices ? 'Prices included' : 'Prices hidden' }} · {{ userShare.viewCount }} {{ userShare.viewCount === 1 ? 'view' : 'views' }}
                    </p>
                  </div>
                  <button type="button" class="min-h-11 px-2 text-sm text-danger hover:underline" :disabled="working" @click="revokeUser(userShare.id)">Remove</button>
                </div>
              </div>
            </template>

            <div class="rounded-lg border border-border bg-bg-surface p-3 space-y-2">
              <p class="text-xs text-text-secondary">
                <span class="text-text">Shared:</span> your display name, and for each item its photos, brand and model,
                reference, case, dial, strap, movement, water resistance and any product link.
              </p>
              <p class="text-xs text-text-secondary">
                <span class="text-text">Never shared:</span> your collection, what you paid for anything, notes, storage
                locations, wear history, and your account details. Target prices only if you turn them on.
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
import type { WishlistShare, WishlistShareUser, WishlistUserShare } from '@/types'
import AppIcon from '@/components/icons/AppIcon.vue'
import {
  createWishlistShare, getWishlistShare, revokeWishlistShare, shareUrl, updateWishlistShare,
  getWishlistUserShares, revokeWishlistUserShare, searchWishlistShareUsers, shareWishlistWithUser,
} from '@/services/sharing'

const emit = defineEmits<{ close: [] }>()

const share = ref<WishlistShare | null>(null)
const loading = ref(true)
const working = ref(false)
const error = ref('')
const copied = ref(false)
const copyHint = ref('')
const includePrices = ref(false)
const mode = ref<'link' | 'user'>('link')
const userQuery = ref('')
const userResults = ref<WishlistShareUser[]>([])
const userShares = ref<WishlistUserShare[]>([])
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
    const [linkShare, directShares] = await Promise.all([
      getWishlistShare(),
      getWishlistUserShares(),
    ])
    share.value = linkShare
    userShares.value = directShares
    includePrices.value = linkShare?.includePrices ?? false
  } catch {
    error.value = 'Could not check whether your wish list is already shared.'
  } finally {
    loading.value = false
  }
})

async function searchUsers() {
  working.value = true
  error.value = ''
  try {
    userResults.value = await searchWishlistShareUsers(userQuery.value.trim())
  } catch {
    error.value = 'Could not search for users.'
  } finally {
    working.value = false
  }
}

async function shareWith(user: WishlistShareUser) {
  working.value = true
  error.value = ''
  try {
    const created = await shareWishlistWithUser(user.id, includePrices.value)
    userShares.value = [...userShares.value.filter(existing => existing.id !== created.id), created]
      .sort((left, right) => left.recipientUsername.localeCompare(right.recipientUsername))
  } catch {
    error.value = 'Could not share with that user.'
  } finally {
    working.value = false
  }
}

async function revokeUser(shareId: number) {
  if (!confirm('Remove this user’s access to your wish list?')) return
  working.value = true
  error.value = ''
  try {
    await revokeWishlistUserShare(shareId)
    userShares.value = userShares.value.filter(share => share.id !== shareId)
  } catch {
    error.value = 'Could not remove that user’s access.'
  } finally {
    working.value = false
  }
}

async function create() {
  working.value = true
  error.value = ''
  try {
    share.value = await createWishlistShare(includePrices.value)
  } catch {
    error.value = 'Could not create a share link.'
  } finally {
    working.value = false
  }
}

async function setPrices(next: boolean) {
  working.value = true
  error.value = ''
  try {
    // The link itself is unchanged, so anyone already holding it sees the change.
    share.value = await updateWishlistShare(next)
  } catch {
    error.value = 'Could not change that setting.'
  } finally {
    working.value = false
  }
}

async function revoke() {
  if (!confirm('Revoke this link? Anyone who already has it will stop being able to open it.')) return
  working.value = true
  error.value = ''
  try {
    await revokeWishlistShare()
    share.value = null
    includePrices.value = false
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
