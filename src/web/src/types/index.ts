export type MovementType = 'Automatic' | 'Manual' | 'Quartz' | 'Digital' | 'Unknown'
export type AcquisitionType = 'New' | 'Used' | 'Trade' | 'Other'
export type DispositionType = 'Retired' | 'Returned' | 'Sold' | 'Traded' | 'Other'

export interface WatchDisposition {
  type: DispositionType
  dispositionDate: string
  notes?: string
  soldTo?: string
  salePrice?: number
  receivedWatchId?: number
  receivedWatchName?: string
  tradeDetails?: string
  otherLabel?: string
  returnReason?: string
  returnedTo?: string
  refundAmount?: number
}

export type UpdateWatchDisposition = Omit<WatchDisposition, 'receivedWatchName'>

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
  acquisitionType: AcquisitionType
  acquiredFrom?: string
  acquisitionSourceUrl?: string
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
  sku?: string
  serialNumber?: string
  productionYear?: number
  batteryType?: string
  lastBatteryChangedDate?: string
  linkUrl?: string
  linkText?: string
  storageLocation?: string
  isWishList: boolean
  wishlistPriority?: number
  disposition?: WatchDisposition
  isRetired: boolean
  retiredAt?: string
  createdAt: string
  updatedAt: string
}

export interface WatchRecommendationRequest {
  occasion: string
  outfitDescription: string
  colorPalette?: string
  weather?: string
  preferences?: string
}

export interface WatchRecommendation {
  primary: WatchRecommendationOption
  secondary: WatchRecommendationOption
}

export interface WatchRecommendationOption {
  watchId: number
  brand: string
  model: string
  imageUrl?: string
  reason: string
  stylingTips: string[]
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
  acquisitionType?: AcquisitionType
  acquiredFrom?: string
  acquisitionSourceUrl?: string
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
  sku?: string
  serialNumber?: string
  productionYear?: number
  batteryType?: string
  lastBatteryChangedDate?: string
  linkUrl?: string
  linkText?: string
  storageLocation?: string
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
  storageLocations: string[]
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
  startedAt?: string
  endedAt?: string
  durationMinutes?: number
  watchImageUrl?: string
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

export type StyleMessageRole = 'User' | 'Assistant'

export interface StyleRecommendation {
  id: number
  watchId: number
  occasion?: string | null
  weather?: string | null
  summary: string
  outfit: string
  /** Null until the user says whether the outfit worked out. */
  wasHelpful?: boolean | null
  feedbackNotes?: string | null
  feedbackAt?: string | null
  createdAt: string
}

export interface StyleMessage {
  id: number
  role: StyleMessageRole
  content: string
  recommendation?: StyleRecommendation | null
  createdAt: string
}

export interface StyleSession {
  id: number
  occasion?: string | null
  weather?: string | null
  messages: StyleMessage[]
  createdAt: string
  updatedAt: string
}

export interface StyleChatState {
  configured: boolean
  configurationHint?: string | null
  session: StyleSession
  memory: StyleRecommendation[]
  followUps: string[]
}

export interface SendStyleMessage {
  message?: string
  occasion?: string
  weather?: string
}

export type AdvisorMessageRole = 'User' | 'Assistant'

export interface AdvisorCitation {
  title: string
  url: string
  provider: string
  confidence: 'high' | 'medium' | 'low'
  observedAt: string
}

export interface AdvisorRecommendationCard {
  watchId?: number | null
  provider?: string | null
  providerItemId?: string | null
  title: string
  itemUrl?: string | null
  imageUrl?: string | null
  price?: number | null
  shippingPrice?: number | null
  totalPrice?: number | null
  currency?: string | null
  condition?: string | null
  brand?: string | null
  model?: string | null
  referenceNumber?: string | null
  observedAt?: string | null
  fitScore?: number | null
  reasons: string[]
  feedback?: AdvisorRecommendationFeedback | null
}

export type AdvisorFeedbackKind = 'Helpful' | 'Irrelevant' | 'AlreadyOwned' | 'NotInterested'

export interface AdvisorRecommendationFeedback {
  id: number
  kind: AdvisorFeedbackKind
  notes?: string | null
  updatedAt: string
}

export interface AdvisorWishlistActionResult {
  added: boolean
  watchId: number
  message: string
}

export interface AdvisorToolActivity {
  tool: string
  status: 'completed' | 'completed_with_warnings' | 'unavailable' | 'failed'
  message?: string | null
}

export interface AdvisorMessage {
  id: number
  role: AdvisorMessageRole
  content: string
  citations: AdvisorCitation[]
  recommendationCards: AdvisorRecommendationCard[]
  followUps: string[]
  toolActivity: AdvisorToolActivity[]
  createdAt: string
}

export interface AdvisorSession {
  id: number
  messages: AdvisorMessage[]
  createdAt: string
  updatedAt: string
}

export interface AdvisorChatState {
  configured: boolean
  configurationHint?: string | null
  session: AdvisorSession
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
