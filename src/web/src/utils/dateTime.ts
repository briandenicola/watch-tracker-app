const DEFAULT_TIME_ZONE = 'America/Chicago'

let applicationTimeZone = DEFAULT_TIME_ZONE

export function setApplicationTimeZone(timeZone: string) {
  try {
    new Intl.DateTimeFormat('en-US', { timeZone }).format()
    applicationTimeZone = timeZone
  } catch {
    applicationTimeZone = DEFAULT_TIME_ZONE
  }
}

export function getApplicationTimeZone() {
  return applicationTimeZone
}

export function formatInstant(
  value: string | Date,
  options: Intl.DateTimeFormatOptions,
  locale?: string | string[],
) {
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return new Intl.DateTimeFormat(locale, {
    ...options,
    timeZone: applicationTimeZone,
  }).format(date)
}

export function formatCalendarDate(
  dateKey: string,
  options: Intl.DateTimeFormatOptions,
  locale?: string | string[],
) {
  const date = new Date(`${dateKey.slice(0, 10)}T12:00:00Z`)
  if (Number.isNaN(date.getTime())) return ''
  return new Intl.DateTimeFormat(locale, {
    ...options,
    timeZone: 'UTC',
  }).format(date)
}

export function instantDateKey(value: string | Date) {
  const parts = zonedParts(value)
  return `${parts.year}-${pad(parts.month)}-${pad(parts.day)}`
}

export function currentDateKey() {
  return instantDateKey(new Date())
}

export function dateInputValue(value?: string | null) {
  if (!value) return ''
  return /^\d{4}-\d{2}-\d{2}/.test(value) ? value.slice(0, 10) : ''
}

export function instantTimeInput(value?: string) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const parts = zonedParts(date)
  return `${pad(parts.hour)}:${pad(parts.minute)}`
}

export function relativeTime(value: string | Date, now = new Date()): string {
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return 'Recently'

  const seconds = Math.round((date.getTime() - now.getTime()) / 1_000)
  const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
    ['year', 31_536_000],
    ['month', 2_592_000],
    ['day', 86_400],
    ['hour', 3_600],
    ['minute', 60],
    ['second', 1],
  ]
  const [unit, size] = units.find(([, unitSize]) => Math.abs(seconds) >= unitSize) ?? units.at(-1)!
  return new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' }).format(
    Math.round(seconds / size),
    unit,
  )
}

export function zonedDateTimeToUtc(
  dateKey: string,
  time: string,
  preferredInstant?: string,
) {
  const [year, month, day] = dateKey.split('-').map(Number)
  const [hour, minute] = time.split(':').map(Number)
  if (![year, month, day, hour, minute].every(Number.isFinite)) {
    throw new Error('Invalid date or time.')
  }

  const target = Date.UTC(year, month - 1, day, hour, minute)
  let candidate = target
  for (let attempt = 0; attempt < 3; attempt++) {
    const actual = zonedParts(new Date(candidate))
    const represented = Date.UTC(
      actual.year,
      actual.month - 1,
      actual.day,
      actual.hour,
      actual.minute,
    )
    candidate += target - represented
  }

  const matchingCandidates = [-2, -1, 0, 1, 2]
    .map(offset => new Date(candidate + offset * 60 * 60 * 1000))
    .filter(result => matchesWallClock(result, year, month, day, hour, minute))
  if (matchingCandidates.length === 0) {
    throw new Error('This time does not exist in the configured timezone.')
  }

  const preferredTime = preferredInstant ? new Date(preferredInstant).getTime() : Number.NaN
  const result = Number.isNaN(preferredTime)
    ? matchingCandidates[0]
    : matchingCandidates.reduce((closest, value) =>
        Math.abs(value.getTime() - preferredTime) < Math.abs(closest.getTime() - preferredTime)
          ? value
          : closest,
      )
  return result.toISOString()
}

function matchesWallClock(
  date: Date,
  year: number,
  month: number,
  day: number,
  hour: number,
  minute: number,
) {
  const actual = zonedParts(date)
  return actual.year === year
    && actual.month === month
    && actual.day === day
    && actual.hour === hour
    && actual.minute === minute
}

function zonedParts(value: string | Date) {
  const date = value instanceof Date ? value : new Date(value)
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: applicationTimeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hourCycle: 'h23',
  }).formatToParts(date)
  const values = Object.fromEntries(parts.map(part => [part.type, part.value]))
  return {
    year: Number(values.year),
    month: Number(values.month),
    day: Number(values.day),
    hour: Number(values.hour),
    minute: Number(values.minute),
    second: Number(values.second),
  }
}

function pad(value: number) {
  return String(value).padStart(2, '0')
}
