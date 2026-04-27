<template>
  <form @submit.prevent="emit('submit', formData)" class="space-y-5 max-w-lg">
    <!-- Required Fields -->
    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Brand *</label>
      <div class="relative">
        <input
          v-model="formData.brand"
          required
          @input="onBrandInput"
          @focus="showBrandSuggestions = true"
          @blur="hideBrandSuggestions"
          autocomplete="off"
          placeholder="e.g. Omega, Rolex, Seiko"
          class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
        />
        <ul
          v-if="showBrandSuggestions && filteredBrands.length > 0"
          class="absolute z-20 top-full left-0 right-0 mt-1 bg-bg-surface border border-border rounded-lg overflow-hidden shadow-lg max-h-48 overflow-y-auto"
        >
          <li
            v-for="brand in filteredBrands"
            :key="brand"
            @mousedown.prevent="selectBrand(brand)"
            class="px-4 py-2.5 text-sm text-text hover:bg-bg-elevated cursor-pointer transition-colors"
          >
            {{ brand }}
          </li>
        </ul>
      </div>
    </div>

    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Model *</label>
      <input
        v-model="formData.model"
        required
        placeholder="e.g. Speedmaster Professional"
        class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
      />
    </div>

    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Movement *</label>
      <select v-model="formData.movementType" required class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors appearance-none">
        <option value="Automatic">Automatic</option>
        <option value="Manual">Manual</option>
        <option value="Quartz">Quartz</option>
        <option value="Digital">Digital</option>
      </select>
    </div>

    <!-- Optional Fields (collapsible) -->
    <div class="border-t border-border pt-4">
      <button
        type="button"
        @click="showOptional = !showOptional"
        class="flex items-center gap-2 text-sm font-medium text-text-secondary hover:text-text transition-colors"
      >
        <svg
          class="w-4 h-4 transition-transform"
          :class="{ 'rotate-90': showOptional }"
          fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="1.5"
        >
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7" />
        </svg>
        Additional Details
        <span class="text-text-muted text-xs">(optional)</span>
      </button>

      <Transition name="expand">
        <div v-if="showOptional" class="mt-4 space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1">Case Size (mm)</label>
              <input v-model.number="formData.caseSizeMm" type="number" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors" />
            </div>
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1">Crystal Type</label>
              <input v-model="formData.crystalType" placeholder="Sapphire" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1">Band Type</label>
              <input v-model="formData.bandType" placeholder="Bracelet" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
            </div>
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1">Band Color</label>
              <input v-model="formData.bandColor" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1">Dial Color</label>
              <input v-model="formData.dialColor" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors" />
            </div>
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1">Water Resistance</label>
              <input v-model="formData.waterResistance" placeholder="100m" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
            </div>
          </div>

          <div>
            <label class="block text-xs font-medium text-text-muted mb-1">Purchase Date</label>
            <input v-model="formData.purchaseDate" type="date" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors" />
          </div>

          <div>
            <label class="block text-xs font-medium text-text-muted mb-1">Purchase Price</label>
            <input v-model.number="formData.purchasePrice" type="number" step="0.01" placeholder="0.00" class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
          </div>

          <div>
            <label class="block text-xs font-medium text-text-muted mb-1">Notes</label>
            <textarea v-model="formData.notes" rows="2" placeholder="Any additional notes..." class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors resize-none" />
          </div>

          <div>
            <label class="block text-xs font-medium text-text-muted mb-1">Link URL</label>
            <input v-model="formData.linkUrl" type="url" placeholder="https://..." class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
          </div>
        </div>
      </Transition>
    </div>

    <button type="submit" :disabled="loading" class="w-full py-3 bg-accent hover:bg-accent-hover text-bg font-semibold rounded-lg transition-colors disabled:opacity-50">
      {{ loading ? 'Saving...' : 'Save Watch' }}
    </button>
  </form>
</template>

<script setup lang="ts">
import { reactive, ref, computed } from 'vue'
import type { CreateWatch } from '@/types'

const props = defineProps<{
  initial?: CreateWatch
  loading?: boolean
  existingBrands?: string[]
}>()

const emit = defineEmits<{
  submit: [data: CreateWatch]
}>()

const showOptional = ref(false)
const showBrandSuggestions = ref(false)

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

// Auto-expand optional section if editing and has optional data
if (props.initial && (props.initial.caseSizeMm || props.initial.bandType || props.initial.purchaseDate || props.initial.purchasePrice || props.initial.notes || props.initial.crystalType || props.initial.linkUrl)) {
  showOptional.value = true
}

// Brand typeahead
const filteredBrands = computed(() => {
  if (!formData.brand || !props.existingBrands) return []
  const query = formData.brand.toLowerCase()
  return props.existingBrands
    .filter(b => b.toLowerCase().includes(query) && b.toLowerCase() !== query)
    .slice(0, 8)
})

function onBrandInput() {
  showBrandSuggestions.value = true
}

function selectBrand(brand: string) {
  formData.brand = brand
  showBrandSuggestions.value = false
}

function hideBrandSuggestions() {
  setTimeout(() => { showBrandSuggestions.value = false }, 150)
}
</script>

<style scoped>
.expand-enter-active,
.expand-leave-active {
  transition: all 0.25s ease;
  overflow: hidden;
}
.expand-enter-from,
.expand-leave-to {
  opacity: 0;
  max-height: 0;
}
.expand-enter-to,
.expand-leave-from {
  opacity: 1;
  max-height: 1000px;
}
</style>
