import { ref, onMounted, onUnmounted } from 'vue'

/**
 * Pull-to-refresh composable for PWA pages.
 * Attaches to the window and triggers a callback when the user
 * pulls down from the top of the page.
 */
export function usePullToRefresh(onRefresh: () => Promise<void>) {
  const refreshing = ref(false)
  const pullDistance = ref(0)
  const pulling = ref(false)

  const THRESHOLD = 80
  let startY = 0
  let isAtTop = false

  function onTouchStart(e: TouchEvent) {
    if (window.scrollY <= 0) {
      startY = e.touches[0].clientY
      isAtTop = true
    } else {
      isAtTop = false
    }
  }

  function onTouchMove(e: TouchEvent) {
    if (!isAtTop || refreshing.value) return
    const dy = e.touches[0].clientY - startY
    if (dy > 0) {
      pulling.value = true
      pullDistance.value = Math.min(dy * 0.5, THRESHOLD * 1.5) // dampen
    }
  }

  async function onTouchEnd() {
    if (!pulling.value) return
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
