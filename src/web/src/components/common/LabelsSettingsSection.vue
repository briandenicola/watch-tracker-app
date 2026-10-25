<template>
  <section class="bg-bg-card border border-border rounded-xl p-4">
    <h3 class="text-lg font-medium text-text mb-1">Labels &amp; Views</h3>
    <p class="mb-4 text-sm text-text-muted">
      Add labels from a watch's page. Save a view from the Collection filter panel, and pin up to {{ PIN_LIMIT }} views to the sidebar.
    </p>

    <p v-if="loadError" class="text-sm text-danger">{{ loadError }}</p>
    <template v-else>
      <h4 class="mb-2 text-xs font-medium uppercase tracking-wider text-text-secondary">Labels</h4>
      <p v-if="store.labels.length === 0" class="mb-4 text-sm text-text-muted">No labels yet.</p>
      <ul v-else class="mb-5 divide-y divide-border rounded-lg border border-border">
        <li v-for="label in store.labels" :key="label.id" class="flex items-center gap-3 px-3 py-2">
          <input
            type="color"
            :value="label.color"
            class="h-7 w-7 shrink-0 cursor-pointer rounded border border-border bg-transparent"
            :aria-label="`Color for ${label.name}`"
            @change="recolor(label, ($event.target as HTMLInputElement).value)"
          />
          <input
            v-if="renamingId === label.id"
            v-model="renameDraft"
            type="text"
            maxlength="50"
            class="min-w-0 flex-1 rounded-md border border-border bg-bg px-2 py-1 text-sm text-text focus:border-accent focus:outline-none"
            :aria-label="`New name for ${label.name}`"
            @keydown.enter.prevent="commitRename(label)"
            @keydown.esc="renamingId = null"
            @blur="commitRename(label)"
          />
          <button v-else type="button" class="min-w-0 flex-1 truncate text-left text-sm text-text" title="Rename" @click="startRename(label)">
            {{ label.name }}
          </button>
          <span class="shrink-0 text-xs text-text-muted">{{ label.watchCount }} {{ label.watchCount === 1 ? 'watch' : 'watches' }}</span>
          <button type="button" class="shrink-0 text-xs text-text-secondary hover:text-danger" @click="remove(label)">Delete</button>
        </li>
      </ul>

      <h4 class="mb-2 text-xs font-medium uppercase tracking-wider text-text-secondary">Saved views</h4>
      <p v-if="store.views.length === 0" class="text-sm text-text-muted">No saved views yet.</p>
      <ul v-else class="divide-y divide-border rounded-lg border border-border">
        <li v-for="view in store.views" :key="view.id" class="flex items-center gap-3 px-3 py-2">
          <RouterLink :to="{ path: '/', query: { view: String(view.id) } }" class="min-w-0 flex-1 truncate text-sm text-text hover:text-accent">
            {{ view.name }}
          </RouterLink>
          <span class="shrink-0 text-xs text-text-muted">{{ view.scope === 'Wishlist' ? 'Wish List' : 'Collection' }}</span>
          <button
            type="button"
            class="shrink-0 text-xs disabled:opacity-50"
            :class="view.pinned ? 'text-accent' : 'text-text-secondary hover:text-text'"
            :disabled="!view.pinned && store.pinLimitReached"
            :title="!view.pinned && store.pinLimitReached ? `You can pin up to ${PIN_LIMIT} views` : undefined"
            @click="run(() => store.setPinned(view.id, !view.pinned), 'The view could not be pinned.')"
          >
            {{ view.pinned ? 'Pinned' : 'Pin' }}
          </button>
          <button type="button" class="shrink-0 text-xs text-text-secondary hover:text-danger" @click="removeView(view)">Delete</button>
        </li>
      </ul>
      <p v-if="error" class="mt-3 text-sm text-danger">{{ error }}</p>
    </template>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import type { Label, SavedView } from '@/types'
import { PIN_LIMIT, useLabelsStore } from '@/stores/labels'
import { serverMessage } from '@/utils/serverMessage'

const store = useLabelsStore()
const loadError = ref('')
const error = ref('')
const renamingId = ref<number | null>(null)
const renameDraft = ref('')

onMounted(async () => {
  try {
    await store.load()
  } catch {
    loadError.value = 'Labels and views could not be loaded. Refresh the page to try again.'
  }
})

async function run(action: () => Promise<unknown>, failure: string) {
  error.value = ''
  try {
    await action()
  } catch (e) {
    error.value = serverMessage(e) ?? `${failure} Try again.`
  }
}

function startRename(label: Label) {
  renamingId.value = label.id
  renameDraft.value = label.name
}

function commitRename(label: Label) {
  if (renamingId.value !== label.id) return
  renamingId.value = null
  const name = renameDraft.value.trim()
  if (!name || name === label.name) return
  void run(() => store.updateLabel(label.id, { name, color: label.color }), 'The label could not be renamed.')
}

function recolor(label: Label, color: string) {
  void run(() => store.updateLabel(label.id, { name: label.name, color }), 'The color could not be saved.')
}

function remove(label: Label) {
  const used = label.watchCount ? ` It will be removed from ${label.watchCount} ${label.watchCount === 1 ? 'watch' : 'watches'}.` : ''
  if (!window.confirm(`Delete the label "${label.name}"?${used}`)) return
  void run(() => store.deleteLabel(label.id), 'The label could not be deleted.')
}

function removeView(view: SavedView) {
  if (!window.confirm(`Delete the view "${view.name}"? Your watches and labels are not affected.`)) return
  void run(() => store.deleteView(view.id), 'The view could not be deleted.')
}
</script>
