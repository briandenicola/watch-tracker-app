import { ref, watch } from 'vue'

export type ThemeMode = 'dark' | 'light' | 'system'

const STORAGE_KEY = 'watch-tracker-theme'

const mode = ref<ThemeMode>((localStorage.getItem(STORAGE_KEY) as ThemeMode) || 'system')

function getEffectiveTheme(): 'dark' | 'light' {
  if (mode.value === 'system') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  }
  return mode.value
}

function applyTheme() {
  const effective = getEffectiveTheme()
  document.documentElement.setAttribute('data-theme', effective)
  // Update theme-color meta for PWA
  const meta = document.querySelector('meta[name="theme-color"]')
  if (meta) {
    meta.setAttribute('content', effective === 'dark' ? '#1a1a1a' : '#f8f6f3')
  }
}

// Watch for changes
watch(mode, (val) => {
  localStorage.setItem(STORAGE_KEY, val)
  applyTheme()
})

// Listen for system preference changes
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
  if (mode.value === 'system') applyTheme()
})

// Apply on load
applyTheme()

export function useTheme() {
  return {
    mode,
    setTheme(newMode: ThemeMode) {
      mode.value = newMode
    },
    getEffectiveTheme,
  }
}
