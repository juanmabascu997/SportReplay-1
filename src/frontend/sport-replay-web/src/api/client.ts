import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api'
});

api.interceptors.request.use((config) => {
  const raw = localStorage.getItem('sr.auth');
  if (raw) {
    const parsed = JSON.parse(raw) as { accessToken: string };
    config.headers.Authorization = `Bearer ${parsed.accessToken}`;
  }
  return config;
});

api.interceptors.response.use(
  (r) => r,
  async (error) => {
    if (error.response?.status === 401 && localStorage.getItem('sr.auth')) {
      const parsed = JSON.parse(localStorage.getItem('sr.auth')!);
      try {
        const refreshed = await axios.post('/api/auth/refresh', { refreshToken: parsed.refreshToken });
        localStorage.setItem('sr.auth', JSON.stringify(refreshed.data));
        error.config.headers.Authorization = `Bearer ${refreshed.data.accessToken}`;
        return api.request(error.config);
      } catch {
        localStorage.removeItem('sr.auth');
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
