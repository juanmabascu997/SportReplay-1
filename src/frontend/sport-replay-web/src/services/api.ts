import api from '../api/client';
import type { Camera, Club, ClubDashboard, ClubPrices, Court, Match, Paged, Payment, Recording, UserProfile, VideoClip } from '../types';

export const clubsApi = {
  list: () => api.get<Club[]>('/clubs').then((r) => r.data),
  get: (id: string) => api.get<Club>(`/clubs/${id}`).then((r) => r.data),
  create: (payload: Partial<Club> & { name: string }) => api.post<Club>('/clubs', payload).then((r) => r.data),
  courts: (clubId: string) => api.get<Court[]>(`/clubs/${clubId}/courts`).then((r) => r.data),
  createCourt: (clubId: string, payload: { name: string; sportType: number; description?: string }) =>
    api.post<Court>(`/clubs/${clubId}/courts`, payload).then((r) => r.data),
  dashboard: (clubId: string) => api.get<ClubDashboard>(`/clubs/${clubId}/dashboard`).then((r) => r.data),
  prices: (clubId: string) => api.get<ClubPrices>(`/clubs/${clubId}/prices`).then((r) => r.data),
  settings: (clubId: string) => api.get(`/clubs/${clubId}/settings`).then((r) => r.data),
  updateSettings: (clubId: string, payload: unknown) => api.put(`/clubs/${clubId}/settings`, payload).then((r) => r.data)
};

export const camerasApi = {
  list: () => api.get<Camera[]>('/cameras').then((r) => r.data),
  byCourt: (courtId: string) => api.get<Camera[]>(`/courts/${courtId}/cameras`).then((r) => r.data),
  create: (courtId: string, payload: unknown) => api.post<Camera>(`/courts/${courtId}/cameras`, payload).then((r) => r.data),
  test: (id: string) => api.post(`/cameras/${id}/test-connection`).then((r) => r.data),
  start: (id: string) => api.post(`/cameras/${id}/start`),
  stop: (id: string) => api.post(`/cameras/${id}/stop`)
};

export const matchesApi = {
  search: (params: Record<string, string | undefined>) => api.get<Paged<Match>>('/matches', { params }).then((r) => r.data),
  get: (id: string) => api.get<Match>(`/matches/${id}`).then((r) => r.data),
  create: (payload: { courtId: string; startTime: string; endTime: string; title?: string }) =>
    api.post<Match>('/matches', payload).then((r) => r.data),
  recordings: (id: string) => api.get<Recording[]>(`/matches/${id}/recordings`).then((r) => r.data),
  clips: (id: string) => api.get<VideoClip[]>(`/matches/${id}/clips`).then((r) => r.data)
};

export const videosApi = {
  createClip: (payload: { matchId: string; recordingId: string; startTime: string; endTime: string }) =>
    api.post<{ clipId: string; status: number }>('/video-clips', payload).then((r) => r.data),
  getClip: (id: string) => api.get<VideoClip>(`/video-clips/${id}`).then((r) => r.data),
  stream: (id: string) => api.get<{ id: string; url: string; kind: string }>(`/videos/${id}/stream`).then((r) => r.data),
  recordingStream: (id: string) =>
    api.get<{ id: string; url: string; kind: string }>(`/recordings/${id}/stream`).then((r) => r.data),
  download: (id: string) => api.get<{ url: string }>(`/videos/${id}/download`).then((r) => r.data),
  requestWhatsApp: (payload: { matchId: string; videoClipId?: string; phoneNumber: string }) =>
    api.post<{ id: string }>('/video-requests', payload).then((r) => r.data)
};

export const paymentsApi = {
  create: (payload: { videoClipId?: string; matchId?: string; videoRequestId?: string; productType: number }) =>
    api.post<Payment>('/payments/create', payload).then((r) => r.data),
  get: (id: string) => api.get<Payment>(`/payments/${id}`).then((r) => r.data)
};

export const profileApi = {
  get: () => api.get<UserProfile>('/profile').then((r) => r.data)
};
