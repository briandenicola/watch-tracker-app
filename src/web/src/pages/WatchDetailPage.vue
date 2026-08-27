<template>
  <div>
    <RouterLink :to="watch?.isWishList ? '/?tab=wishlist' : watch?.disposition ? '/retired' : '/'" class="mb-4 inline-block text-sm text-accent hover:underline">← Back</RouterLink>
    <div v-if="loading" class="flex justify-center py-20"><div class="h-8 w-8 animate-spin rounded-full border-2 border-accent border-t-transparent" /></div>
    <div v-else-if="watch && jsonView" class="mx-auto max-w-5xl">
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 class="font-display text-2xl font-semibold text-text">{{ watch.brand }} {{ watch.model }}</h1>
        <RouterLink :to="{ path: route.path }" class="text-sm text-accent hover:underline">Back to the full page</RouterLink>
      </div>
      <pre class="json-view">{{ watchJson }}</pre>
    </div>
    <div v-else-if="watch" class="mx-auto max-w-5xl">
      <WatchDetailHeader
        :watch="watch" :edit-mode="editMode" :saving-edits="savingEdits" :edit-session-error="editSessionError"
        :wear-loading="wearLoading" :uploading="uploading" :analyzing="analyzing" :purchasing="purchasing"
        :refreshing-resale="refreshingResale" :analysis-error="analysisError"
        @edit="beginEdit" @save-edits="saveEdits" @discard-edits="discardEdits" @wear="handleWear"
        @upload="handleImageUpload" @analyze="handleAnalyze" @style="showStyleAgent = true" @share="showShare = true"
        @refresh-resale="handleRefreshResale" @disposition="openDisposition" @restore="handleRestore"
        @delete="handleDelete" @purchase="handlePurchase"
      />

      <WatchImageGallery :watch="watch" :removing-background="removingBg" @delete-image="handleDeleteImage" @remove-background="handleRemoveBackground" />

      <section v-for="section in detailSections" :key="section.heading" class="detail-card mb-6">
        <h2 class="detail-heading">{{ section.heading }}</h2>
        <dl class="detail-list">
          <DetailRow
            v-for="row in section.rows" :key="row.label" :label="row.label" :value="row.value" :href="row.href"
            :field="row.field" :editable="editMode && !!row.field" :editing="editingField === row.field"
            :saving="savingEdits" :error="errorFor(row.field)" :options="row.field === 'storageLocation' ? storageLocationOptions : undefined"
            :draft="draft" @start="startEdit(row)" @commit="commitEdit" @cancel="cancelEdit" @update:draft="draft = $event"
          />
        </dl>
      </section>

      <section v-if="watch.aiAnalysis" class="detail-card mb-6">
        <h2 class="detail-heading">AI Analysis</h2>
        <div class="prose-markdown text-sm text-text" v-html="renderMarkdown(watch.aiAnalysis)" />
      </section>

      <section class="detail-card mb-6">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div class="min-w-0">
            <h2 class="detail-heading !mb-1">Style Agent</h2>
            <p class="text-sm text-text-muted">Talk through an outfit for this watch. It asks about the occasion and the weather, and remembers what it suggested before.</p>
          </div>
          <button class="flex-shrink-0 rounded-lg bg-accent px-4 py-2 text-sm font-medium text-bg transition-colors hover:bg-accent-hover" @click="showStyleAgent = true">Style this watch</button>
        </div>
      </section>

      <WatchResaleHistory
        v-if="!watch.isWishList" ref="resalePanel" :history="resaleHistory" :error="resaleError"
        :queued-message="resaleQueuedMsg" :saving="savingManualResale" @add="handleAddManualResale" @remove="handleDeleteResaleEntry"
      />

      <section v-if="editMode || watch.notes" class="detail-card">
        <h2 class="detail-heading">Notes</h2>
        <template v-if="editMode">
          <textarea v-model="notesDraft" rows="10" maxlength="10000" placeholder="Anything worth remembering about this watch. Markdown works here." aria-label="Notes" class="notes-editor" />
          <p class="mt-1.5 text-xs text-text-muted">Markdown supported · {{ notesDraft.length.toLocaleString() }} / 10,000</p>
        </template>
        <div v-else-if="watch.notes" class="prose-markdown text-sm text-text" v-html="renderMarkdown(watch.notes)" />
      </section>
    </div>
    <div v-else class="py-20 text-center text-text-muted">Watch not found.</div>

    <AnalysisReviewModal v-if="analysisResult && watch" :watch-id="watch.id" :watch-name="`${watch.brand} ${watch.model}`" :result="analysisResult" @applied="onAnalysisApplied" @close="analysisResult = null" />
    <ShareWatchModal v-if="showShare && watch" :watch-id="watch.id" :watch-name="`${watch.brand} ${watch.model}`" @close="showShare = false" />
    <StyleAgentModal v-if="showStyleAgent && watch" :watch-id="watch.id" :watch-name="`${watch.brand} ${watch.model}`" :has-photo="watch.imageUrls.length > 0" @close="showStyleAgent = false" />
    <DispositionModal
      v-if="showDispositionModal && watch" :current-watch-id="watch.id" :disposition="watch.disposition"
      :watches="allWatches" :saving="savingDisposition" :error-message="dispositionError"
      @cancel="showDispositionModal = false" @save="handleSaveDisposition"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { CreateResaleValueEntry, ResaleValueEntry, UpdateWatchDisposition, Watch, WatchAnalysisResult } from '@/types'
