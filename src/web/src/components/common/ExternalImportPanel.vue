<template>
  <div class="mt-5 border-t border-border pt-5">
    <h4 class="text-sm font-medium text-text mb-1">Import from another app</h4>
    <p class="text-xs text-text-muted mb-3">
      Wristcheck CSV exports are supported. Review every row before anything is added.
    </p>

    <div class="flex flex-wrap items-center gap-3">
      <label class="px-4 py-2 bg-bg-surface border border-border text-text text-sm font-medium rounded-lg hover:border-accent/50 transition-colors cursor-pointer">
        {{ file?.name || 'Choose Wristcheck CSV' }}
        <input type="file" accept=".csv,text/csv" class="hidden" :disabled="busy" @change="chooseFile" />
      </label>
      <button
        type="button"
        class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
        :disabled="!file || busy"
        @click="loadPreview"
      >
        {{ previewing ? 'Reading...' : 'Preview Import' }}
      </button>
    </div>

    <p v-if="errorMessage" role="alert" class="mt-3 text-sm text-danger">{{ errorMessage }}</p>
    <p v-if="resultMessage" role="status" class="mt-3 text-sm text-success">{{ resultMessage }}</p>

    <div v-if="preview" class="mt-4 space-y-4">
      <div class="flex flex-wrap gap-2 text-xs">
        <span class="rounded-full bg-bg-surface border border-border px-3 py-1">{{ preview.source }}</span>
        <span class="rounded-full bg-bg-surface border border-border px-3 py-1">{{ preview.collectionCount }} collection</span>
        <span class="rounded-full bg-bg-surface border border-border px-3 py-1">{{ preview.wishlistCount }} wish list</span>
        <span class="rounded-full bg-bg-surface border border-border px-3 py-1">{{ preview.disposedCount }} former</span>
        <span v-if="preview.duplicateCount" class="rounded-full bg-warning/10 border border-warning/30 px-3 py-1 text-warning">
          {{ preview.duplicateCount }} likely duplicate{{ preview.duplicateCount === 1 ? '' : 's' }}
        </span>
      </div>

      <div class="overflow-x-auto rounded-lg border border-border">
        <table class="w-full min-w-[42rem] text-left text-sm">
          <thead class="bg-bg-surface text-xs text-text-muted">
            <tr>
              <th class="p-3">Import</th>
              <th class="p-3">Watch</th>
              <th class="p-3">Destination</th>
              <th class="p-3">Review</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-border">
            <tr v-for="row in preview.rows" :key="row.rowNumber" :class="{ 'bg-danger/5': !row.canImport }">
              <td class="p-3 align-top">
                <input
                  v-model="selectedRows"
                  type="checkbox"
                  :value="row.rowNumber"
                  :disabled="!row.canImport || importing"
                  :aria-label="`Import ${row.brand} ${row.model}`"
                />
              </td>
              <td class="p-3 align-top">
                <p class="font-medium text-text">{{ row.brand }} {{ row.model }}</p>
                <p class="text-xs text-text-muted">CSV row {{ row.rowNumber }}</p>
              </td>
              <td class="p-3 align-top text-text-secondary">{{ row.destination }}</td>
              <td class="p-3 align-top">
                <p v-if="row.isDuplicate" class="text-warning">
                  Likely duplicate<span v-if="row.duplicateWatchId"> of watch #{{ row.duplicateWatchId }}</span>:
                  {{ row.duplicateReason }}
                </p>
                <p v-for="warning in row.warnings" :key="warning" class="text-warning">{{ warning }}</p>
                <p v-for="rowError in row.errors" :key="rowError" class="text-danger">{{ rowError }}</p>
                <p v-if="!row.isDuplicate && !row.warnings.length && !row.errors.length" class="text-success">Ready</p>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <button
          type="button"
          class="px-4 py-2 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          :disabled="selectedRows.length === 0 || importing"
          @click="runImport"
        >
          {{ importing ? 'Importing...' : `Import ${selectedRows.length} Selected` }}
        </button>
        <p class="text-xs text-text-muted">Likely duplicates are unchecked by default; select one to import it anyway.</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ExternalImportPreview } from '@/types'
import { importExternalData, previewExternalImport } from '@/services/dataImport'
import { serverMessage } from '@/utils/serverMessage'

const emit = defineEmits<{ imported: [] }>()

const file = ref<File | null>(null)
const preview = ref<ExternalImportPreview | null>(null)
const selectedRows = ref<number[]>([])
const previewing = ref(false)
const importing = ref(false)
const errorMessage = ref('')
const resultMessage = ref('')
const busy = computed(() => previewing.value || importing.value)

function chooseFile(event: Event) {
  file.value = (event.target as HTMLInputElement).files?.[0] || null
  preview.value = null
  selectedRows.value = []
  errorMessage.value = ''
  resultMessage.value = ''
}

async function loadPreview() {
  if (!file.value) return
  previewing.value = true
  errorMessage.value = ''
  resultMessage.value = ''
  try {
    preview.value = await previewExternalImport(file.value)
    selectedRows.value = preview.value.rows
      .filter(row => row.canImport && !row.isDuplicate)
      .map(row => row.rowNumber)
  } catch (error) {
    preview.value = null
    selectedRows.value = []
    errorMessage.value = serverMessage(error) || 'Could not read this export.'
  } finally {
    previewing.value = false
  }
}

async function runImport() {
  if (!file.value || selectedRows.value.length === 0) return
  importing.value = true
  errorMessage.value = ''
  resultMessage.value = ''
  try {
    const result = await importExternalData(file.value, selectedRows.value)
    resultMessage.value = `Imported ${result.imported} watch${result.imported === 1 ? '' : 'es'}; skipped ${result.skipped}.`
    preview.value = null
    selectedRows.value = []
    emit('imported')
  } catch (error) {
    errorMessage.value = serverMessage(error) || 'Could not import the selected watches.'
  } finally {
    importing.value = false
  }
}
</script>
