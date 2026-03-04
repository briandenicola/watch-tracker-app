export type MovementType = 'Automatic' | 'Manual' | 'Quartz' | 'Digital';

export interface WatchImage {
  id: number;
  url: string;
}

export interface Watch {
  id: number;
  brand: string;
  model: string;
  movementType: MovementType;
  caseSizeMm?: number;
  bandType?: string;
  purchaseDate?: string;
  purchasePrice?: number;
  notes?: string;
  aiAnalysis?: string;
  lastWornDate?: string;
  timesWorn: number;
  imageUrls: WatchImage[];
  crystalType?: string;
  caseShape?: string;
  crownType?: string;
  calendarType?: string;
  countryOfOrigin?: string;
  waterResistance?: string;
  lugWidthMm?: number;
  dialColor?: string;
  bezelType?: string;
  powerReserveHours?: number;
  serialNumber?: string;
  linkUrl?: string;
  linkText?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWatch {
  brand: string;
  model: string;
  movementType: MovementType;
  caseSizeMm?: number;
  bandType?: string;
  purchaseDate?: string;
  purchasePrice?: number;
  notes?: string;
  crystalType?: string;
  caseShape?: string;
  crownType?: string;
  calendarType?: string;
  countryOfOrigin?: string;
  waterResistance?: string;
  lugWidthMm?: number;
  dialColor?: string;
  bezelType?: string;
  powerReserveHours?: number;
  serialNumber?: string;
  linkUrl?: string;
  linkText?: string;
}

export type UpdateWatch = CreateWatch;

export interface AuthResponse {
  token: string;
  username: string;
  email: string;
  role: string;
  profileImage?: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  username: string;
  email: string;
  password: string;
}

export interface UserDto {
  id: number;
  username: string;
  email: string;
  role: string;
  isLockedOut: boolean;
  failedLoginAttempts: number;
  createdAt: string;
}

export interface AppSettingDto {
  key: string;
  value: string;
}
