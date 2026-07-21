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
        <p v-if="watch.isWishList" class="text-sm text-text-muted mt-1">Wish List</p>
        <p v-else class="text-sm text-text-muted mt-1">{{ watch.movementType }} · Worn {{ watch.timesWorn }} times</p>
      </div>

      <!-- Price callout -->
      <div v-if="watch.purchasePrice || watch.currentResaleValue" class="mb-4 flex flex-wrap items-center gap-3">
        <span v-if="watch.purchasePrice" class="inline-block px-4 py-2 bg-accent/10 border border-accent/30 rounded-lg text-lg font-display font-semibold text-accent">
          ${{ watch.purchasePrice.toFixed(2) }}
        </span>
        <span v-if="watch.purchaseDate" class="text-xs text-text-muted">
          {{ watch.isWishList ? 'Target price' : `Purchased ${new Date(watch.purchaseDate).toLocaleDateString()}` }}
        </span>
        <template v-if="!watch.isWishList && watch.currentResaleValue">
          <span class="inline-block px-4 py-2 bg-bg-surface border border-border rounded-lg text-lg font-display font-semibold text-text">
            ${{ watch.currentResaleValue.toFixed(2) }}
            <span class="text-xs font-normal text-text-muted">resale</span>
          </span>
          <span v-if="resaleGain !== null" class="text-sm font-medium" :class="resaleGain >= 0 ? 'text-success' : 'text-danger'">
            {{ resaleGain >= 0 ? '+' : '' }}${{ resaleGain.toFixed(2) }}
          </span>
          <span v-if="watch.resaleValueUpdatedAt" class="text-xs text-text-muted">
            Updated {{ new Date(watch.resaleValueUpdatedAt).toLocaleDateString() }}
          </span>
        </template>
      </div>

      <!-- Actions -->
      <div class="flex flex-wrap gap-2 mb-6">
        <!-- Collection watch actions -->
        <template v-if="!watch.isWishList">
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
          <button
            @click="handleRefreshResale"
            :disabled="refreshingResale"
            class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors disabled:opacity-50"
          >
            {{ refreshingResale ? 'Queuing…' : '🔄 Refresh Resale Value' }}
          </button>
          <button @click="handleRetire" class="px-4 py-2 bg-bg-surface border border-border text-text-secondary text-sm rounded-lg hover:border-accent/50 transition-colors">
            Retire
          </button>
          <button @click="handleDelete" class="px-4 py-2 bg-bg-surface border border-danger/50 text-danger text-sm rounded-lg hover:bg-danger/10 transition-colors">
            Delete
          </button>
        </template>
        <!-- Wish list actions -->
        <template v-else>
          <button @click="handlePurchase" :disabled="purchasing" class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50">
            {{ purchasing ? 'Moving…' : '🛒 Mark as Purchased' }}
          </button>
          <RouterLink :to="`/wishlist/${watch.id}/edit`" class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors">
            Edit
          </RouterLink>
          <label class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors cursor-pointer">
            {{ uploading ? 'Uploading…' : 'Upload Images' }}
            <input type="file" accept="image/*" multiple class="hidden" @change="handleImageUpload" :disabled="uploading" />
          </label>
          <button @click="handleDelete" class="px-4 py-2 bg-bg-surface border border-danger/50 text-danger text-sm rounded-lg hover:bg-danger/10 transition-colors">
            Delete
          </button>
        </template>
      </div>

      <!-- AI Analysis -->
      <div v-if="watch.aiAnalysis" class="bg-bg-card border border-border rounded-xl p-4 mb-6">
        <h3 class="text-sm font-medium text-text-secondary mb-2">🤖 AI Analysis</h3>
        <div class="prose-markdown text-sm text-text" v-html="renderMarkdown(watch.aiAnalysis)" />
      </div>

      <!-- Resale Value History -->
      <div v-if="!watch.isWishList" class="bg-bg-card border border-border rounded-xl p-4 mb-6">
        <div class="flex items-center justify-between mb-2">
          <h3 class="text-sm font-medium text-text-secondary">Resale Value History</h3>
          <button @click="showManualResaleForm = !showManualResaleForm" class="text-xs text-accent hover:underline">
            {{ showManualResaleForm ? 'Cancel' : '+ Log Value' }}
          </button>
        </div>
        <p v-if="resaleError" class="text-xs text-danger mb-2">{{ resaleError }}</p>
        <p v-if="resaleQueuedMsg" class="text-xs text-success mb-2">{{ resaleQueuedMsg }}</p>

        <div v-if="showManualResaleForm" class="flex flex-wrap items-end gap-2 mb-4 p-3 bg-bg-surface border border-border rounded-lg">
          <div>
            <label class="block text-xs text-text-muted mb-1">Value</label>
            <input v-model.number="manualResaleValue" type="number" step="0.01" min="0" placeholder="0.00" class="w-28 px-2 py-1.5 bg-bg border border-border rounded-lg text-sm text-text" />
          </div>
          <div>
            <label class="block text-xs text-text-muted mb-1">Date</label>
            <input v-model="manualResaleDate" type="date" class="px-2 py-1.5 bg-bg border border-border rounded-lg text-sm text-text" />
          </div>
          <div class="flex-1 min-w-[8rem]">
            <label class="block text-xs text-text-muted mb-1">Notes</label>
            <input v-model="manualResaleNotes" type="text" placeholder="Optional" class="w-full px-2 py-1.5 bg-bg border border-border rounded-lg text-sm text-text" />
          </div>
          <button
            @click="handleAddManualResale"
            :disabled="savingManualResale || !manualResaleValue"
            class="px-3 py-1.5 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {{ savingManualResale ? 'Saving…' : 'Save' }}
          </button>
        </div>

        <div v-if="resaleHistory.length === 0" class="text-sm text-text-muted">
          No resale value recorded yet — log one manually or refresh an estimate.
        </div>
        <div v-else class="space-y-2">
          <div v-for="entry in resaleHistory" :key="entry.id" class="flex items-start justify-between gap-3 text-sm">
            <div class="min-w-0">
              <span class="text-text font-medium">${{ entry.value.toFixed(2) }}</span>
              <span class="ml-2 px-1.5 py-0.5 rounded-full text-[10px] uppercase tracking-wide bg-bg-surface border border-border text-text-muted">
                {{ entry.source === 'Manual' ? 'Manual' : 'Web Estimate' }}
              </span>
              <span class="ml-2 text-xs text-text-muted">{{ new Date(entry.recordedAt).toLocaleDateString() }}</span>
              <p v-if="entry.reasoning" class="text-xs text-text-muted mt-0.5 truncate" :title="entry.reasoning">{{ entry.reasoning }}</p>
            </div>
            <button @click="handleDeleteResaleEntry(entry.id)" class="text-xs text-danger hover:underline flex-shrink-0">Remove</button>
          </div>
        </div>
      </div>

      <!-- Details Chips -->
      <div class="flex flex-wrap gap-2 mb-6">
        <span v-if="!watch.isWishList && watch.movementType" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.movementType }}
        </span>
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
        <span v-if="!watch.isWishList && watch.powerReserveHours" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.powerReserveHours }}h reserve
        </span>
        <span v-if="watch.countryOfOrigin" class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
          {{ watch.countryOfOrigin }}
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
          {{ watch.linkText || 'Store Link' }} ↗
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
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { marked } from 'marked'
import type { Watch, ResaleValueEntry } from '@/types'
import {
  getWatch, imageUrl, recordWear, retireWatch, deleteWatch, uploadImage, deleteImage, removeBackground,
  analyzeWatch, updateWatch, getResaleHistory, addManualResaleValue, deleteResaleValueEntry, refreshResaleValue,
} from '@/services/watches'

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
const purchasing = ref(false)

