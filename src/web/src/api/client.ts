
import axios from 'axios';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL !== undefined
  ? import.meta.env.VITE_API_BASE_URL
  : 'http://localhost:5062';
const client = axios.create({
  baseURL: `${apiBaseUrl}/api`,
  headers: { 'Content-Type': 'application/json' },
});

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  const rt = localStorage.getItem('refreshToken');
  if (!rt) return false;
  try {
    const { data } = await axios.post(`${apiBaseUrl}/api/auth/refresh`, { refreshToken: rt }, {
      headers: { 'Content-Type': 'application/json' },
    });
    localStorage.setItem('token', data.token);
    if (data.refreshToken) localStorage.setItem('refreshToken', data.refreshToken);
    return true;
  } catch {
    return false;
  }
}

client.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (
      axios.isAxiosError(error) &&
      error.response?.status === 401 &&
      !originalRequest._retry &&
      !originalRequest.url?.includes('/auth/refresh') &&
      !originalRequest.url?.includes('/auth/login')
    ) {
      originalRequest._retry = true;

      // Deduplicate concurrent refresh attempts
      if (!refreshPromise) {
        refreshPromise = tryRefresh().finally(() => { refreshPromise = null; });
      }

      const success = await refreshPromise;
      if (success) {
        originalRequest.headers.Authorization = `Bearer ${localStorage.getItem('token')}`;
        return client(originalRequest);
      }
    }

    if (axios.isAxiosError(error) && error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default client;
