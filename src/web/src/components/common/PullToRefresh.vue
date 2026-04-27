<template>
  <Transition name="pull-indicator">
    <div
      v-if="pulling || refreshing"
      class="flex items-center justify-center py-3 -mt-2 mb-2"
    >
      <div
        class="w-8 h-8 flex items-center justify-center rounded-full border border-border bg-bg-surface"
        :class="{ 'animate-spin': refreshing }"
      >
        <svg
          v-if="refreshing"
          class="w-4 h-4 text-accent"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          stroke-width="2"
        >
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
        </svg>
        <svg
          v-else
          class="w-4 h-4 text-text-muted transition-transform"
          :style="{ transform: `rotate(${Math.min(pullProgress, 1) * 180}deg)` }"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          stroke-width="2"
        >
          <path stroke-linecap="round" stroke-linejoin="round" d="M19 14l-7 7m0 0l-7-7m7 7V3" />
        </svg>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  pulling: boolean
  refreshing: boolean
  pullDistance: number
}>()

const pullProgress = computed(() => props.pullDistance / 80)
</script>

<style scoped>
.pull-indicator-enter-active,
.pull-indicator-leave-active {
  transition: all 0.2s ease;
}
.pull-indicator-enter-from,
.pull-indicator-leave-to {
  opacity: 0;
  transform: translateY(-1rem);
}
</style>
