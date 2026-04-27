<template>
  <svg xmlns="http://www.w3.org/2000/svg" :width="size" :height="size" viewBox="0 0 24 24" fill="none" :stroke="color" :stroke-width="strokeWidth" stroke-linecap="round" stroke-linejoin="round">
    <!-- Collection: elegant vitrine / display shelves -->
    <template v-if="name === 'collection'">
      <rect x="2" y="3" width="20" height="18" rx="2" />
      <line x1="2" y1="10" x2="22" y2="10" />
      <line x1="2" y1="17" x2="22" y2="17" />
      <circle cx="8" cy="6.5" r="1.5" />
      <circle cx="16" cy="6.5" r="1.5" />
      <circle cx="12" cy="13.5" r="1.5" />
    </template>

    <!-- Statistics: elegant rising line with dot accents -->
    <template v-else-if="name === 'stats'">
      <polyline points="3 20 8 14 12 16 17 9 21 4" />
      <circle cx="8" cy="14" r="1" :fill="color" stroke="none" />
      <circle cx="12" cy="16" r="1" :fill="color" stroke="none" />
      <circle cx="17" cy="9" r="1" :fill="color" stroke="none" />
    </template>

    <!-- Retired: watch on cushion / resting -->
    <template v-else-if="name === 'retired'">
      <ellipse cx="12" cy="16" rx="9" ry="4" />
      <circle cx="12" cy="10" r="5" />
      <polyline points="12 8 12 10 13.5 11" />
    </template>

    <!-- Settings: elegant dial with indicators -->
    <template v-else-if="name === 'settings'">
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="1.5" :fill="color" stroke="none" />
      <line x1="12" y1="3" x2="12" y2="5" />
      <line x1="12" y1="19" x2="12" y2="21" />
      <line x1="3" y1="12" x2="5" y2="12" />
      <line x1="19" y1="12" x2="21" y2="12" />
      <line x1="12" y1="12" x2="8" y2="8" />
    </template>

    <!-- Admin: crown (Rolex-inspired) -->
    <template v-else-if="name === 'admin'">
      <path d="M3 17l2-10 4 4 3-6 3 6 4-4 2 10z" />
      <line x1="3" y1="17" x2="21" y2="17" />
      <circle cx="6" cy="19" r="0.5" :fill="color" stroke="none" />
      <circle cx="12" cy="19" r="0.5" :fill="color" stroke="none" />
      <circle cx="18" cy="19" r="0.5" :fill="color" stroke="none" />
    </template>

    <!-- Plus: refined cross -->
    <template v-else-if="name === 'plus'">
      <line x1="12" y1="5" x2="12" y2="19" />
      <line x1="5" y1="12" x2="19" y2="12" />
    </template>

    <!-- Watch: elegant timepiece with lugs -->
    <template v-else-if="name === 'watch'">
      <circle cx="12" cy="12" r="7" />
      <circle cx="12" cy="12" r="5.5" />
      <polyline points="12 8.5 12 12 14 13.5" />
      <line x1="10" y1="5" x2="10" y2="3" />
      <line x1="14" y1="5" x2="14" y2="3" />
      <line x1="10" y1="19" x2="10" y2="21" />
      <line x1="14" y1="19" x2="14" y2="21" />
    </template>

    <!-- Sun (light mode) -->
    <template v-else-if="name === 'sun'">
      <circle cx="12" cy="12" r="4" />
      <line x1="12" y1="2" x2="12" y2="4" />
      <line x1="12" y1="20" x2="12" y2="22" />
      <line x1="4.93" y1="4.93" x2="6.34" y2="6.34" />
      <line x1="17.66" y1="17.66" x2="19.07" y2="19.07" />
      <line x1="2" y1="12" x2="4" y2="12" />
      <line x1="20" y1="12" x2="22" y2="12" />
      <line x1="4.93" y1="19.07" x2="6.34" y2="17.66" />
      <line x1="17.66" y1="6.34" x2="19.07" y2="4.93" />
    </template>

    <!-- Moon (dark mode) -->
    <template v-else-if="name === 'moon'">
      <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
    </template>

    <!-- Menu: refined three lines with tapered lengths -->
    <template v-else-if="name === 'menu'">
      <line x1="4" y1="7" x2="20" y2="7" />
      <line x1="4" y1="12" x2="16" y2="12" />
      <line x1="4" y1="17" x2="20" y2="17" />
    </template>

    <!-- Close -->
    <template v-else-if="name === 'close'">
      <line x1="18" y1="6" x2="6" y2="18" />
      <line x1="6" y1="6" x2="18" y2="18" />
    </template>

    <!-- Chevron Left -->
    <template v-else-if="name === 'chevron-left'">
      <polyline points="15 18 9 12 15 6" />
    </template>

    <!-- Chevron Right -->
    <template v-else-if="name === 'chevron-right'">
      <polyline points="9 6 15 12 9 18" />
    </template>

    <!-- Sign Out: door with arrow -->
    <template v-else-if="name === 'sign-out'">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" y1="12" x2="9" y2="12" />
    </template>
  </svg>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  name: string
  size?: number | string
  color?: string
  strokeWidth?: number | string
}>(), {
  size: 24,
  color: 'currentColor',
  strokeWidth: 1.25,
})
</script>
