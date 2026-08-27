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

    <!-- Recommendation: sparkle over a watch dial -->
    <template v-else-if="name === 'recommend'">
      <circle cx="11" cy="13" r="6" />
      <polyline points="11 10 11 13 13 14" />
      <path d="M18 2l.7 2.3L21 5l-2.3.7L18 8l-.7-2.3L15 5l2.3-.7z" />
      <line x1="9" y1="7" x2="9" y2="4" />
      <line x1="13" y1="19" x2="13" y2="22" />
    </template>

    <!-- Advisor: conversational sparkle -->
    <template v-else-if="name === 'advisor'">
      <path d="M4 5.5h12a3 3 0 0 1 3 3v4a3 3 0 0 1-3 3H9l-4.5 3v-3.5A3 3 0 0 1 2 12V8.5a3 3 0 0 1 2-3z" />
      <path d="M14.5 1.5l.6 1.9L17 4l-1.9.6-.6 1.9-.6-1.9L12 4l1.9-.6z" />
      <line x1="7" y1="10.5" x2="14" y2="10.5" />
    </template>

    <!-- Review: a written report over ruled findings -->
    <template v-else-if="name === 'review'">
      <path d="M5 3.5h9.5L19 8v12.5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-16a1 1 0 0 1 1-1z" />
      <polyline points="14 3.5 14 8.5 19 8.5" />
      <line x1="7.5" y1="12" x2="15.5" y2="12" />
      <line x1="7.5" y1="15.5" x2="13" y2="15.5" />
      <line x1="7.5" y1="19" x2="15.5" y2="19" />
    </template>

    <!-- Storage: shelf / case display -->
    <template v-else-if="name === 'storage'">
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <line x1="3" y1="12" x2="21" y2="12" />
      <circle cx="8" cy="8.5" r="1.4" />
      <circle cx="16" cy="8.5" r="1.4" />
      <circle cx="12" cy="15.5" r="1.4" />
    </template>

    <!-- Retired: watch on cushion / resting -->
    <template v-else-if="name === 'retired'">
      <ellipse cx="12" cy="16" rx="9" ry="4" />
      <circle cx="12" cy="10" r="5" />
      <polyline points="12 8 12 10 13.5 11" />
    </template>

    <!-- Wear Log: calendar with watch mark -->
    <template v-else-if="name === 'wear-log'">
      <rect x="3" y="4" width="18" height="17" rx="2" />
      <line x1="3" y1="9" x2="21" y2="9" />
      <line x1="8" y1="2" x2="8" y2="6" />
      <line x1="16" y1="2" x2="16" y2="6" />
      <circle cx="12" cy="15" r="3" />
      <polyline points="12 13.5 12 15 13.2 15.8" />
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

    <!-- Bell -->
    <template v-else-if="name === 'bell'">
      <path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
      <path d="M10 21h4" />
    </template>

    <!-- Edit -->
    <template v-else-if="name === 'edit'">
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4z" />
    </template>

    <!-- Check -->
    <template v-else-if="name === 'check'">
      <polyline points="20 6 9 17 4 12" />
    </template>

    <!-- Chevron Left -->
    <template v-else-if="name === 'chevron-left'">
      <polyline points="15 18 9 12 15 6" />
    </template>

    <!-- Chevron Right -->
    <template v-else-if="name === 'chevron-right'">
      <polyline points="9 6 15 12 9 18" />
    </template>

    <!-- Compact grid: many small tiles -->
    <template v-else-if="name === 'grid'">
      <rect x="3" y="3" width="7" height="7" rx="1.5" />
      <rect x="14" y="3" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <rect x="14" y="14" width="7" height="7" rx="1.5" />
    </template>

    <!-- Cards: one large tile with a caption -->
    <template v-else-if="name === 'cards'">
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <line x1="3" y1="15" x2="21" y2="15" />
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
