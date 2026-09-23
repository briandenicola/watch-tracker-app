<template>
  <div class="mx-auto max-w-4xl">
    <p class="mb-1 text-xs font-semibold uppercase tracking-[0.2em] text-accent">Reference</p>
    <h1 class="font-display text-3xl font-semibold text-text">Watch Field Guide</h1>
    <p class="mb-8 mt-2 max-w-2xl text-sm leading-6 text-text-secondary">
      A plain-language guide to the details you can record for watches in your collection and wish list.
      Values can be left blank when they do not apply or are not known.
    </p>
    <WatchHelpContent />
  </div>
</template>

<script setup lang="ts">
import WatchHelpContent from '@/components/common/WatchHelpContent.vue'
</script>
