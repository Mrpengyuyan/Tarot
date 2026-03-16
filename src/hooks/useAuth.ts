import { useCallback, useEffect } from 'react';
import { useAuthStore } from '../stores/authStore';
import { authService } from '../services/authService';
import { useNotification } from '../components/UI/Notification';
import { RegisterData as APIRegisterData } from '../types/api';

export interface LoginCredentials {
  username?: string;
  email?: string;
  password: string;
}

export interface RegisterData extends APIRegisterData {
  confirmPassword?: string;
}

export interface UseAuthReturn {
  user: any;
  isLoggedIn: boolean;
  isLoading: boolean;
  error: string | null;
  login: (credentials: LoginCredentials) => Promise<boolean>;
  register: (data: RegisterData) => Promise<boolean>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<boolean>;
  updateProfile: (userData: any) => Promise<boolean>;
  clearError: () => void;
  checkAuth: () => Promise<boolean>;
}

export const useAuth = (): UseAuthReturn => {
  const {
    user,
    isLoggedIn,
    isLoading,
    error,
    login: loginStore,
    logout: logoutStore,
    setLoading,
    setError,
    clearError: clearErrorStore,
    updateUser,
  } = useAuthStore();

  const { showError, showSuccess } = useNotification();

  const login = useCallback(async (credentials: LoginCredentials): Promise<boolean> => {
    try {
      setLoading(true);
      setError(null);

      const loginData = {
        username: credentials.username || credentials.email || '',
        password: credentials.password,
      };

      const response = await authService.login(loginData);
      const token = response?.access_token || response?.data?.token;
      if (!token) {
        throw new Error('Login response missing access token');
      }

      const userInfo = response?.data?.user || (await authService.getCurrentUser());
      loginStore(userInfo, token);
      showSuccess('登录成功');
      return true;
    } catch (err: any) {
      const msg = err?.message || '登录失败，请稍后重试';
      setError(msg);
      showError(msg);
      return false;
    } finally {
      setLoading(false);
    }
  }, [loginStore, setLoading, setError, showError, showSuccess]);

  const register = useCallback(async (data: RegisterData): Promise<boolean> => {
    try {
      setLoading(true);
      setError(null);

      if (data.password !== data.confirmPassword) {
        const msg = '两次输入的密码不一致';
        setError(msg);
        showError(msg);
        return false;
      }

      if (data.password.length < 6) {
        const msg = '密码长度至少需要 6 位';
        setError(msg);
        showError(msg);
        return false;
      }

      const apiData: APIRegisterData = {
        username: data.username,
        email: data.email,
        password: data.password,
        nickname: data.nickname,
      };

      await authService.register(apiData);

      const loginOk = await login({ username: data.username, password: data.password });
      if (loginOk) {
        showSuccess('注册成功');
        return true;
      }

      return false;
    } catch (err: any) {
      const msg = err?.message || '注册失败，请稍后重试';
      setError(msg);
      showError(msg);
      return false;
    } finally {
      setLoading(false);
    }
  }, [login, setLoading, setError, showError, showSuccess]);

  const logout = useCallback(async (): Promise<void> => {
    try {
      setLoading(true);
      await authService.logout();
    } catch {
      // best effort
    } finally {
      logoutStore();
      setLoading(false);
    }
  }, [logoutStore, setLoading]);

  const refreshToken = useCallback(async (): Promise<boolean> => {
    try {
      const response = await authService.refreshToken();
      const token = response?.access_token || response?.data?.token;
      if (!token) {
        logoutStore();
        return false;
      }

      const userInfo = response?.data?.user || (await authService.getCurrentUser());
      loginStore(userInfo, token);
      return true;
    } catch {
      logoutStore();
      return false;
    }
  }, [loginStore, logoutStore]);

  const updateProfile = useCallback(async (userData: any): Promise<boolean> => {
    try {
      setLoading(true);
      setError(null);
      const userInfo = await authService.updateProfile(userData);
      updateUser(userInfo);
      showSuccess('个人信息更新成功');
      return true;
    } catch (err: any) {
      const msg = err?.message || '更新失败，请稍后重试';
      setError(msg);
      showError(msg);
      return false;
    } finally {
      setLoading(false);
    }
  }, [setLoading, setError, showError, showSuccess, updateUser]);

  const checkAuth = useCallback(async (): Promise<boolean> => {
    try {
      const currentUser = await authService.getCurrentUser();
      loginStore(currentUser);
      return true;
    } catch {
      logoutStore();
      return false;
    }
  }, [loginStore, logoutStore]);

  const clearError = useCallback(() => {
    clearErrorStore();
  }, [clearErrorStore]);

  useEffect(() => {
    void checkAuth();
  }, [checkAuth]);

  return {
    user,
    isLoggedIn,
    isLoading,
    error,
    login,
    register,
    logout,
    refreshToken,
    updateProfile,
    clearError,
    checkAuth,
  };
};

export const useRequireAuth = () => {
  const { isLoggedIn, checkAuth } = useAuth();

  useEffect(() => {
    if (!isLoggedIn) {
      void checkAuth();
    }
  }, [isLoggedIn, checkAuth]);

  return { isAuthenticated: isLoggedIn };
};

export const useCurrentUser = () => {
  const { user } = useAuth();
  return user;
};

export const useUserPermissions = () => {
  const { user } = useAuth();
  const isAdmin = Boolean(user?.is_superuser);

  return {
    isAdmin,
    canAccessAdmin: isAdmin,
    canEditProfile: Boolean(user),
    canViewHistory: Boolean(user),
  };
};

export default useAuth;
