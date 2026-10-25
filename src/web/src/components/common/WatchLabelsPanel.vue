<template>
  <section class="detail-card mb-6">
    <div class="mb-3 flex items-center justify-between gap-3">
      <h2 class="detail-heading !mb-0">Labels</h2>
      <button
        v-if="!editing"
        type="button"
        class="text-sm text-accent hover:text-accent-hover"
        @click="startEditing"
      >
        {{ current.length ? 'Edit' : 'Add labels' }}
      </button>
    </div>

    <template v-if="editing">
      <LabelPicker v-model="draft" allow-create aria-label="Labels for this watch" />
      <div class="mt-4 flex gap-2">
        <button
          type="button"
          :disabled="saving"
          class="rounded-lg bg-accent px-4 py-2 text-sm font-medium text-bg transition-colors hover:bg-accent-hover disabled:opacity-50"
          @click="save"
        >
          {{ saving ? 'Saving…' : 'Save labels' }}
        </button>
        <button
          type="button"
          :disabled="saving"
          class="rounded-lg border border-border px-4 py-2 text-sm text-text-secondary hover:text-text"
          @click="editing = false"
        >
          Cancel
        </button>
      </div>
      <p v-if="error" class="mt-2 text-sm text-danger">{{ error }}</p>
    </template>

    <div v-else-if="currentLabels.length" class="flex flex-wrap gap-2">
      <RouterLink
        v-for="label in currentLabels"
        :key="label.id"
        :to="{ path: '/', query: { tab: watch.isWishList ? 'wishlist' : 'collection', label: String(label.id) } }"
        :title="`Show everything labelled ${label.name}`"
      >
        <LabelChip :label="label" />
      </RouterLink>
    </div>
    <p v-else class="text-sm text-text-muted">No labels. Labels let you group watches and save filtered views of them.</p>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { Watch } from '@/types'
import { useLabelsStore } from '@/stores/labels'
import { serverMessage } from '@/utils/serverMessage'
import LabelChip from './LabelChip.vue'
import LabelPicker from './LabelPicker.vue'

const props = defineProps<{ watch: Watch }>()
const emit = defineEmits<{ updated: [labelIds: number[]] }>()

const store = useLabelsStore()
const editing = ref(false)
const saving = ref(false)
const error = ref('')
const draft = ref<number[]>([])

const current = computed(() => props.watch.labelIds ?? [])
const currentLabels = computed(() => current.value
  .map(id => store.labelsById.get(id))
  .filter(label => label !== undefined)
  .sort((a, b) => a.name.localeCompare(b.name)))

onMounted(() => { void store.load().catch(() => {}) })

function startEditing() {
  draft.value = [...current.value]
  error.value = ''
  editing.value = true
}

async function save() {
  saving.value = true
  error.value = ''
  try {
    emit('updated', await store.setWatchLabels(props.watch.id, current.value, draft.value))
    editing.value = false
  } catch (e) {
    error.value = serverMessage(e) ?? 'The labels could not be saved. Try again.'
  } finally {
    saving.value = false
  }
}
</script>