import AnalysisReviewModal from '@/components/common/AnalysisReviewModal.vue'
import DetailRow from '@/components/common/DetailRow.vue'
import DispositionModal from '@/components/common/DispositionModal.vue'
import ShareWatchModal from '@/components/common/ShareWatchModal.vue'
import StyleAgentModal from '@/components/common/StyleAgentModal.vue'
import WatchDetailHeader from '@/components/common/WatchDetailHeader.vue'
import WatchImageGallery from '@/components/common/WatchImageGallery.vue'
import WatchResaleHistory from '@/components/common/WatchResaleHistory.vue'
import { serverMessage, useWatchDetailEditor } from '@/composables/useWatchDetailEditor'
import { renderMarkdown } from '@/utils/markdown'
import {
  addManualResaleValue, analyzeWatch, clearWatchDisposition, deleteImage, deleteResaleValueEntry, deleteWatch,
  getResaleHistory, getWatch, getWatches, recordWear, refreshResaleValue, removeBackground, setWatchDisposition,
  toUpdatePayload, updateWatch, uploadImage,
} from '@/services/watches'

const route = useRoute()
const router = useRouter()
const watch = ref<Watch | null>(null)
const loading = ref(true)
const jsonView = computed(() => String(route.query.format ?? '').toLowerCase() === 'json')
const watchJson = computed(() => (watch.value ? JSON.stringify(watch.value, null, 2) : ''))
const {
  beginEdit, cancelEdit, commitEdit, detailSections, discardEdits, draft, editMode, editSessionError,
  editingField, errorFor, notesDraft, saveEdits, savingEdits, startEdit, storageLocationOptions,
} = useWatchDetailEditor(watch)

const wearLoading = ref(false)
const uploading = ref(false)
const removingBg = ref(false)
const analyzing = ref(false)
const purchasing = ref(false)
const resaleHistory = ref<ResaleValueEntry[]>([])
const refreshingResale = ref(false)
const resaleError = ref('')
const resaleQueuedMsg = ref('')
const savingManualResale = ref(false)
const resalePanel = ref<InstanceType<typeof WatchResaleHistory> | null>(null)
const allWatches = ref<Watch[]>([])
const showStyleAgent = ref(false)
const showShare = ref(false)
const analysisResult = ref<WatchAnalysisResult | null>(null)
const analysisError = ref('')
const showDispositionModal = ref(false)
const savingDisposition = ref(false)
const dispositionError = ref('')

