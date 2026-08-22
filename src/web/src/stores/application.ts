import { ref } from 'vue'
import { api } from '@/services/api'
import { setApplicationTimeZone } from '@/utils/dateTime'

const ready = ref(false)
let loadPromise: Promise<void> | undefined

export function useApplicationSettings() {
  async function load() {
    if (ready.value) return
    loadPromise ??= api.get<{ timeZone: string }>('/api/configuration')
      .then(response => setApplicationTimeZone(response.data.timeZone))
      .catch(() => {
        // The date utility keeps its safe application default when configuration is unavailable.
      })
      .finally(() => {
        ready.value = true
      })
    await loadPromise
  }

  return { ready, load }
}
