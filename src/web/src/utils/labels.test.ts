import { describe, expect, it } from 'vitest'
import { matchesLabels } from './labels'

describe('matchesLabels', () => {
  it('matches everything when no labels are selected', () => {
    expect(matchesLabels(undefined, [], 'All')).toBe(true)
  })

  it('needs one selected label for Any and every selected label for All', () => {
    expect(matchesLabels([1, 3], [1, 2], 'Any')).toBe(true)
    expect(matchesLabels([1, 3], [1, 2], 'All')).toBe(false)
    expect(matchesLabels([1, 2, 3], [1, 2], 'All')).toBe(true)
    expect(matchesLabels(undefined, [1], 'Any')).toBe(false)
  })
})
