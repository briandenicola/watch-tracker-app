import type { LabelMatchMode } from '@/types'

/** Whether a watch's labels satisfy a label filter. An empty filter matches every watch. */
export function matchesLabels(watchLabelIds: number[] | undefined, selected: number[], mode: LabelMatchMode): boolean {
  if (selected.length === 0) return true
  const own = new Set(watchLabelIds ?? [])
  return mode === 'All' ? selected.every(id => own.has(id)) : selected.some(id => own.has(id))
}