const resaleHistory = ref<ResaleValueEntry[]>([])
const refreshingResale = ref(false)
const resaleError = ref('')
const resaleQueuedMsg = ref('')
const showManualResaleForm = ref(false)
const manualResaleValue = ref<number | null>(null)
const manualResaleDate = ref('')
const manualResaleNotes = ref('')
const savingManualResale = ref(false)

const resaleGain = computed(() => {
  if (!watch.value?.currentResaleValue || !watch.value?.purchasePrice) return null
  return watch.value.currentResaleValue - watch.value.purchasePrice
})

onMounted(async () => {
  try {
    watch.value = await getWatch(Number(route.params.id))
    if (watch.value && !watch.value.isWishList) {
      resaleHistory.value = await getResaleHistory(watch.value.id)
    }
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

async function handlePurchase() {
  if (!watch.value || !confirm('Move this watch from your wish list to your collection?')) return
  purchasing.value = true
  try {
    const w = watch.value
    await updateWatch(w.id, {
      brand: w.brand,
      model: w.model,
      movementType: w.movementType,
      caseSizeMm: w.caseSizeMm,
      bandType: w.bandType,
      bandColor: w.bandColor,
      purchaseDate: w.purchaseDate,
      purchasePrice: w.purchasePrice,
      notes: w.notes,
      crystalType: w.crystalType,
      dialColor: w.dialColor,
      waterResistance: w.waterResistance,
      linkUrl: w.linkUrl,
      linkText: w.linkText,
      isWishList: false,
    })
    watch.value = await getWatch(w.id)
  } finally {
    purchasing.value = false
  }
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

async function handleRefreshResale() {
  if (!watch.value) return
  refreshingResale.value = true
  resaleError.value = ''
  resaleQueuedMsg.value = ''
  try {
    await refreshResaleValue(watch.value.id)
    resaleQueuedMsg.value = 'Refresh queued — check back in a bit for the updated value.'
  } catch (e: any) {
    resaleError.value = e?.response?.data?.error || 'Could not queue a resale value refresh.'
  } finally {
    refreshingResale.value = false
  }
}

async function handleAddManualResale() {
  if (!watch.value || manualResaleValue.value === null) return
  savingManualResale.value = true
  resaleError.value = ''
  try {
    watch.value = await addManualResaleValue(watch.value.id, {
      value: manualResaleValue.value,
      recordedAt: manualResaleDate.value || undefined,
      notes: manualResaleNotes.value || undefined,
    })
    resaleHistory.value = await getResaleHistory(watch.value.id)
    manualResaleValue.value = null
    manualResaleDate.value = ''
    manualResaleNotes.value = ''
    showManualResaleForm.value = false
  } finally {
    savingManualResale.value = false
  }
}

async function handleDeleteResaleEntry(entryId: number) {
  if (!watch.value || !confirm('Remove this resale value entry?')) return
  await deleteResaleValueEntry(entryId)
  resaleHistory.value = resaleHistory.value.filter(e => e.id !== entryId)
  watch.value = await getWatch(watch.value.id)
}
</script>
