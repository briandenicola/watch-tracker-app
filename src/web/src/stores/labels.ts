import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { Label, SaveLabel, SavedView, SaveSavedView } from '@/types'
import * as labelApi from '@/services/labels'

export const PIN_LIMIT = 5

// Offered in order when creating a label, so new labels start out distinguishable.
export const LABEL_COLORS = ['#c4a35a', '#38bdf8', '#14b8a6', '#a78bfa', '#ef6b6b', '#84cc16', '#ec4899', '#94a3b8']

/** Labels and saved views are shared by the sidebar, the collection and the watch pages. */
export const useLabelsStore = defineStore('labels', () => {
  const labels = ref<Label[]>([])
  const views = ref<SavedView[]>([])
  const loaded = ref(false)
  let loading: Promise<void> | null = null

  const labelsById = computed(() => new Map(labels.value.map(label => [label.id, label])))
  const pinnedViews = computed(() => views.value.filter(view => view.pinned))
  const pinLimitReached = computed(() => pinnedViews.value.length >= PIN_LIMIT)

  function load(force = false): Promise<void> {
    if (loaded.value && !force) return Promise.resolve()
    loading ??= Promise.all([labelApi.getLabels(), labelApi.getSavedViews()])
      .then(([nextLabels, nextViews]) => {
        labels.value = nextLabels
        views.value = nextViews
        loaded.value = true
      })
      .finally(() => { loading = null })
    return loading
  }

  function nextColor(): string {
    return LABEL_COLORS[labels.value.length % LABEL_COLORS.length]
  }

  function sortLabels() {
    labels.value.sort((a, b) => a.name.localeCompare(b.name))
  }

  async function createLabel(label: SaveLabel): Promise<Label> {
    const created = await labelApi.createLabel({ color: nextColor(), ...label })
    labels.value.push(created)
    sortLabels()
    return created
  }

  async function updateLabel(id: number, label: SaveLabel): Promise<void> {
    const updated = await labelApi.updateLabel(id, label)
    labels.value = labels.value.map(existing => existing.id === id ? updated : existing)
    sortLabels()
  }

  async function deleteLabel(id: number): Promise<void> {
    await labelApi.deleteLabel(id)
    labels.value = labels.value.filter(label => label.id !== id)
    views.value = views.value.map(view => ({ ...view, labelIds: view.labelIds.filter(labelId => labelId !== id) }))
  }

  /** Saves a watch's labels and keeps each label's watch count in step. */
  async function setWatchLabels(watchId: number, previous: number[], next: number[]): Promise<number[]> {
    const saved = await labelApi.setWatchLabels(watchId, next)
    const before = new Set(previous)
    const after = new Set(saved)
    labels.value = labels.value.map(label => {
      const delta = Number(after.has(label.id)) - Number(before.has(label.id))
      return delta ? { ...label, watchCount: label.watchCount + delta } : label
    })
    return saved
  }

  function replaceView(view: SavedView) {
    const others = views.value.filter(existing => existing.id !== view.id)
    views.value = sortViews([...others, view])
  }

  async function createView(view: SaveSavedView): Promise<SavedView> {
    const created = await labelApi.createSavedView(view)
    replaceView(created)
    return created
  }

  async function updateView(id: number, view: SaveSavedView): Promise<SavedView> {
    const updated = await labelApi.updateSavedView(id, view)
    replaceView(updated)
    return updated
  }

  async function setPinned(id: number, pinned: boolean): Promise<void> {
    replaceView(await labelApi.setSavedViewPinned(id, pinned))
  }

  async function deleteView(id: number): Promise<void> {
    await labelApi.deleteSavedView(id)
    views.value = views.value.filter(view => view.id !== id)
  }

  function clear() {
    labels.value = []
    views.value = []
    loaded.value = false
  }

  return {
    labels, views, loaded, labelsById, pinnedViews, pinLimitReached,
    load, createLabel, updateLabel, deleteLabel, setWatchLabels,
    createView, updateView, setPinned, deleteView, clear,
  }
})

// Same order as the API: pinned in the order they were pinned, then the rest by name.
function sortViews(views: SavedView[]): SavedView[] {
  return views.sort((a, b) => {
    if (a.pinned !== b.pinned) return a.pinned ? -1 : 1
    if (a.pinned && b.pinned) return Date.parse(a.pinnedAt ?? '') - Date.parse(b.pinnedAt ?? '')
    return a.name.localeCompare(b.name, undefined, { sensitivity: 'base' })
  })
}
