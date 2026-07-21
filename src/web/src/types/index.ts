export type MovementType = 'Automatic' | 'Manual' | 'Quartz' | 'Digital'

export interface WatchImage {
  id: number
  url: string
}

export interface Watch {
  id: number
  brand: string
  model: string
  movementType: MovementType
  caseSizeMm?: number
  bandType?: string
  bandColor?: string
  purchaseDate?: string
  purchasePrice?: number
  notes?: string
  aiAnalysis?: string
  lastWornDate?: string
  timesWorn: number
  currentResaleValue?: number
  resaleValueUpdatedAt?: string
  imageUrls: WatchImage[]
  crystalType?: string
  caseShape?: string
  crownType?: string
  calendarType?: string
  countryOfOrigin?: string
  waterResistance?: string
  lugWidthMm?: number
  dialColor?: string
  bezelType?: string
  powerReserveHours?: number
  serialNumber?: string
  batteryType?: string
  linkUrl?: string
  linkText?: string
  isWishList: boolean
  isRetired: boolean
  retiredAt?: string
  createdAt: string
  updatedAt: string
}

export interface CreateWatch {
  brand: string
  model: string
  movementType?: MovementType
  caseSizeMm?: number
  bandType?: string
  bandColor?: string
  purchaseDate?: string
  purchasePrice?: number
  notes?: string
  crystalType?: string
  caseShape?: string
  crownType?: string
  calendarType?: string
  countryOfOrigin?: string
  waterResistance?: string
  lugWidthMm?: number
  dialColor?: string
  bezelType?: string
  powerReserveHours?: number
  serialNumber?: string
  batteryType?: string
  linkUrl?: string
  linkText?: string
  isWishList?: boolean
}

export type UpdateWatch = CreateWatch

export interface AuthResponse {
  token: string
  refreshToken?: string
  username: string
  email: string
  role: string
  profileImage?: string
}

export interface LoginCredentials {
  email: string
  password: string
}

export interface RegisterCredentials {
  username: string
  email: string
  password: string
}

export type OidcProvider = 'Entra' | 'PocketId'

export interface OidcProviderPublic {
  provider: OidcProvider
  displayName: string
}

export interface OidcProviderSettings {
  provider: OidcProvider
  enabled: boolean
  displayName: string
  authority: string
  clientId: string
  scopes: string
  hasClientSecret: boolean
  updatedAt: string
}

export interface OidcProviderTestResult {
  success: boolean
  message: string
}

export interface LinkedOidcProvider {
  provider: OidcProvider
  displayName: string
  email: string
  linkedAt: string
  lastUsedAt: string
}

export interface UserDto {
  id: number
  username: string
  email: string
  role: string
  isLockedOut: boolean
  failedLoginAttempts: number
  createdAt: string
}

export interface WearLog {
  id: number
  watchId: number
  watchBrand: string
  watchModel: string
  wornDate: string
}

export type ResaleValueSource = 'Manual' | 'WebSearchEstimate'

export interface ResaleValueEntry {
  id: number
  watchId: number
  value: number
  source: ResaleValueSource
  reasoning?: string
  recordedAt: string
}

export interface CreateResaleValueEntry {
  value: number
  recordedAt?: string
  notes?: string
}

export interface ResaleRefreshSummary {
  due: number
  refreshed: number
  skipped: number
  failed: number
}

export interface AppSettingDto {
  key: string
  value: string
}

export interface OllamaModel {
  name: string
}

export interface ApiKey {
  id: number
  name: string
  prefix: string
  createdAt: string
  lastUsedAt?: string
}
