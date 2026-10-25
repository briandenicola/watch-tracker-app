<template>
  <div>
    <div class="flex flex-wrap gap-2" role="group" :aria-label="ariaLabel">
      <button
        v-for="label in store.labels"
        :key="label.id"
        type="button"
        class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium transition-colors"
        :class="selected.has(label.id) ? 'text-text' : 'border-border text-text-secondary hover:text-text'"
        :style="selected.has(label.id) ? { borderColor: label.color, backgroundColor: `color-mix(in srgb, ${label.color} 22%, transparent)` } : undefined"
        :aria-pressed="selected.has(label.id)"
        @click="toggle(label.id)"
      >
        <span class="h-2 w-2 rounded-full" :style="{ backgroundColor: label.color }" aria-hidden="true" />
        {{ label.name }}
      </button>
      <p v-if="store.labels.length === 0 && !allowCreate" class="text-xs text-text-muted">
        No labels yet. Add them from a watch's page or in Settings.
      </p>
    </div>
    <form v-if="allowCreate" class="mt-3 flex gap-2" @submit.prevent="create">
      <input
        v-model="newName"
        type="text"
        maxlength="50"
        placeholder="New label"
        aria-label="New label name"
        class="min-w-0 flex-1 rounded-lg border border-border bg-bg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:border-accent focus:outline-none"
      />
      <button
        type="submit"
        :disabled="!newName.trim() || creating"
        class="rounded-lg border border-border bg-bg-surface px-3 py-2 text-sm text-text-secondary transition-colors hover:border-accent hover:text-text disabled:opacity-50"
      >
        {{ creating ? 'Adding…' : 'Add' }}
      </button>
    </form>
    <p v-if="error" class="mt-2 text-xs text-danger">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useLabelsStore } from '@/stores/labels'
import { serverMessage } from '@/utils/serverMessage'

const props = withDefaults(defineProps<{
  modelValue: number[]
  allowCreate?: boolean
  ariaLabel?: string
}>(), { allowCreate: false, ariaLabel: 'Labels' })
const emit = defineEmits<{ 'update:modelValue': [ids: number[]] }>()

const store = useLabelsStore()
const selected = computed(() => new Set(props.modelValue))
const newName = ref('')
const creating = ref(false)
const error = ref('')

function toggle(id: number) {
  emit('update:modelValue', selected.value.has(id)
    ? props.modelValue.filter(existing => existing !== id)
    : [...props.modelValue, id])
}

async function create() {
  const name = newName.value.trim()
  if (!name) return
  const existing = store.labels.find(label => label.name.toLowerCase() === name.toLowerCase())
  if (existing) {
    if (!selected.value.has(existing.id)) toggle(existing.id)
    newName.value = ''
    return
  }
  creating.value = true
  error.value = ''
  try {
    const label = await store.createLabel({ name })
    emit('update:modelValue', [...props.modelValue, label.id])
    newName.value = ''
  } catch (e) {
    error.value = serverMessage(e) ?? 'The label could not be created. Try again.'
  } finally {
    creating.value = false
  }
}
</script>
