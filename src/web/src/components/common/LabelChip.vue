<template>
  <span
    class="inline-flex max-w-full items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium"
    :style="{ borderColor: `color-mix(in srgb, ${label.color} 55%, transparent)`, backgroundColor: `color-mix(in srgb, ${label.color} 14%, transparent)` }"
  >
    <span class="h-2 w-2 shrink-0 rounded-full" :style="{ backgroundColor: label.color }" aria-hidden="true" />
    <span class="truncate text-text">{{ label.name }}</span>
  </span>
</template>

<script setup lang="ts">
import type { Label } from '@/types'

defineProps<{ label: Pick<Label, 'name' | 'color'> }>()
</script>
