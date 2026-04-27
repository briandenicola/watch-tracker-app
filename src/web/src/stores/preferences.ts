import { ref, watch } from 'vue'

export type SortOption = 'dateAdded' | 'brand' | 'lastWorn' | 'timesWorn'

const STORAGE_KEY = 'watch-tracker-preferences'

interface Preferences {
  defaultSort: SortOption
}

const defaults: Preferences = { defaultSort: 'dateAdded' }

function load(): Preferences {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) return { ...defaults, ...JSON.parse(raw) }
  } catch { /* ignore */ }
  return { ...defaults }
}

const prefs = ref<Preferences>(load())

watch(prefs, (val) => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(val))
}, { deep: true })

export function usePreferences() {
  return {
    prefs,
    setDefaultSort(sort: SortOption) {
      prefs.value.defaultSort = sort
    },
  }
}
