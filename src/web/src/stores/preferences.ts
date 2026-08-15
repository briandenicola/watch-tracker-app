import { ref, watch } from 'vue'

export type SortOption = 'dateAdded' | 'brand' | 'lastWorn' | 'timesWorn' | 'priority'

const STORAGE_KEY = 'watch-tracker-preferences'

interface Preferences {
  collectionDefaultSort: Exclude<SortOption, 'priority'>
  wishlistDefaultSort: SortOption
}

const defaults: Preferences = {
  collectionDefaultSort: 'dateAdded',
  wishlistDefaultSort: 'priority',
}

function load(): Preferences {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) {
      const stored = JSON.parse(raw) as Partial<Preferences> & { defaultSort?: SortOption }
      const legacySort = stored.defaultSort
      return {
        collectionDefaultSort: stored.collectionDefaultSort
          ?? (legacySort === 'priority' ? 'dateAdded' : legacySort)
          ?? defaults.collectionDefaultSort,
        wishlistDefaultSort: stored.wishlistDefaultSort
          ?? legacySort
          ?? defaults.wishlistDefaultSort,
      }
    }
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
    setCollectionDefaultSort(sort: Exclude<SortOption, 'priority'>) {
      prefs.value.collectionDefaultSort = sort
    },
    setWishlistDefaultSort(sort: SortOption) {
      prefs.value.wishlistDefaultSort = sort
    },
  }
}
