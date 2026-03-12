import { useState, useEffect, useCallback } from 'react';

// 本地存储事件类型
type StorageEventCallback<T> = (newValue: T | null, oldValue: T | null) => void;

// 存储配置选项
export interface UseLocalStorageOptions<T> {
  defaultValue?: T;
  serializer?: {
    parse: (value: string) => T;
    stringify: (value: T) => string;
  };
  onError?: (error: Error) => void;
  syncAcrossTabs?: boolean;
}

// 默认序列化器
const defaultSerializer = {
  parse: JSON.parse,
  stringify: JSON.stringify,
};

// 基础本地存储钩子
export const useLocalStorage = <T = any>(
  key: string,
  options: UseLocalStorageOptions<T> = {}
): [T | null, (value: T | null) => void, () => void] => {
  const {
    defaultValue = null,
    serializer = defaultSerializer,
    onError,
    syncAcrossTabs = true,
  } = options;

  // 获取初始值
  const getStoredValue = useCallback((): T | null => {
    try {
      const item = localStorage.getItem(key);
      if (item === null) {
        return defaultValue;
      }
      return serializer.parse(item);
    } catch (error) {
      onError?.(error as Error);
      return defaultValue;
    }
  }, [key, defaultValue, serializer, onError]);

  const [storedValue, setStoredValue] = useState<T | null>(getStoredValue);

  // 设置值
  const setValue = useCallback((value: T | null) => {
    try {
      setStoredValue(value);

      if (value === null) {
        localStorage.removeItem(key);
      } else {
        localStorage.setItem(key, serializer.stringify(value));
      }
    } catch (error) {
      onError?.(error as Error);
    }
  }, [key, serializer, onError]);

  // 删除值
  const removeValue = useCallback(() => {
    try {
      localStorage.removeItem(key);
      setStoredValue(defaultValue);
    } catch (error) {
      onError?.(error as Error);
    }
  }, [key, defaultValue, onError]);

  // 监听storage事件（跨标签页同步）
  useEffect(() => {
    if (!syncAcrossTabs) return;

    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === key && e.storageArea === localStorage) {
        try {
          const newValue = e.newValue ? serializer.parse(e.newValue) : defaultValue;
          setStoredValue(newValue);
        } catch (error) {
          onError?.(error as Error);
        }
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, [key, serializer, defaultValue, onError, syncAcrossTabs]);

  return [storedValue, setValue, removeValue];
};

// 带有回调的本地存储钩子
export const useLocalStorageWithCallback = <T = any>(
  key: string,
  callback: StorageEventCallback<T>,
  options: UseLocalStorageOptions<T> = {}
): [T | null, (value: T | null) => void, () => void] => {
  const [value, setValue, removeValue] = useLocalStorage<T>(key, options);

  const setValueWithCallback = useCallback((newValue: T | null) => {
    const oldValue = value;
    setValue(newValue);
    callback(newValue, oldValue);
  }, [value, setValue, callback]);

  const removeValueWithCallback = useCallback(() => {
    const oldValue = value;
    removeValue();
    callback(null, oldValue);
  }, [value, removeValue, callback]);

  return [value, setValueWithCallback, removeValueWithCallback];
};

// 用户偏好设置钩子
interface UserPreferences {
  theme?: 'light' | 'dark' | 'auto';
  language?: 'zh-CN' | 'en-US';
  fontSize?: 'small' | 'medium' | 'large';
  soundEnabled?: boolean;
  animationsEnabled?: boolean;
  autoSave?: boolean;
  tarotDeckStyle?: 'classic' | 'modern' | 'mystical';
}

export const useUserPreferences = () => {
  const [preferences, setPreferences, removePreferences] = useLocalStorage<UserPreferences>(
    'user-preferences',
    {
      defaultValue: {
        theme: 'dark',
        language: 'zh-CN',
        fontSize: 'medium',
        soundEnabled: true,
        animationsEnabled: true,
        autoSave: true,
        tarotDeckStyle: 'mystical',
      },
    }
  );

  const updatePreference = useCallback(<K extends keyof UserPreferences>(
    key: K,
    value: UserPreferences[K]
  ) => {
    setPreferences({
      ...preferences,
      [key]: value,
    });
  }, [preferences, setPreferences]);

  const resetPreferences = useCallback(() => {
    removePreferences();
  }, [removePreferences]);

  return {
    preferences: preferences || {},
    updatePreference,
    resetPreferences,
    setPreferences,
  };
};

// 游戏数据缓存钩子
interface GameCache {
  lastReadingId?: string;
  favoriteCards?: string[];
  recentSpreads?: string[];
  completedReadings?: number;
  lastVisitedPage?: string;
}

export const useGameCache = () => {
  const [cache, setCache, removeCache] = useLocalStorage<GameCache>(
    'game-cache',
    {
      defaultValue: {
        favoriteCards: [],
        recentSpreads: [],
        completedReadings: 0,
      },
    }
  );

  const updateCache = useCallback(<K extends keyof GameCache>(
    key: K,
    value: GameCache[K]
  ) => {
    setCache({
      ...cache,
      [key]: value,
    });
  }, [cache, setCache]);

  const addFavoriteCard = useCallback((cardId: string) => {
    const currentFavorites = cache?.favoriteCards || [];
    if (!currentFavorites.includes(cardId)) {
      updateCache('favoriteCards', [...currentFavorites, cardId]);
    }
  }, [cache?.favoriteCards, updateCache]);

  const removeFavoriteCard = useCallback((cardId: string) => {
    const currentFavorites = cache?.favoriteCards || [];
    updateCache('favoriteCards', currentFavorites.filter(id => id !== cardId));
  }, [cache?.favoriteCards, updateCache]);

  const addRecentSpread = useCallback((spreadId: string) => {
    const currentSpreads = cache?.recentSpreads || [];
    const updatedSpreads = [spreadId, ...currentSpreads.filter(id => id !== spreadId)].slice(0, 5);
    updateCache('recentSpreads', updatedSpreads);
  }, [cache?.recentSpreads, updateCache]);

  const incrementCompletedReadings = useCallback(() => {
    const current = cache?.completedReadings || 0;
    updateCache('completedReadings', current + 1);
  }, [cache?.completedReadings, updateCache]);

  const clearCache = useCallback(() => {
    removeCache();
  }, [removeCache]);

  return {
    cache: cache || {},
    updateCache,
    addFavoriteCard,
    removeFavoriteCard,
    addRecentSpread,
    incrementCompletedReadings,
    clearCache,
  };
};

// 表单数据缓存钩子
export const useFormCache = <T extends Record<string, any>>(
  formId: string,
  defaultValues: T
) => {
  const [formData, setFormData, removeFormData] = useLocalStorage<T>(
    `form-cache-${formId}`,
    {
      defaultValue: defaultValues,
    }
  );

  const updateField = useCallback(<K extends keyof T>(
    field: K,
    value: T[K]
  ) => {
    setFormData({
      ...formData,
      [field]: value,
    } as T);
  }, [formData, setFormData]);

  const resetForm = useCallback(() => {
    setFormData(defaultValues);
  }, [setFormData, defaultValues]);

  const clearCache = useCallback(() => {
    removeFormData();
  }, [removeFormData]);

  const isDirty = useCallback(() => {
    const current = formData || defaultValues;
    return JSON.stringify(current) !== JSON.stringify(defaultValues);
  }, [formData, defaultValues]);

  return {
    formData: formData || defaultValues,
    updateField,
    setFormData,
    resetForm,
    clearCache,
    isDirty,
  };
};

// 简单的键值对存储钩子
export const useStorage = () => {
  const get = useCallback(<T = any>(key: string, defaultValue?: T): T | null => {
    try {
      const item = localStorage.getItem(key);
      return item ? JSON.parse(item) : defaultValue || null;
    } catch (error) {
      console.warn(`Error reading localStorage key "${key}":`, error);
      return defaultValue || null;
    }
  }, []);

  const set = useCallback((key: string, value: any): void => {
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch (error) {
      console.warn(`Error setting localStorage key "${key}":`, error);
    }
  }, []);

  const remove = useCallback((key: string): void => {
    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.warn(`Error removing localStorage key "${key}":`, error);
    }
  }, []);

  const clear = useCallback((): void => {
    try {
      localStorage.clear();
    } catch (error) {
      console.warn('Error clearing localStorage:', error);
    }
  }, []);

  return { get, set, remove, clear };
};

export default useLocalStorage;