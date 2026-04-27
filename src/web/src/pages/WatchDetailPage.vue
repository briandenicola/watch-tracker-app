<template>
  <div>
    <RouterLink to="/" class="text-accent text-sm hover:underline mb-4 inline-block">← Back</RouterLink>
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>
    <div v-else-if="watch">
      <!-- Image Gallery -->
      <div v-if="watch.imageUrls.length > 0" class="relative rounded-xl overflow-hidden bg-bg-surface mb-6">
        <div class="h-[300px] lg:h-[400px] flex items-center justify-center">
          <img
            :src="imageUrl(watch.imageUrls[imageIndex].url)"
            :alt="`${watch.brand} ${watch.model}`"
            class="max-w-full max-h-full object-contain"
          />
        </div>
        <div v-if="watch.imageUrls.length > 1" class="absolute bottom-3 inset-x-0 flex justify-center gap-1.5">
          <button
            v-for="(_, i) in watch.imageUrls"
            :key="i"
            @click="imageIndex = i"
            class="w-2 h-2 rounded-full transition-colors"
            :class="i === imageIndex ? 'bg-accent' : 'bg-white/40'"
          />
        </div>
      </div>

      <!-- Image management buttons -->
      <div v-if="watch.imageUrls.length > 0" class="flex flex-wrap gap-2 mb-4">
        <button
          @click="handleRemoveBackground"
          :disabled="removingBg"
          class="px-3 py-1.5 bg-bg-surface border border-border text-text-secondary text-xs rounded-lg hover:border-accent/50 transition-colors disabled:opacity-50"
        >
          {{ removingBg ? 'Removing…' : 'Remove Background' }}
        </button>
        <button
          @click="handleDeleteImage"
          class="px-3 py-1.5 bg-bg-surface border border-danger/50 text-danger text-xs rounded-lg hover:bg-danger/10 transition-colors"
        >
          Delete Image
        </button>
      </div>

      <!-- Header -->
      <div class="mb-4">
        <h1 class="font-display text-2xl font-semibold text-text">{{ watch.brand }} {{ watch.model }}</h1>
        <p class="text-sm text-text-muted mt-1">{{ watch.movementType }} · Worn {{ watch.timesWorn }} times</p>
      </div>

      <!-- Actions -->
      <div class="flex flex-wrap gap-2 mb-6">
        <button @click="handleWear" :disabled="wearLoading" class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50">
          {{ wearLoading ? 'Recording...' : '⌚ Wore Today' }}
        </button>
        <RouterLink :to="`/watches/${watch.id}/edit`" class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors">
          Edit
        </RouterLink>
        <label class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors cursor-pointer">
          {{ uploading ? 'Uploading…' : 'Upload Images' }}
          <input type="file" accept="image/*" multiple class="hidden" @change="handleImageUpload" :disabled="uploading" />
        </label>
        <button
          @click="handleAnalyze"
          :disabled="analyzing || !watch.imageUrls.length"
          class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors disabled:opacity-50"
        >
          {{ analyzing ? 'Analyzing…' : '🤖 AI Analyze' }}
        </button>
        <button @click="handleRetire" class="px-4 py-2 bg-bg-surface border border-border text-text-secondary text-sm rounded-lg hover:border-accent/50 transition-colors">
          Retire
        </button>
        <button @click="handleDelete" class="px-4 py-2 bg-bg-surface border border-danger/50 text-danger text-sm rounded-lg hover:bg-danger/10 transition-colors">
          Delete
        </button>
      </div>

      <!-- AI Analysis -->
      <div v-if="watch.aiAnalysis" class="bg-bg-card border border-border rounded-xl p-4 mb-6">
        <h3 class="text-sm font-medium text-text-secondary mb-2">🤖 AI Analysis</h3>
        <div class="prose-markdown text-sm text-text" v-html="renderMarkdown(watch.aiAnalysis)" />
      </div>

      <!-- Details Chips -->
      <div class="flex flex-wrap gap-2 mb-6">
        <span v-if="watch.caseSizeMm" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.caseSizeMm }}mm case
        </span>
        <span v-if="watch.caseShape" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.caseShape }}
        </span>
        <span v-if="watch.bandType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.bandType }} band
        </span>
        <span v-if="watch.bandColor" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.bandColor }}
        </span>
        <span v-if="watch.dialColor" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.dialColor }} dial
        </span>
        <span v-if="watch.crystalType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.crystalType }} crystal
        </span>
        <span v-if="watch.bezelType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.bezelType }} bezel
        </span>
        <span v-if="watch.crownType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.crownType }} crown
        </span>
        <span v-if="watch.calendarType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.calendarType }}
        </span>
        <span v-if="watch.waterResistance" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.waterResistance }}
        </span>
        <span v-if="watch.lugWidthMm" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.lugWidthMm }}mm lug
        </span>
        <span v-if="watch.powerReserveHours" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.powerReserveHours }}h reserve
        </span>
        <span v-if="watch.countryOfOrigin" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.countryOfOrigin }}
        </span>
        <span v-if="watch.purchasePrice" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          ${{ watch.purchasePrice.toFixed(2) }}
        </span>
        <span v-if="watch.purchaseDate" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          Purchased {{ new Date(watch.purchaseDate).toLocaleDateString() }}
        </span>
        <span v-if="watch.batteryType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.batteryType }} battery
        </span>
        <span v-if="watch.serialNumber" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          S/N {{ watch.serialNumber }}
        </span>
      </div>

      <!-- Link -->
      <div v-if="watch.linkUrl" class="mb-6">
        <a :href="watch.linkUrl" target="_blank" rel="noopener noreferrer" class="text-sm text-accent hover:underline">
          {{ watch.linkText || watch.linkUrl }} ↗
        </a>
      </div>

      <!-- Notes -->
      <div v-if="watch.notes" class="bg-bg-surface border border-border rounded-xl p-4">
        <h3 class="text-sm font-medium text-text-secondary mb-2">Notes</h3>
        <div class="prose-markdown text-sm text-text" v-html="renderMarkdown(watch.notes)" />
      </div>
    </div>
    <div v-else class="text-center py-20 text-text-muted">Watch not found.</div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { marked } from 'marked'
