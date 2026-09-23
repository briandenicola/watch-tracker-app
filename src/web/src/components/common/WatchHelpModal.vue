<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-[80] flex items-end justify-center bg-black/60 sm:items-center sm:p-4" @click.self="emit('close')">
      <div
        ref="dialogEl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="watch-help-title"
        tabindex="-1"
        class="max-h-[92vh] w-full overflow-y-auto rounded-t-2xl border border-border bg-bg sm:max-w-3xl sm:rounded-2xl focus:outline-none"
        @keydown.esc="emit('close')"
      >
        <header class="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-border bg-bg/95 p-5 backdrop-blur">
          <div>
            <p class="mb-1 text-[0.65rem] uppercase tracking-[0.24em] text-accent">Reference</p>
            <h2 id="watch-help-title" class="font-display text-2xl font-semibold text-text">Watch Field Guide</h2>
            <p class="mt-1 text-sm text-text-muted">Plain-language explanations for the details you can record.</p>
          </div>
          <button type="button" class="inline-flex h-11 w-11 flex-shrink-0 items-center justify-center text-text-muted hover:text-text" aria-label="Close field guide" @click="emit('close')">
            <AppIcon name="close" :size="20" :stroke-width="2" />
          </button>
        </header>
        <div class="p-5">
          <WatchHelpContent />
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AppIcon from '@/components/icons/AppIcon.vue'
import WatchHelpContent from '@/components/common/WatchHelpContent.vue'

const emit = defineEmits<{ close: [] }>()
const dialogEl = ref<HTMLElement | null>(null)

onMounted(() => dialogEl.value?.focus())
</script>
