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
  lugToLugMm?: number
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
  marketplaceCurrency?: string
  marketplaceObservedAt?: string
  storageLocation?: string
  isWishList: boolean
  wishlistPriority?: number
  priceAlertEnabled: boolean
  priceAlertTarget?: number
  priceCheckedAt?: string
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
  lugToLugMm?: number
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

export interface WishlistExtractionResult {
  brand?: string
  model?: string
  purchasePrice?: number
  linkUrl: string
  linkText: string
  imageUrl?: string
  warnings: string[]
}

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

/** Optional overrides for a recorded wear. Omitted entirely, the server uses "now". */
export interface RecordWearOptions {
  wornDate: string
  startedAt?: string
  endedAt?: string
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

export type PriceObservationKind = 'Unknown' | 'New' | 'Preowned'
export type PriceMatchConfidence = 'Low' | 'Medium' | 'High'
export type PriceAlertTrigger = 'BelowTarget' | 'NewBest'
export type PriceScanStatus = 'Found' | 'NotConfigured' | 'Blocked' | 'ProviderError' | 'NoMatch'

export interface PriceMonitoring {
  priceAlertEnabled: boolean
  priceAlertTarget?: number
  priceCheckedAt?: string
}

export interface UpdatePriceMonitoring {
  priceAlertEnabled: boolean
  priceAlertTarget?: number | null
}

export interface PriceObservation {
  id: number
  source: string
  providerListingId?: string
  listingUrl: string
  listingTitle: string
  price: number
  currency: string
  condition?: string
  kind: PriceObservationKind
  matchConfidence: PriceMatchConfidence
  observedAt: string
}

export interface PriceScanSourceResult {
  source: string
  status: PriceScanStatus
  error?: string
  listings: PriceObservation[]
}

export interface PriceScanResult {
  watchId: number
  checkedAt: string
  sources: PriceScanSourceResult[]
  observationsAdded: number
  alertsCreated: number
}

export interface PriceAlert {
  id: number
  watchId: number
  watchBrand: string
  watchModel: string
  trigger: PriceAlertTrigger
  isRead: boolean
  readAt?: string
  createdAt: string
  observation: PriceObservation
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
  durationMs: number
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

/** A value the AI analysis proposes for a field the watch has no value for. */
export interface WatchFieldSuggestion {
  field: string
  label: string
  kind: 'text' | 'number' | 'integer'
  value: string
  confidence: 'high' | 'medium' | 'low'
  reason?: string | null
}

/** A page the analysis read — the watch's reference link, or the store it came from. */
export interface AnalysisSource {
  label: string
  url: string
}

export interface WatchAnalysisResult {
  summary: string
  suggestions: WatchFieldSuggestion[]
  sources: AnalysisSource[]
}

export interface ApplyAnalysisResult {
  applied: string[]
  rejected: string[]
  watch: Watch
}

export interface WishlistShare {
  token: string
  url?: string | null
  path: string
  /** Whether visitors see each item's target price. */
  includePrices: boolean
  createdAt: string
  lastViewedAt?: string | null
  viewCount: number
}

export interface SharedWishlistItem {
  brand: string
  model: string
  sku?: string | null
  movementType: MovementType
  caseSizeMm?: number | null
  caseShape?: string | null
  dialColor?: string | null
  bandType?: string | null
  bandColor?: string | null
  waterResistance?: string | null
  countryOfOrigin?: string | null
  linkUrl?: string | null
  linkText?: string | null
  /** Present only when the owner chose to publish prices. */
  targetPrice?: number | null
  imageUrls: WatchImage[]
}

export interface SharedWishlist {
  ownerName: string
  includesPrices: boolean
  items: SharedWishlistItem[]
  sharedAt: string
}

export interface WatchShare {
  token: string
  /** The full link, when an admin has set a public address for shares. */
  url?: string | null
  /** Path on this app's own origin, e.g. "/s/<token>". Used when no public address is set. */
  path: string
  createdAt: string
  lastViewedAt?: string | null
  viewCount: number
}

/**
 * The redacted watch a share link exposes. Anything absent here is absent by
 * design — price, provenance, serial, notes, resale, storage and wear history
 * never leave the account.
 */
export interface SharedWatch {
  brand: string
  model: string
  sku?: string | null
  movementType: MovementType
  caseSizeMm?: number | null
  caseShape?: string | null
  crystalType?: string | null
  bezelType?: string | null
  crownType?: string | null
  calendarType?: string | null
  dialColor?: string | null
  bandType?: string | null
  bandColor?: string | null
  lugWidthMm?: number | null
  lugToLugMm?: number | null
  waterResistance?: string | null
  powerReserveHours?: number | null
  batteryType?: string | null
  productionYear?: number | null
  countryOfOrigin?: string | null
  linkUrl?: string | null
  linkText?: string | null
  isWishList: boolean
  imageUrls: WatchImage[]
  sharedAt: string
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

export type CollectionInsightConfidence = 'Low' | 'Medium' | 'High'

export interface CollectionCoverageValue {
  value: string
  count: number
  watchIds: number[]
}

export interface CollectionCoverage {
  dimension: string
  values: CollectionCoverageValue[]
}

export interface CollectionInsight {
  summary: string
  reason: string
  confidence: CollectionInsightConfidence
  watchIds: number[]
  evidenceFields: string[]
}

export interface WishlistOverlap {
  wishlistWatchId: number
  collectionWatchIds: number[]
  reason: string
}

export interface CollectionSetStats {
  label: string
  watchCount: number
  dataCompletenessPercent: number
  coverage: CollectionCoverage[]
  redundancies: CollectionInsight[]
  gaps: CollectionInsight[]
}

export interface ReviewWatch {
  id: number
  brand: string
  model: string
  movementType: string
  caseSizeMm?: number | null
  dialColor?: string | null
  bandType?: string | null
  price?: number | null
  wishlistPriority?: number | null
  timesWorn?: number | null
  lastWornDate?: string | null
}

export interface WishlistFit {
  watchId: number
  totalScore: number
  collectionFitScore: number
  reasons: string[]
}

export interface CollectionReviewFacts {
  collection: CollectionSetStats
  wishlist: CollectionSetStats
  combined: CollectionSetStats
  dataQuality: CollectionInsight[]
  wishlistOverlaps: WishlistOverlap[]
  wishlistFit: WishlistFit[]
  collectionWatches: ReviewWatch[]
  wishlistWatches: ReviewWatch[]
  underusedWatchIds: number[]
}

export interface CollectionReviewFinding {
  summary: string
  detail: string
  watchIds: number[]
}

export interface MarketplaceProviderStatus {
  provider: string
  status: 'Success' | 'NotConfigured' | 'ProviderError'
  error?: string | null
}

export interface CollectionReviewCandidates {
  candidates: AdvisorRecommendationCard[]
  marketplaceStatus: MarketplaceProviderStatus[]
  generatedAt?: string | null
  droppedStaleListings: boolean
}

export interface CollectionReview {
  summary?: string | null
  strengths: CollectionReviewFinding[]
  weaknesses: CollectionReviewFinding[]
  recommendations: CollectionReviewFinding[]
  facts: CollectionReviewFacts
  generatedAt: string
  isStale: boolean
  candidates: CollectionReviewCandidates
}

export interface CollectionReviewState {
  configured: boolean
  configurationHint?: string | null
  review?: CollectionReview | null
}
