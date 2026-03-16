import axios from 'axios';
import { config, debugLog } from '../config/env';

export const api = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 10000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use(
  (requestConfig) => {
    debugLog('API request:', requestConfig.method?.toUpperCase(), requestConfig.url);
    return requestConfig;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => {
    if (response.data && typeof response.data === 'object' && 'data' in response.data) {
      return response.data.data;
    }
    return response.data;
  },
  async (error) => {
    if (error.response?.status === 401) {
      try {
        const { useAuthStore } = await import('../stores/authStore');
        useAuthStore.getState().logout();
      } catch {
        // best-effort state cleanup
      }

      if (window.location.pathname !== '/auth/login') {
        window.location.href = '/auth/login';
      }
    }

    const isDev = process.env.NODE_ENV === 'development';
    console.error('API response error:', {
      status: error.response?.status,
      message: error.message,
      detail: isDev ? error.response?.data : undefined,
    });

    const errorMessage =
      error.response?.data?.detail ||
      error.response?.data?.message ||
      error.message ||
      'Request failed';

    return Promise.reject(new Error(errorMessage));
  }
);

export default api;