import type { Watch } from '@/types'
import { getWatch, imageUrl, recordWear, retireWatch, deleteWatch, uploadImage, deleteImage, removeBackground, analyzeWatch } from '@/services/watches'

const route = useRoute()
const router = useRouter()

function renderMarkdown(text: string): string {
  return marked.parse(text, { async: false }) as string
}

const watch = ref<Watch | null>(null)
const loading = ref(true)
const imageIndex = ref(0)
const wearLoading = ref(false)
const uploading = ref(false)
const removingBg = ref(false)
const analyzing = ref(false)

onMounted(async () => {
  try {
    watch.value = await getWatch(Number(route.params.id))
  } finally {
    loading.value = false
  }
})

async function handleWear() {
  if (!watch.value) return
  wearLoading.value = true
  try {
    await recordWear(watch.value.id)
    watch.value.timesWorn++
  } finally {
    wearLoading.value = false
  }
}

async function handleRetire() {
  if (!watch.value || !confirm('Retire this watch?')) return
  await retireWatch(watch.value.id)
  router.push('/')
}

async function handleDelete() {
  if (!watch.value || !confirm('Delete this watch permanently?')) return
  await deleteWatch(watch.value.id)
  router.push('/')
}

async function handleImageUpload(e: Event) {
  if (!watch.value) return
  const files = (e.target as HTMLInputElement).files
  if (!files?.length) return
  uploading.value = true
  try {
    for (const file of Array.from(files)) {
      await uploadImage(watch.value.id, file)
    }
    watch.value = await getWatch(watch.value.id)
  } finally {
    uploading.value = false
    ;(e.target as HTMLInputElement).value = ''
  }
}

async function handleDeleteImage() {
  if (!watch.value || !watch.value.imageUrls[imageIndex.value]) return
  if (!confirm('Delete this image?')) return
  const img = watch.value.imageUrls[imageIndex.value]
  await deleteImage(watch.value.id, img.id)
  watch.value = await getWatch(watch.value.id)
  if (imageIndex.value >= watch.value.imageUrls.length) {
    imageIndex.value = Math.max(0, watch.value.imageUrls.length - 1)
  }
}

async function handleRemoveBackground() {
  if (!watch.value || !watch.value.imageUrls[imageIndex.value]) return
  removingBg.value = true
  try {
    const img = watch.value.imageUrls[imageIndex.value]
    await removeBackground(watch.value.id, img.id)
    watch.value = await getWatch(watch.value.id)
  } finally {
    removingBg.value = false
  }
}

async function handleAnalyze() {
  if (!watch.value || !watch.value.imageUrls[imageIndex.value]) return
  analyzing.value = true
  try {
    const img = watch.value.imageUrls[imageIndex.value]
    const analysis = await analyzeWatch(watch.value.id, img.id)
    watch.value.aiAnalysis = analysis
  } finally {
    analyzing.value = false
  }
}
</script>
