<template>
  <div class="detail-row">
    <dt class="detail-label">{{ label }}</dt>
    <dd class="detail-value">
      <!-- Being edited -->
      <div v-if="editing" class="edit-shell">
        <select
          v-if="meta && meta.input === 'select' && meta.strict"
          ref="control"
          :value="draft"
          :disabled="saving"
          class="edit-control"
          @change="onSelect"
          @keydown.esc.prevent="$emit('cancel')"
        >
          <option value="">{{ meta.required ? 'Select…' : 'Not set' }}</option>
          <option v-for="option in choices" :key="option" :value="option">{{ option }}</option>
        </select>

        <template v-else>
          <input
            ref="control"
            :value="draft"
            :type="inputType"
            :min="meta?.min"
            :max="meta?.max"
            :step="meta?.step"
            :maxlength="meta?.maxlength"
            :list="choices.length ? listId : undefined"
            :disabled="saving"
            class="edit-control"
            @input="$emit('update:draft', ($event.target as HTMLInputElement).value)"
            @keydown.enter.prevent="$emit('commit')"
            @keydown.esc.prevent="$emit('cancel')"
            @blur="$emit('commit')"
          />
          <!-- Suggestions that still allow a value of the user's own -->
          <datalist v-if="choices.length" :id="listId">
            <option v-for="option in choices" :key="option" :value="option" />
          </datalist>
        </template>

      </div>

      <!-- Editable, awaiting a tap -->
      <button
        v-else-if="editable"
        type="button"
        class="detail-edit"
        :class="{ 'is-empty': !value }"
        :disabled="saving"
        @click="$emit('start')"
      >
        {{ saving ? 'Saving…' : (value || 'Add') }}
      </button>

      <!-- Read-only -->
      <a
        v-else-if="href && value"
        :href="href"
        target="_blank"
        rel="noopener noreferrer"
        class="detail-link"
      >{{ value }} ↗</a>
      <span v-else>{{ value }}</span>

      <p v-if="error" class="detail-error">{{ error }}</p>
    </dd>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch as vueWatch } from 'vue'
import { fieldMeta, type InlineField } from '@/constants/watch'

const props = defineProps<{
  label: string
  value?: string
  href?: string
  field?: InlineField
  editable?: boolean
  editing?: boolean
  saving?: boolean
  error?: string
  draft?: string
  /** Choices only known at runtime, such as the user's storage locations. */
  options?: string[]
}>()

const emit = defineEmits<{
  start: []
  commit: []
  cancel: []
  'update:draft': [value: string]
}>()

const control = ref<HTMLInputElement | HTMLSelectElement | null>(null)

const meta = computed(() => (props.field ? fieldMeta[props.field] : undefined))
const choices = computed(() => props.options ?? [...(meta.value?.options ?? [])])
const listId = computed(() => `opts-${props.field}`)
const inputType = computed(() => (meta.value?.input === 'number' ? 'number' : meta.value?.input === 'date' ? 'date' : 'text'))

// Focus the control as it appears, and select existing text so typing replaces it.
vueWatch(() => props.editing, async (isEditing) => {
  if (!isEditing) return
  await nextTick()
  control.value?.focus()
  if (control.value instanceof HTMLInputElement && inputType.value === 'text') control.value.select()
})

function onSelect(event: Event) {
  emit('update:draft', (event.target as HTMLSelectElement).value)
  // A select has no separate confirm step; choosing a value is the commit.
  nextTick(() => emit('commit'))
}
</script>

<style scoped>
.edit-shell {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.edit-control {
  flex: 1;
  min-width: 0;
  background: var(--color-bg-surface);
  border: 1px solid var(--color-accent);
  border-radius: 0.5rem;
  color: var(--color-text);
  font: inherit;
  font-size: 0.95rem;
  padding: 0.35rem 0.5rem;
}

.edit-control:focus {
  outline: 2px solid var(--color-accent);
  outline-offset: 1px;
}

.detail-edit {
  width: 100%;
  text-align: left;
  background: none;
  border: 1px dashed var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  font: inherit;
  font-size: 1rem;
  padding: 0.3rem 0.5rem;
  margin: -0.3rem -0.5rem;
  cursor: pointer;
  transition: border-color 0.15s ease, color 0.15s ease;
}

.detail-edit:hover:not(:disabled),
.detail-edit:focus-visible {
  border-color: var(--color-accent);
  color: var(--color-accent);
}

.detail-edit.is-empty {
  color: var(--color-text-muted);
}

.detail-edit:disabled {
  cursor: default;
  opacity: 0.7;
}

.detail-error {
  color: var(--color-danger);
  font-size: 0.8rem;
  margin-top: 0.35rem;
}
</style>
