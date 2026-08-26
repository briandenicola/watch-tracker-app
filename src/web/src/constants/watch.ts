import type { AcquisitionType, MovementType, UpdateWatch } from '@/types'

// Option lists shared by the watch form and the detail page, so the two cannot
// drift apart on what counts as a known band or crystal type.

export const bandTypes = [
  'Bracelet', 'Leather', 'Rubber', 'NATO', 'Canvas', 'Mesh', 'Silicone', 'Ceramic', 'Titanium',
]

export const crystalTypes = ['Sapphire', 'Mineral', 'Hardlex', 'Acrylic', 'Hesalite']

export const movementTypes: MovementType[] = ['Automatic', 'Manual', 'Quartz', 'Digital', 'Unknown']
export const acquisitionTypes: AcquisitionType[] = ['New', 'Used', 'Trade', 'Other']

/**
 * Fields the detail page can edit in place. Notes renders as a markdown block
 * rather than a row, and isWishList belongs to the purchase action, so neither
 * is editable inline.
 */
export type InlineField = Exclude<keyof UpdateWatch, 'notes' | 'isWishList'>

export interface FieldMeta {
  input: 'text' | 'number' | 'date' | 'select'
  /** Mirrors the server's validation, so bad input is caught before the round trip. */
  min?: number
  max?: number
  step?: number
  maxlength?: number
  required?: boolean
  /** Suggested values. Free text is still accepted unless `strict`. */
  options?: readonly string[]
  /** Only the listed options are valid. */
  strict?: boolean
}

/**
 * How each inline-editable field is entered and what the server will accept.
 * Keyed by InlineField, so adding a field to UpdateWatch fails the build until
 * it is either described here or excluded from InlineField above.
 *
 * Constraints mirror the data annotations on CreateWatchDto / UpdateWatchDto.
 */
export const fieldMeta: Record<InlineField, FieldMeta> = {
  brand: { input: 'text', maxlength: 200, required: true },
  model: { input: 'text', maxlength: 200, required: true },
  movementType: { input: 'select', options: movementTypes, strict: true, required: true },

  sku: { input: 'text', maxlength: 100 },
  serialNumber: { input: 'text', maxlength: 200 },
  productionYear: { input: 'number', min: 1800, max: 2200, step: 1 },
  countryOfOrigin: { input: 'text', maxlength: 100 },

  caseSizeMm: { input: 'number', min: 1, max: 200, step: 0.1 },
  lugWidthMm: { input: 'number', min: 1, max: 100, step: 0.5 },
  lugToLugMm: { input: 'number', min: 1, max: 200, step: 0.1 },
  caseShape: { input: 'text', maxlength: 100 },
  crystalType: { input: 'select', options: crystalTypes },
  bezelType: { input: 'text', maxlength: 100 },
  crownType: { input: 'text', maxlength: 100 },
  dialColor: { input: 'text', maxlength: 100 },
  waterResistance: { input: 'text', maxlength: 100 },
  bandType: { input: 'select', options: bandTypes },
  bandColor: { input: 'text', maxlength: 100 },

  powerReserveHours: { input: 'number', min: 0, max: 10000, step: 1 },
  calendarType: { input: 'text', maxlength: 100 },
  batteryType: { input: 'text', maxlength: 100 },
  lastBatteryChangedDate: { input: 'date' },

  purchasePrice: { input: 'number', min: 0, max: 10_000_000, step: 0.01 },
  purchaseDate: { input: 'date' },
  acquisitionType: { input: 'select', options: acquisitionTypes, strict: true, required: true },
  acquiredFrom: { input: 'text', maxlength: 200 },
  acquisitionSourceUrl: { input: 'text', maxlength: 2000 },
  linkUrl: { input: 'text', maxlength: 2000 },
  linkText: { input: 'text', maxlength: 200 },

  // Options come from the signed-in user's profile, so they are supplied at runtime.
  storageLocation: { input: 'select', strict: true },
}
