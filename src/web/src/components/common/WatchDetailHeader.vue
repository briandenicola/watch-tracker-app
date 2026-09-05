<template>
  <header class="mb-5">
    <div class="mb-2 grid grid-cols-[1fr_auto_1fr] items-center gap-3">
      <RouterLink :to="backTo" class="justify-self-start text-sm text-accent hover:underline">← Back</RouterLink>
      <p class="collection-label"><span>{{ watch.isWishList ? 'Wish List' : watch.disposition ? dispositionLabel(watch) : 'Collection' }}</span></p>
      <span aria-hidden="true"></span>
    </div>

    <div class="relative flex items-start justify-between gap-4">
      <div class="min-w-0">
        <h1 ref="titleEl" class="font-display text-3xl font-semibold leading-tight text-text" :class="{ 'title-stacked': titleStacked }">
          <span class="watch-brand">{{ watch.brand }}</span> {{ watch.model }}
          <span class="title-probe-clip" aria-hidden="true"><span ref="titleProbeEl" class="title-probe">{{ watch.brand }} {{ watch.model }}</span></span>
        </h1>
      </div>
      <div class="relative flex flex-shrink-0 items-center gap-2">
        <template v-if="editMode">
          <button @click="emit('save-edits')" :disabled="savingEdits" class="header-action text-success" aria-label="Save edits" title="Save edits">
            <AppIcon name="check" :size="20" :stroke-width="2" />
          </button>
          <button @click="emit('discard-edits')" :disabled="savingEdits" class="header-action text-danger" aria-label="Discard edits" title="Discard edits">
            <AppIcon name="close" :size="20" :stroke-width="2" />
          </button>
        </template>
        <button v-else @click="run('edit')" class="header-action" aria-label="Edit watch" title="Edit watch">
          <AppIcon name="edit" :size="20" :stroke-width="1.75" />
        </button>
        <button @click="actionsOpen = !actionsOpen" class="header-action text-xl leading-none" aria-label="Watch actions">…</button>
        <div v-if="actionsOpen" class="absolute right-0 top-12 z-30 w-56 overflow-hidden rounded-xl border border-border bg-bg-card shadow-xl">
          <template v-if="!watch.isWishList">
            <button v-if="!watch.disposition" @click="run('wear')" :disabled="wearLoading" class="menu-action text-accent">{{ wearLoading ? 'Recording...' : 'Wore Today' }}</button>
            <label class="menu-action cursor-pointer">
              {{ uploading ? 'Uploading…' : 'Upload Images' }}
              <input type="file" accept="image/*" multiple class="hidden" :disabled="uploading" @change="upload" />
            </label>
            <button @click="run('analyze')" :disabled="analyzing || !watch.imageUrls.length" class="menu-action">{{ analyzing ? 'Analyzing…' : 'AI Analyze' }}</button>
            <button @click="run('style')" class="menu-action">Style Agent</button>
            <button @click="run('share')" class="menu-action">Share</button>
            <button @click="run('refresh-resale')" :disabled="refreshingResale" class="menu-action">{{ refreshingResale ? 'Queuing…' : 'Refresh Resale' }}</button>
            <button @click="run('disposition')" class="menu-action">{{ watch.disposition ? 'Edit disposition' : 'Remove from collection' }}</button>
            <button v-if="watch.disposition" @click="run('restore')" class="menu-action text-accent">Restore to collection</button>
            <button @click="run('delete')" class="menu-action text-danger">Delete</button>
          </template>
          <template v-else>
            <button @click="run('purchase')" :disabled="purchasing" class="menu-action text-accent">{{ purchasing ? 'Moving…' : 'Mark Purchased' }}</button>
            <button @click="run('style')" class="menu-action">Style Agent</button>
            <button @click="run('share')" class="menu-action">Share</button>
            <label class="menu-action cursor-pointer">
              {{ uploading ? 'Uploading…' : 'Upload Images' }}
              <input type="file" accept="image/*" multiple class="hidden" :disabled="uploading" @change="upload" />
            </label>
            <button @click="run('analyze')" :disabled="analyzing || !watch.imageUrls.length" class="menu-action">{{ analyzing ? 'Analyzing…' : 'AI Analyze' }}</button>
            <button @click="run('delete')" class="menu-action text-danger">Delete</button>
          </template>
        </div>
      </div>
    </div>

    <p v-if="editSessionError" class="mt-4 text-sm text-danger">{{ editSessionError }}</p>
    <p v-if="analysisError" class="mt-2 text-sm text-danger">{{ analysisError }}</p>
  </header>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch as vueWatch } from 'vue'
