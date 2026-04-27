import { ref, onMounted, onUnmounted } from 'vue'

/**
 * Pull-to-refresh composable for PWA pages.
 * Attaches to the window and triggers a callback when the user
 * pulls down from the top of the page.
 * Ignores horizontal swipes to avoid interfering with swipe navigation.
 */
export function usePullToRefresh(onRefresh: () => Promise<void>) {
  const refreshing = ref(false)
  const pullDistance = ref(0)
  const pulling = ref(false)

  const THRESHOLD = 80
  let startX = 0
  let startY = 0
  let isAtTop = false
  let direction: 'vertical' | 'horizontal' | null = null

  function onTouchStart(e: TouchEvent) {
    if (window.scrollY <= 0) {
      startX = e.touches[0].clientX
      startY = e.touches[0].clientY
      isAtTop = true
      direction = null
    } else {
      isAtTop = false
    }
  }

  function onTouchMove(e: TouchEvent) {
    if (!isAtTop || refreshing.value) return
    const dx = e.touches[0].clientX - startX
    const dy = e.touches[0].clientY - startY

    // Determine direction after 8px of movement
    if (direction === null && (Math.abs(dx) > 8 || Math.abs(dy) > 8)) {
      direction = Math.abs(dy) > Math.abs(dx) ? 'vertical' : 'horizontal'
    }

    // Only activate pull-to-refresh for vertical downward gestures
    if (direction === 'horizontal') return
    if (direction === 'vertical' && dy > 0) {
      pulling.value = true
      pullDistance.value = Math.min(dy * 0.5, THRESHOLD * 1.5) // dampen
    }
  }

  async function onTouchEnd() {
    if (!pulling.value) {
      direction = null
      return
    }
    if (pullDistance.value >= THRESHOLD) {
      refreshing.value = true
      try {
        await onRefresh()
      } finally {
        refreshing.value = false
      }
    }
    pullDistance.value = 0
    pulling.value = false
    direction = null
  }

  onMounted(() => {
    document.addEventListener('touchstart', onTouchStart, { passive: true })
    document.addEventListener('touchmove', onTouchMove, { passive: true })
    document.addEventListener('touchend', onTouchEnd)
  })

  onUnmounted(() => {
    document.removeEventListener('touchstart', onTouchStart)
    document.removeEventListener('touchmove', onTouchMove)
    document.removeEventListener('touchend', onTouchEnd)
  })

  return { refreshing, pullDistance, pulling }
}
