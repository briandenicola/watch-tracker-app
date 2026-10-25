import { ref, watch } from 'vue'

export interface ThemeInfo {
  label: string
  /** Background, surface and accent, for the swatch in the picker. */
  swatch: readonly [string, string, string]
  light: boolean
}

// Order here is the order in the Settings picker and the sidebar toggle.
export const THEMES = {
  dark: { label: 'Dark', swatch: ['#1a1a1a', '#242424', '#c4a35a'], light: false },
  light: { label: 'Light', swatch: ['#f8f6f3', '#ffffff', '#9e7c3c'], light: true },
  aurea: { label: 'Aurea', swatch: ['#0f0f1a', '#16213e', '#c9a84c'], light: false },
  parchment: { label: 'Parchment', swatch: ['#f5f0e8', '#ffffff', '#9a7b2e'], light: true },
  'british-museum': { label: 'British Museum', swatch: ['#0c0c0b', '#1a1a19', '#d8d4cc'], light: false },
  louvre: { label: 'Louvre', swatch: ['#1a1510', '#2a2218', '#977b3c'], light: false },
  capitoline: { label: 'Capitoline', swatch: ['#0a0808', '#8e1f2f', '#f0bc42'], light: false },
  byzantine: { label: 'Byzantine', swatch: ['#100a08', '#ba0e09', '#ffbf00'], light: false },
  'modern-greek': { label: 'Modern Greek', swatch: ['#060d18', '#0d5eaf', '#ffffff'], light: false },
} as const satisfies Record<string, ThemeInfo>

export type EffectiveTheme = keyof typeof THEMES
export type ThemeMode = EffectiveTheme | 'system'

export const BASE_THEMES: ThemeMode[] = ['system', 'dark', 'light']
export const MUSEUM_THEMES: ThemeMode[] = ['aurea', 'parchment', 'british-museum', 'louvre', 'capitoline', 'byzantine', 'modern-greek']

export const THEME_LABELS: Record<ThemeMode, string> = {
  system: 'System',
  ...(Object.fromEntries(Object.entries(THEMES).map(([id, t]) => [id, t.label])) as Record<EffectiveTheme, string>),
}

const STORAGE_KEY = 'watch-tracker-theme'

function isThemeMode(value: string | null): value is ThemeMode {
  return value === 'system' || (value !== null && value in THEMES)
}

// A theme saved by an older build may no longer exist; fall back to System.
const stored = localStorage.getItem(STORAGE_KEY)
const mode = ref<ThemeMode>(isThemeMode(stored) ? stored : 'system')

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
    meta.setAttribute('content', THEMES[effective].swatch[0])
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
    isLight: () => THEMES[getEffectiveTheme()].light,
  }
}