import { RouterLink } from 'vue-router'
import type { Watch } from '@/types'
import AppIcon from '@/components/icons/AppIcon.vue'
import { dispositionLabel } from '@/composables/useWatchDetailEditor'

const props = defineProps<{
  watch: Watch
  backTo: string
  editMode: boolean
  savingEdits: boolean
  editSessionError: string
  wearLoading: boolean
  uploading: boolean
  analyzing: boolean
  purchasing: boolean
  refreshingResale: boolean
  analysisError: string
}>()

type HeaderAction = 'edit' | 'wear' | 'analyze' | 'style' | 'share' | 'refresh-resale' | 'disposition' | 'restore' | 'delete' | 'purchase'

const emit = defineEmits<{
  (event: HeaderAction): void
  (event: 'save-edits' | 'discard-edits'): void
  (event: 'upload', files: File[]): void
}>()

const actionsOpen = ref(false)
const titleEl = ref<HTMLElement | null>(null)
const titleProbeEl = ref<HTMLElement | null>(null)
const titleStacked = ref(false)
let titleObserver: ResizeObserver | null = null

function measureTitle() {
  const el = titleEl.value
  const probe = titleProbeEl.value
  if (!el || !probe) return
  titleStacked.value = probe.getBoundingClientRect().width > el.clientWidth + 0.5
}

function run(event: HeaderAction) {
  actionsOpen.value = false
  emit(event)
}

function upload(event: Event) {
  const input = event.target as HTMLInputElement
  const files = Array.from(input.files ?? [])
  if (files.length) emit('upload', files)
  input.value = ''
}

vueWatch(titleEl, (element) => {
  titleObserver?.disconnect()
  titleObserver = null
  if (!element) return
  titleObserver = new ResizeObserver(() => measureTitle())
  titleObserver.observe(element)
})

onMounted(() => { document.fonts?.ready.then(measureTitle) })
vueWatch(() => `${props.watch.brand} ${props.watch.model}`, () => nextTick(measureTitle))
onBeforeUnmount(() => titleObserver?.disconnect())
</script>

<style scoped>
.collection-label { display: inline-flex; min-width: 0; max-width: 100%; align-items: center; gap: 0.7rem; font-family: var(--font-display); font-size: 0.95rem; font-weight: 500; text-transform: uppercase; color: var(--color-accent); }
.collection-label span { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; letter-spacing: 0.3em; margin-right: -0.3em; }
.collection-label::before, .collection-label::after { content: ''; display: block; flex: none; width: 1.75rem; height: 1px; background: linear-gradient(90deg, transparent, color-mix(in srgb, var(--color-accent) 70%, transparent)); }
.collection-label::after { background: linear-gradient(90deg, color-mix(in srgb, var(--color-accent) 70%, transparent), transparent); }
.title-stacked .watch-brand { display: block; }
.title-probe-clip { position: absolute; top: 0; left: 0; width: 0; height: 0; overflow: hidden; }
.title-probe { display: inline-block; visibility: hidden; white-space: nowrap; pointer-events: none; }
.header-action { display: inline-flex; width: 2.75rem; height: 2.75rem; align-items: center; justify-content: center; border: 1px solid var(--color-border); border-radius: 0.5rem; background: var(--color-bg-surface); color: var(--color-text); transition: border-color 0.15s ease, color 0.15s ease; }
.header-action:hover:not(:disabled), .header-action:focus-visible { border-color: color-mix(in srgb, var(--color-accent) 50%, transparent); color: var(--color-accent); }
.header-action:disabled { cursor: default; opacity: 0.5; }
.menu-action { display: block; width: 100%; padding: 0.75rem 1rem; text-align: left; color: var(--color-text); font-size: 0.9rem; font-weight: 500; white-space: nowrap; transition: background-color 0.15s ease, color 0.15s ease; }
.menu-action:hover { background: var(--color-bg-surface); color: var(--color-accent); }
.menu-action:disabled { cursor: not-allowed; opacity: 0.55; }
</style>
