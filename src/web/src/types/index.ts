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
  imageUrls: WatchImage[];
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
}

export type UpdateWatch = CreateWatch;

export interface AuthResponse {
  token: string;
  username: string;
  email: string;
  role: string;
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
