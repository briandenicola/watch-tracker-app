import { ref, watch } from 'vue'

export type ThemeMode = 'dark' | 'light' | 'burnt-orange' | 'system'
export type EffectiveTheme = Exclude<ThemeMode, 'system'>

const STORAGE_KEY = 'watch-tracker-theme'

const META_COLORS: Record<EffectiveTheme, string> = {
  dark: '#1a1a1a',
  light: '#f8f6f3',
  'burnt-orange': '#2b1a10',
}

export const THEME_LABELS: Record<ThemeMode, string> = {
  light: 'Light',
  dark: 'Dark',
  'burnt-orange': 'Burnt Orange',
  system: 'System',
}

const mode = ref<ThemeMode>((localStorage.getItem(STORAGE_KEY) as ThemeMode) || 'system')

function getEffectiveTheme(): EffectiveTheme {
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
    meta.setAttribute('content', META_COLORS[effective])
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
