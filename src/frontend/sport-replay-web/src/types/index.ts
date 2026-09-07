export type Role = 'Admin' | 'ClubOwner' | 'Operator' | 'Player';

export interface AuthResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}

export interface Club {
  id: string;
  name: string;
  description?: string;
  city?: string;
  province?: string;
  country: string;
  ownerUserId: string;
  isActive: boolean;
  allowFullMatchDownload: boolean;
}

export interface Court {
  id: string;
  clubId: string;
  name: string;
  sportType: number;
  description?: string;
  isActive: boolean;
}

export interface Camera {
  id: string;
  courtId: string;
  courtName: string;
  name: string;
  ipAddress?: string;
  protocol: number;
  status: number;
  lastHeartbeat?: string;
  isActive: boolean;
  isSimulated: boolean;
  resolution?: string;
  fps?: number;
  bitrate?: number;
}

export interface Match {
  id: string;
  courtId: string;
  courtName: string;
  clubId: string;
  clubName: string;
  province?: string;
  startTime: string;
  endTime: string;
  status: number;
  title?: string;
  hasRecording?: boolean;
  recordingCount?: number;
}

export interface ClubPrices {
  clubId: string;
  clipPrice: number;
  fullMatchPrice: number;
  currency: string;
  allowFullMatchDownload: boolean;
}

export interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  role: Role;
  isActive: boolean;
}

export interface Recording {
  id: string;
  matchId: string;
  cameraId: string;
  cameraName: string;
  startedAt: string;
  endedAt?: string;
  status: number;
  hlsPath?: string;
  durationSeconds?: number;
}

export interface VideoClip {
  id: string;
  matchId: string;
  recordingId: string;
  startTime: string;
  endTime: string;
  durationSeconds: number;
  status: number;
  thumbnailUrl?: string;
  streamUrl?: string;
  expiresAt?: string;
}

export interface Payment {
  id: string;
  amount: number;
  currency: string;
  status: number;
  initPoint?: string;
  createdAt: string;
}

export interface ClubDashboard {
  courtCount: number;
  camerasOnline: number;
  camerasOffline: number;
  camerasError: number;
  matchesToday: number;
  videosGenerated: number;
  clipsSold: number;
  revenue: number;
  pendingPayments: number;
  cameraErrors: { cameraId: string; cameraName: string; message: string; occurredAt: string }[];
}

export interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
