import axios from 'axios';
import { config, debugLog } from '../config/env';

const normalizeApiErrorMessage = (payload: unknown): string | null => {
  if (!payload) {
    return null;
  }

  if (typeof payload === 'string') {
    const trimmed = payload.trim();
    return trimmed || null;
  }

  if (Array.isArray(payload)) {
    const messages = payload
      .map((item) => normalizeApiErrorMessage(item))
      .filter((message): message is string => Boolean(message));
    return messages.length > 0 ? messages.join(' | ') : null;
  }

  if (typeof payload === 'object') {
    const record = payload as Record<string, unknown>;

    if (typeof record.msg === 'string' && Array.isArray(record.loc)) {
      const path = record.loc
        .filter((segment) => typeof segment === 'string' || typeof segment === 'number')
        .map((segment) => String(segment))
        .join('.');
      return path ? `${path}: ${record.msg}` : record.msg;
    }

    if (record.detail !== undefined) {
      return normalizeApiErrorMessage(record.detail);
    }

    if (typeof record.message === 'string') {
      const trimmed = record.message.trim();
      return trimmed || null;
    }

    try {
      return JSON.stringify(record);
    } catch {
      return null;
    }
  }

  return String(payload);
};

export const api = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 10000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

const readCookie = (name: string): string | null => {
  if (typeof document === 'undefined') {
    return null;
  }
  const prefix = `${name}=`;
  const entry = document.cookie
    .split(';')
    .map((item) => item.trim())
    .find((item) => item.startsWith(prefix));
  if (!entry) {
    return null;
  }
  return decodeURIComponent(entry.slice(prefix.length));
};

api.interceptors.request.use(
  (requestConfig) => {
    const method = String(requestConfig.method || 'GET').toUpperCase();
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)) {
      const csrfToken = readCookie('csrf_token');
      if (csrfToken) {
        requestConfig.headers = requestConfig.headers || {};
        requestConfig.headers['X-CSRF-Token'] = csrfToken;
      }
    }

    debugLog('API request:', requestConfig.method?.toUpperCase(), requestConfig.url);
    return requestConfig;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response.data,
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
      normalizeApiErrorMessage(error.response?.data) ||
      error.message ||
      'Request failed';

    return Promise.reject(new Error(errorMessage));
  }
);

export default api;
