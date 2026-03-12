import { api } from './api';
import { LoginData, RegisterData, AuthResponse, User } from '../types/api';

export const authService = {
  login: async (data: LoginData): Promise<AuthResponse> => {
    const params = new URLSearchParams();
    params.append('username', data.username);
    params.append('password', data.password);

    return api.post('/login', params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
    });
  },

  register: async (data: RegisterData): Promise<User> => {
    return api.post('/register', data);
  },

  getCurrentUser: async (): Promise<User> => {
    return api.get('/users/me');
  },

  refreshToken: async (): Promise<AuthResponse> => {
    return api.post('/refresh');
  },

  logout: async (): Promise<{ message: string }> => {
    return api.post('/logout');
  },

  updateProfile: async (userData: Partial<User>): Promise<User> => {
    return api.put('/users/me', userData);
  },
};
