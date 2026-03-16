import { create } from 'zustand';
import { User } from '../types/api';

interface AuthState {
  user: User | null;
  isLoggedIn: boolean;
  isLoading: boolean;
  error: string | null;
  isInitialized: boolean;

  login: (user: User, token?: string) => void;
  logout: () => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  clearError: () => void;
  updateUser: (user: User) => void;
  initializeAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isLoggedIn: false,
  isLoading: false,
  error: null,
  isInitialized: false,

  login: (user) => {
    localStorage.setItem('user', JSON.stringify(user));
    set({
      user,
      isLoggedIn: true,
      error: null,
      isLoading: false,
      isInitialized: true,
    });
  },

  logout: () => {
    localStorage.removeItem('user');
    set({
      user: null,
      isLoggedIn: false,
      error: null,
      isLoading: false,
      isInitialized: true,
    });
  },

  setLoading: (loading) => set({ isLoading: loading }),

  setError: (error) => set({ error, isLoading: false }),

  clearError: () => set({ error: null }),

  updateUser: (user) => {
    localStorage.setItem('user', JSON.stringify(user));
    set({ user });
  },

  initializeAuth: async () => {
    try {
      set({ isLoading: true });
      const { authService } = await import('../services/authService');
      const user = await authService.getCurrentUser();
      localStorage.setItem('user', JSON.stringify(user));
      set({
        user,
        isLoggedIn: true,
        isLoading: false,
        error: null,
        isInitialized: true,
      });
    } catch {
      localStorage.removeItem('user');
      set({
        user: null,
        isLoggedIn: false,
        isLoading: false,
        error: null,
        isInitialized: true,
      });
    }
  },
}));
