<template>
  <div>
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Add to Wish List</h2>
    <section class="max-w-lg mb-6 p-4 bg-bg-surface border border-border rounded-xl">
      <h3 class="font-medium text-text mb-1">Import from a product page</h3>
      <p class="text-xs text-text-muted mb-3">
        Extract the core details with Ollama, then review them before saving.
      </p>
      <div class="flex flex-col sm:flex-row gap-2">
        <div class="relative flex-1">
          <input
            ref="sourceUrlInput"
            v-model="sourceUrl"
            data-testid="wishlist-source-url"
            type="url"
            maxlength="2000"
            placeholder="https://store.example/watch"
            class="w-full pl-3 pr-11 py-2.5 bg-bg border border-border rounded-lg text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-accent transition-colors"
            @keydown.enter.prevent="handleExtract"
          />
          <button
            v-if="sourceUrl"
            type="button"
            data-testid="wishlist-clear-url"
            aria-label="Clear URL"
            :disabled="extracting"
            class="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-text-muted hover:text-text disabled:opacity-40 disabled:hover:text-text-muted transition-colors"
            @click="clearSourceUrl"
          >
            <AppIcon name="close" :size="18" :stroke-width="2" />
          </button>
        </div>
        <button
          type="button"
          :disabled="extracting || !sourceUrl.trim()"
          class="px-4 py-2.5 bg-accent hover:bg-accent-hover text-bg text-sm font-semibold rounded-lg transition-colors disabled:opacity-50"
          @click="handleExtract"
        >
          {{ extracting ? 'Extracting...' : 'Extract details' }}
        </button>
      </div>
      <p v-if="extractError" class="text-danger text-sm mt-3">{{ extractError }}</p>
      <div v-else-if="extractionComplete" class="mt-3">
        <p class="text-success text-sm">Details added below. Review them before saving.</p>
        <ul v-if="extractionWarnings.length" class="mt-2 space-y-1 text-xs text-warning">
          <li v-for="warning in extractionWarnings" :key="warning">{{ warning }}</li>
        </ul>
      </div>
    </section>
    <WatchForm
      ref="watchForm"
      mode="wishlist"
      @submit="handleSubmit"
      :loading="loading"
      :existing-brands="brands"
      :storage-locations="storageLocations"
    />
    <p v-if="error" class="text-danger text-sm mt-4">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  createWatch,
  extractWishlistUrl,
  getWatches,
  uploadImage,
  importImageFromUrl,
} from '@/services/watches'
import { api } from '@/services/api'
import WatchForm from '@/components/common/WatchForm.vue'
import AppIcon from '@/components/icons/AppIcon.vue'
import type { AuthResponse, CreateWatch } from '@/types'

const router = useRouter()
const loading = ref(false)
const error = ref('')
const brands = ref<string[]>([])
const storageLocations = ref<string[]>([])
const sourceUrl = ref('')
const extracting = ref(false)
const extractError = ref('')
const extractionComplete = ref(false)
const extractionWarnings = ref<string[]>([])
const watchForm = ref<InstanceType<typeof WatchForm> | null>(null)
const sourceUrlInput = ref<HTMLInputElement | null>(null)

onMounted(async () => {
  try {
    const [watches, profileResp] = await Promise.all([
      getWatches(),
      api.get<AuthResponse>('/api/auth/me'),
    ])
    brands.value = [...new Set(watches.map(w => w.brand))].sort()
    storageLocations.value = profileResp.data.storageLocations || []
  } catch { /* non-critical */ }
})

// Clears the URL and the error that described it. The extraction success
// banner stays: it refers to the values already sitting in the form below,
// which this button does not touch.
function clearSourceUrl() {
  sourceUrl.value = ''
  extractError.value = ''
  sourceUrlInput.value?.focus()
}

async function handleExtract() {
  if (!sourceUrl.value.trim() || extracting.value) return

  extracting.value = true
  extractError.value = ''
  extractionComplete.value = false
  extractionWarnings.value = []
  try {
    const result = await extractWishlistUrl(sourceUrl.value.trim())
    watchForm.value?.applyDraft({
      brand: result.brand,
      model: result.model,
      purchasePrice: result.purchasePrice,
      linkUrl: result.linkUrl,
      linkText: result.linkText,
    }, result.imageUrl)
    extractionWarnings.value = result.warnings
    extractionComplete.value = true
  } catch (e: any) {
    extractError.value = e.response?.data?.error || 'Could not extract details from this page.'
  } finally {
    extracting.value = false
  }
}

async function handleSubmit(data: CreateWatch, photo?: File, imageUrl?: string) {
  loading.value = true
  error.value = ''
  try {
    const watch = await createWatch({ ...data, isWishList: true })
    if (photo) {
      await uploadImage(watch.id, photo)
    } else if (imageUrl) {
      await importImageFromUrl(watch.id, imageUrl)
    }
    router.push('/?tab=wishlist')
  } catch (e: any) {
    error.value = e.response?.data?.error || 'Failed to add'
  } finally {
    loading.value = false
  }
}
</script>
