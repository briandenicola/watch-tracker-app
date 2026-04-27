<template>
  <form @submit.prevent="handleFormSubmit" class="space-y-5 max-w-lg">
    <!-- Photo Section -->
    <div v-if="!hidePhoto" class="flex flex-col items-center gap-3">
      <div
        class="w-32 h-32 rounded-xl bg-bg-surface border-2 border-dashed border-border overflow-hidden flex items-center justify-center cursor-pointer hover:border-accent transition-colors"
        @click="triggerFileInput"
      >
        <img v-if="photoPreview" :src="photoPreview" class="w-full h-full object-cover" />
        <img v-else-if="existingImageUrl" :src="existingImageUrl" class="w-full h-full object-contain p-1" />
        <div v-else class="text-center">
          <svg class="w-8 h-8 mx-auto text-text-muted mb-1" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="1.25">
            <rect x="3" y="3" width="18" height="18" rx="2" />
            <circle cx="8.5" cy="8.5" r="1.5" />
            <path d="M21 15l-5-5L5 21" />
          </svg>
          <span class="text-xs text-text-muted">Add Photo</span>
        </div>
      </div>
      <button
        v-if="photoPreview"
        type="button"
        @click="removePhoto"
        class="text-xs text-danger hover:text-danger-hover transition-colors"
      >
        Remove Photo
      </button>
      <input ref="fileInput" type="file" accept="image/*" class="hidden" @change="onFileSelected" />
    </div>

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

    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Purchase Date *</label>
      <input v-model="formData.purchaseDate" type="date" required class="w-full px-4 py-3 bg-bg-surface border border-border rounded-lg text-text focus:outline-none focus:border-accent transition-colors" />
    </div>

    <div>
      <label class="block text-sm font-medium text-text-secondary mb-1">Purchase Price *</label>
      <div class="relative">
        <span class="absolute left-4 top-1/2 -translate-y-1/2 text-text-muted text-sm">$</span>
        <input v-model.number="formData.purchasePrice" type="number" step="0.01" min="0" required placeholder="0.00" class="w-full pl-8 pr-4 py-3 bg-bg-surface border border-border rounded-lg text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors" />
      </div>
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
              <div class="relative">
                <select
                  v-if="!customCrystalType"
                  v-model="formData.crystalType"
                  class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
                >
                  <option value="">Select...</option>
                  <option v-for="ct in crystalTypes" :key="ct" :value="ct">{{ ct }}</option>
                  <option value="__custom__">Other...</option>
                </select>
                <div v-else class="flex gap-1">
                  <input
                    v-model="formData.crystalType"
                    placeholder="Custom type"
                    class="flex-1 px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
                  />
                  <button type="button" @click="customCrystalType = false; formData.crystalType = ''" class="px-2 text-text-muted hover:text-text text-xs">✕</button>
                </div>
              </div>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1">Band Type</label>
              <div class="relative">
                <select
                  v-if="!customBandType"
                  v-model="formData.bandType"
                  class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text focus:outline-none focus:border-accent transition-colors appearance-none"
                >
                  <option value="">Select...</option>
                  <option v-for="bt in bandTypes" :key="bt" :value="bt">{{ bt }}</option>
                  <option value="__custom__">Other...</option>
                </select>
                <div v-else class="flex gap-1">
                  <input
                    v-model="formData.bandType"
                    placeholder="Custom type"
                    class="flex-1 px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
                  />
                  <button type="button" @click="customBandType = false; formData.bandType = ''" class="px-2 text-text-muted hover:text-text text-xs">✕</button>
                </div>
              </div>
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
            <label class="block text-xs font-medium text-text-muted mb-1">Notes</label>
            <textarea v-model="formData.notes" rows="6" placeholder="Any additional notes..." class="w-full px-3 py-2.5 bg-bg-surface border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors resize-y" />
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
import { reactive, ref, computed, watch as vueWatch } from 'vue'
import type { CreateWatch, Watch } from '@/types'
import { imageUrl as toImageUrl } from '@/services/watches'

const props = defineProps<{
  initial?: Partial<Watch>
  loading?: boolean
  existingBrands?: string[]
  hidePhoto?: boolean
}>()

const emit = defineEmits<{
  submit: [data: CreateWatch, photo?: File]
}>()

const bandTypes = ['Bracelet', 'Leather', 'Rubber', 'NATO', 'Canvas', 'Mesh', 'Silicone', 'Ceramic', 'Titanium']
const crystalTypes = ['Sapphire', 'Mineral', 'Hardlex', 'Acrylic', 'Hesalite']

const showOptional = ref(false)
const showBrandSuggestions = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
const photoFile = ref<File | null>(null)
const photoPreview = ref<string | null>(null)
const customBandType = ref(false)
const customCrystalType = ref(false)

// Show existing image from the watch being edited
const existingImageUrl = computed(() => {
  if (props.initial && 'imageUrls' in props.initial && props.initial.imageUrls?.length) {
    return toImageUrl(props.initial.imageUrls[0].url)
  }
  return null
})

// Format purchaseDate for date input (needs YYYY-MM-DD)
function formatDateForInput(dateStr?: string): string {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  if (isNaN(d.getTime())) return dateStr
  return d.toISOString().split('T')[0]
}

const formData = reactive<CreateWatch>({
  brand: props.initial?.brand || '',
  model: props.initial?.model || '',
  movementType: props.initial?.movementType || 'Automatic',
  caseSizeMm: props.initial?.caseSizeMm,
  bandType: props.initial?.bandType || '',
  bandColor: props.initial?.bandColor || '',
  purchaseDate: formatDateForInput(props.initial?.purchaseDate),
  purchasePrice: props.initial?.purchasePrice,
  notes: props.initial?.notes || '',
  crystalType: props.initial?.crystalType || '',
  dialColor: props.initial?.dialColor || '',
  waterResistance: props.initial?.waterResistance || '',
  linkUrl: props.initial?.linkUrl || '',
})

// If editing with a band type not in the predefined list, show custom input
if (props.initial?.bandType && !bandTypes.includes(props.initial.bandType)) {
  customBandType.value = true
}
if (props.initial?.crystalType && !crystalTypes.includes(props.initial.crystalType)) {
  customCrystalType.value = true
}

// Watch for "Other..." selection
vueWatch(() => formData.bandType, (val) => {
  if (val === '__custom__') {
    customBandType.value = true
    formData.bandType = ''
  }
})
vueWatch(() => formData.crystalType, (val) => {
  if (val === '__custom__') {
    customCrystalType.value = true
    formData.crystalType = ''
  }
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

// Photo handling
function triggerFileInput() {
  fileInput.value?.click()
}

function onFileSelected(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  photoFile.value = file
  photoPreview.value = URL.createObjectURL(file)
}

function removePhoto() {
  photoFile.value = null
  if (photoPreview.value) {
    URL.revokeObjectURL(photoPreview.value)
    photoPreview.value = null
  }
  if (fileInput.value) fileInput.value.value = ''
}

function handleFormSubmit() {
  emit('submit', formData, photoFile.value || undefined)
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
