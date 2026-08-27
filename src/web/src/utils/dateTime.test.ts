import { beforeEach, describe, expect, it } from 'vitest'
import {
  instantDateKey,
  instantTimeInput,
  relativeTime,
  setApplicationTimeZone,
  zonedDateTimeToUtc,
} from './dateTime'

describe('application timezone date handling', () => {
  beforeEach(() => setApplicationTimeZone('America/Chicago'))

  it('groups a Dallas evening UTC instant on the prior calendar day', () => {
    const instant = '2025-08-19T01:32:00.000Z'

    expect(instantDateKey(instant)).toBe('2025-08-18')
    expect(instantTimeInput(instant)).toBe('20:32')
  })

  it('round-trips Dallas wall clock time to UTC during daylight saving time', () => {
    expect(zonedDateTimeToUtc('2025-08-18', '20:32'))
      .toBe('2025-08-19T01:32:00.000Z')
  })

  it('uses the standard-time offset outside daylight saving time', () => {
    expect(zonedDateTimeToUtc('2025-01-18', '20:32'))
      .toBe('2025-01-19T02:32:00.000Z')
  })

  it('rejects wall clock times skipped by the daylight saving transition', () => {
    expect(() => zonedDateTimeToUtc('2025-03-09', '02:30'))
      .toThrow('This time does not exist')
  })

  it('preserves the later occurrence of an ambiguous fall-back time', () => {
    expect(zonedDateTimeToUtc(
      '2025-11-02',
      '01:30',
      '2025-11-02T07:30:00.000Z',
    )).toBe('2025-11-02T07:30:00.000Z')
  })

  it('formats notification timestamps relative to a supplied instant', () => {
    expect(relativeTime('2025-08-19T01:31:00.000Z', new Date('2025-08-19T01:32:00.000Z')))
      .toBe('1 minute ago')
  })
})