onMounted(async () => {
  try {
    const [loadedWatch, loadedWatches] = await Promise.all([getWatch(Number(route.params.id)), getWatches(true)])
    watch.value = loadedWatch
    allWatches.value = loadedWatches
    if (!watch.value.isWishList) resaleHistory.value = await getResaleHistory(watch.value.id)
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

function onAnalysisApplied(updated: Watch) {
  watch.value = updated
}

function openDisposition() {
  dispositionError.value = ''
  showDispositionModal.value = true
}

async function handleSaveDisposition(disposition: UpdateWatchDisposition) {
  if (!watch.value) return
  savingDisposition.value = true
  dispositionError.value = ''
  try {
    watch.value = await setWatchDisposition(watch.value.id, disposition)
    allWatches.value = allWatches.value.map(item => item.id === watch.value?.id ? watch.value : item)
    showDispositionModal.value = false
  } catch (error) {
    dispositionError.value = serverMessage(error) || 'Could not save the disposition.'
  } finally {
    savingDisposition.value = false
  }
}

async function handleRestore() {
  if (!watch.value || !confirm('Restore this watch to your active collection?')) return
  watch.value = await clearWatchDisposition(watch.value.id)
  allWatches.value = allWatches.value.map(item => item.id === watch.value?.id ? watch.value : item)
}

async function handleDelete() {
  if (!watch.value || !confirm('Delete this watch permanently?')) return
  const returnTo = watch.value.isWishList ? { path: '/', query: { tab: 'wishlist' } } : { path: '/' }
  await deleteWatch(watch.value.id)
  router.push(returnTo)
}

async function handlePurchase() {
  if (!watch.value || !confirm('Move this watch from your wish list to your collection?')) return
  purchasing.value = true
  try {
    const value = watch.value
    watch.value = await updateWatch(value.id, toUpdatePayload(value, { isWishList: false }))
  } finally {
    purchasing.value = false
  }
}

async function handleImageUpload(files: File[]) {
  if (!watch.value || !files.length) return
  uploading.value = true
  try {
    for (const file of files) await uploadImage(watch.value.id, file)
    watch.value = await getWatch(watch.value.id)
  } finally {
    uploading.value = false
  }
}

async function handleDeleteImage(imageId: number) {
  if (!watch.value || !confirm('Delete this image?')) return
  await deleteImage(watch.value.id, imageId)
  watch.value = await getWatch(watch.value.id)
}

async function handleRemoveBackground(imageId: number) {
  if (!watch.value) return
  removingBg.value = true
  try {
    await removeBackground(watch.value.id, imageId)
    watch.value = await getWatch(watch.value.id)
  } finally {
    removingBg.value = false
  }
}

async function handleAnalyze() {
  if (!watch.value || !watch.value.imageUrls.length) return
  analyzing.value = true
  analysisError.value = ''
  try {
    analysisResult.value = await analyzeWatch(watch.value.id)
    watch.value = await getWatch(watch.value.id)
  } catch (error) {
    analysisError.value = serverMessage(error) || 'The AI analysis could not run.'
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
  } catch (error: any) {
    resaleError.value = error?.response?.data?.error || 'Could not queue a resale value refresh.'
  } finally {
    refreshingResale.value = false
  }
}

async function handleAddManualResale(entry: CreateResaleValueEntry) {
  if (!watch.value) return
  savingManualResale.value = true
  resaleError.value = ''
  try {
    watch.value = await addManualResaleValue(watch.value.id, entry)
    resaleHistory.value = await getResaleHistory(watch.value.id)
    resalePanel.value?.clearManualForm()
  } finally {
    savingManualResale.value = false
  }
}

async function handleDeleteResaleEntry(entryId: number) {
  if (!watch.value || !confirm('Remove this resale value entry?')) return
  await deleteResaleValueEntry(entryId)
  resaleHistory.value = resaleHistory.value.filter(entry => entry.id !== entryId)
  watch.value = await getWatch(watch.value.id)
}
</script>

<style scoped>
.json-view { overflow-x: auto; white-space: pre; border: 1px solid var(--color-border); border-radius: 1rem; background: var(--color-bg-card); padding: 1.25rem; color: var(--color-text); font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.8rem; line-height: 1.6; }
.notes-editor { width: 100%; resize: vertical; border: 1px solid var(--color-border); border-radius: 0.5rem; background: var(--color-bg-surface); padding: 0.75rem; color: var(--color-text); font-family: inherit; font-size: 0.95rem; line-height: 1.6; }
.notes-editor:focus { border-color: var(--color-accent); outline: none; }
.detail-card { border: 1px solid var(--color-border); border-radius: 1rem; background: var(--color-bg-card); padding: 1.25rem; box-shadow: inset 0 1px 0 rgb(255 255 255 / 0.03); }
.detail-heading { margin-bottom: 0.85rem; color: var(--color-accent); font-size: 0.8rem; font-weight: 700; letter-spacing: 0.22em; text-transform: uppercase; }
.detail-list { display: grid; gap: 0; }
:deep(.detail-row) { display: grid; grid-template-columns: minmax(8rem, 0.7fr) minmax(0, 1fr); gap: 1rem; border-bottom: 1px solid var(--color-border); padding: 0.85rem 0; }
:deep(.detail-row:last-child) { border-bottom: 0; }
:deep(.detail-label) { color: var(--color-text-secondary); font-size: 0.95rem; }
:deep(.detail-value) { min-width: 0; overflow-wrap: anywhere; color: var(--color-text); font-size: 1rem; }
:deep(.detail-link) { color: var(--color-accent); }
:deep(.detail-link:hover) { text-decoration: underline; }
@media (min-width: 768px) { .detail-card { padding: 1.5rem 1.75rem; } }
</style>
