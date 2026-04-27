<template>
  <form @submit.prevent="emit('submit', formData)" class="space-y-4 max-w-lg">
    <div class="grid grid-cols-2 gap-4">
      <div class="col-span-2 sm:col-span-1">
        <label class="block text-sm font-medium text-text-secondary mb-1">Brand *</label>
        <input v-model="formData.brand" required class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
      </div>
      <div class="col-span-2 sm:col-span-1">
        <label class="block text-sm font-medium text-text-secondary mb-1">Model *</label>
        <input v-model="formData.model" required class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
      </div>
    </div>

    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Movement *</label>
      <select v-model="formData.movementType" required class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors">
        <option value="Automatic">Automatic</option>
        <option value="Manual">Manual</option>
        <option value="Quartz">Quartz</option>
        <option value="Digital">Digital</option>
      </select>
    </div>

    <div class="grid grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Case Size (mm)</label>
        <input v-model.number="formData.caseSizeMm" type="number" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Band Type</label>
        <input v-model="formData.bandType" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
    </div>

    <div class="grid grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Band Color</label>
        <input v-model="formData.bandColor" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Dial Color</label>
        <input v-model="formData.dialColor" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
    </div>

    <div class="grid grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Purchase Date</label>
        <input v-model="formData.purchaseDate" type="date" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Purchase Price</label>
        <input v-model.number="formData.purchasePrice" type="number" step="0.01" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
    </div>

    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Notes</label>
      <textarea v-model="formData.notes" rows="3" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors resize-none" />
    </div>

    <div class="grid grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Crystal Type</label>
        <input v-model="formData.crystalType" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
      <div>
        <label class="block text-sm font-medium text-text-secondary mb-1">Water Resistance</label>
        <input v-model="formData.waterResistance" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
      </div>
    </div>

    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Link URL</label>
      <input v-model="formData.linkUrl" type="url" class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" placeholder="https://..." />
    </div>

    <button type="submit" :disabled="loading" class="w-full py-3 bg-accent hover:bg-accent-hover text-bg font-semibold rounded-lg transition-colors disabled:opacity-50 mt-6">
      {{ loading ? 'Saving...' : 'Save Watch' }}
    </button>
  </form>
</template>

<script setup lang="ts">
import { reactive } from 'vue'
import type { CreateWatch } from '@/types'

const props = defineProps<{
  initial?: CreateWatch
  loading?: boolean
}>()

const emit = defineEmits<{
  submit: [data: CreateWatch]
}>()

const formData = reactive<CreateWatch>({
  brand: props.initial?.brand || '',
  model: props.initial?.model || '',
  movementType: props.initial?.movementType || 'Automatic',
  caseSizeMm: props.initial?.caseSizeMm,
  bandType: props.initial?.bandType || '',
  bandColor: props.initial?.bandColor || '',
  purchaseDate: props.initial?.purchaseDate || '',
  purchasePrice: props.initial?.purchasePrice,
  notes: props.initial?.notes || '',
  crystalType: props.initial?.crystalType || '',
  dialColor: props.initial?.dialColor || '',
  waterResistance: props.initial?.waterResistance || '',
  linkUrl: props.initial?.linkUrl || '',
})
</script>
