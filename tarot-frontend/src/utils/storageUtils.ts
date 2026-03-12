// 存储工具类

// 存储类型枚举
export enum StorageType {
  LOCAL = 'localStorage',
  SESSION = 'sessionStorage',
}

// 存储配置接口
export interface StorageConfig {
  prefix?: string;
  encryption?: boolean;
  expiration?: number; // 过期时间（毫秒）
  version?: string;
}

// 存储项接口
interface StorageItem<T = any> {
  value: T;
  timestamp: number;
  expiration?: number;
  version?: string;
}

// 默认配置
const DEFAULT_CONFIG: Required<StorageConfig> = {
  prefix: 'tarot_',
  encryption: false,
  expiration: 0, // 0 表示永不过期
  version: '1.0.0',
};

// 简单加密/解密（仅用于基本保护，不是安全加密）
const simpleEncrypt = (text: string): string => {
  return btoa(encodeURIComponent(text));
};

const simpleDecrypt = (encrypted: string): string => {
  try {
    return decodeURIComponent(atob(encrypted));
  } catch (error) {
    console.error('Decryption failed:', error);
    return encrypted;
  }
};

// 存储管理器类
class StorageManager {
  private storage: Storage;
  private config: Required<StorageConfig>;

  constructor(
    storageType: StorageType = StorageType.LOCAL,
    config: StorageConfig = {}
  ) {
    this.storage = window[storageType];
    this.config = { ...DEFAULT_CONFIG, ...config };

    // 检查存储可用性
    if (!this.isStorageAvailable()) {
      console.warn(`${storageType} is not available`);
    }
  }

  // 检查存储是否可用
  private isStorageAvailable(): boolean {
    try {
      const testKey = '__storage_test__';
      this.storage.setItem(testKey, 'test');
      this.storage.removeItem(testKey);
      return true;
    } catch (error) {
      return false;
    }
  }

  // 获取完整的键名
  private getFullKey(key: string): string {
    return `${this.config.prefix}${key}`;
  }

  // 创建存储项
  private createStorageItem<T>(value: T): StorageItem<T> {
    const now = Date.now();
    return {
      value,
      timestamp: now,
      expiration: this.config.expiration > 0 ? now + this.config.expiration : undefined,
      version: this.config.version,
    };
  }

  // 检查项是否过期
  private isExpired(item: StorageItem): boolean {
    if (!item.expiration) return false;
    return Date.now() > item.expiration;
  }

  // 检查版本是否匹配
  private isVersionValid(item: StorageItem): boolean {
    if (!item.version) return true;
    return item.version === this.config.version;
  }

  // 序列化数据
  private serialize(data: any): string {
    try {
      const jsonString = JSON.stringify(data);
      return this.config.encryption ? simpleEncrypt(jsonString) : jsonString;
    } catch (error) {
      console.error('Serialization failed:', error);
      throw error;
    }
  }

  // 反序列化数据
  private deserialize<T>(data: string): T {
    try {
      const jsonString = this.config.encryption ? simpleDecrypt(data) : data;
      return JSON.parse(jsonString);
    } catch (error) {
      console.error('Deserialization failed:', error);
      throw error;
    }
  }

  // 设置存储项
  set<T>(key: string, value: T, customExpiration?: number): boolean {
    try {
      if (!this.isStorageAvailable()) {
        return false;
      }

      const item = this.createStorageItem(value);

      // 使用自定义过期时间
      if (customExpiration && customExpiration > 0) {
        item.expiration = Date.now() + customExpiration;
      }

      const serializedData = this.serialize(item);
      this.storage.setItem(this.getFullKey(key), serializedData);

      return true;
    } catch (error) {
      console.error('Storage set failed:', error);
      return false;
    }
  }

  // 获取存储项
  get<T>(key: string, defaultValue?: T): T | null {
    try {
      if (!this.isStorageAvailable()) {
        return defaultValue ?? null;
      }

      const fullKey = this.getFullKey(key);
      const data = this.storage.getItem(fullKey);

      if (!data) {
        return defaultValue ?? null;
      }

      const item: StorageItem<T> = this.deserialize(data);

      // 检查版本
      if (!this.isVersionValid(item)) {
        this.remove(key);
        return defaultValue ?? null;
      }

      // 检查过期
      if (this.isExpired(item)) {
        this.remove(key);
        return defaultValue ?? null;
      }

      return item.value;
    } catch (error) {
      console.error('Storage get failed:', error);
      return defaultValue ?? null;
    }
  }

  // 删除存储项
  remove(key: string): boolean {
    try {
      if (!this.isStorageAvailable()) {
        return false;
      }

      this.storage.removeItem(this.getFullKey(key));
      return true;
    } catch (error) {
      console.error('Storage remove failed:', error);
      return false;
    }
  }

  // 清除所有带前缀的存储项
  clear(): boolean {
    try {
      if (!this.isStorageAvailable()) {
        return false;
      }

      const keys = this.getKeys();
      keys.forEach(key => this.remove(key));

      return true;
    } catch (error) {
      console.error('Storage clear failed:', error);
      return false;
    }
  }

  // 检查键是否存在且未过期
  has(key: string): boolean {
    return this.get(key) !== null;
  }

  // 获取所有键（去除前缀）
  getKeys(): string[] {
    try {
      if (!this.isStorageAvailable()) {
        return [];
      }

      const keys: string[] = [];
      const prefixLength = this.config.prefix.length;

      for (let i = 0; i < this.storage.length; i++) {
        const fullKey = this.storage.key(i);
        if (fullKey && fullKey.startsWith(this.config.prefix)) {
          keys.push(fullKey.substring(prefixLength));
        }
      }

      return keys;
    } catch (error) {
      console.error('Get keys failed:', error);
      return [];
    }
  }

  // 获取所有存储项
  getAll(): Record<string, any> {
    try {
      const keys = this.getKeys();
      const items: Record<string, any> = {};

      keys.forEach(key => {
        const value = this.get(key);
        if (value !== null) {
          items[key] = value;
        }
      });

      return items;
    } catch (error) {
      console.error('Get all failed:', error);
      return {};
    }
  }

  // 获取存储大小
  getSize(): number {
    try {
      if (!this.isStorageAvailable()) {
        return 0;
      }

      let totalSize = 0;
      const keys = this.getKeys();

      keys.forEach(key => {
        const fullKey = this.getFullKey(key);
        const value = this.storage.getItem(fullKey);
        if (value) {
          totalSize += new Blob([fullKey + value]).size;
        }
      });

      return totalSize;
    } catch (error) {
      console.error('Get size failed:', error);
      return 0;
    }
  }

  // 清理过期项
  cleanExpired(): number {
    try {
      if (!this.isStorageAvailable()) {
        return 0;
      }

      const keys = this.getKeys();
      let cleanedCount = 0;

      keys.forEach(key => {
        const fullKey = this.getFullKey(key);
        const data = this.storage.getItem(fullKey);

        if (data) {
          try {
            const item: StorageItem = this.deserialize(data);

            if (this.isExpired(item) || !this.isVersionValid(item)) {
              this.remove(key);
              cleanedCount++;
            }
          } catch (error) {
            // 如果解析失败，也删除这个项
            this.remove(key);
            cleanedCount++;
          }
        }
      });

      return cleanedCount;
    } catch (error) {
      console.error('Clean expired failed:', error);
      return 0;
    }
  }

  // 设置新的配置
  updateConfig(newConfig: Partial<StorageConfig>): void {
    this.config = { ...this.config, ...newConfig };
  }

  // 导出数据
  export(keys?: string[]): Record<string, any> {
    const keysToExport = keys || this.getKeys();
    const exportData: Record<string, any> = {};

    keysToExport.forEach(key => {
      const value = this.get(key);
      if (value !== null) {
        exportData[key] = value;
      }
    });

    return exportData;
  }

  // 导入数据
  import(data: Record<string, any>, overwrite: boolean = false): number {
    let importedCount = 0;

    Object.entries(data).forEach(([key, value]) => {
      if (overwrite || !this.has(key)) {
        if (this.set(key, value)) {
          importedCount++;
        }
      }
    });

    return importedCount;
  }

  // 备份到另一个存储
  backup(targetStorage: StorageManager): boolean {
    try {
      const allData = this.getAll();
      const importedCount = targetStorage.import(allData, true);
      return importedCount === Object.keys(allData).length;
    } catch (error) {
      console.error('Backup failed:', error);
      return false;
    }
  }
}

// 创建默认实例
export const localStorage = new StorageManager(StorageType.LOCAL);
export const sessionStorage = new StorageManager(StorageType.SESSION);

// 创建加密存储实例
export const secureLocalStorage = new StorageManager(StorageType.LOCAL, {
  encryption: true,
  prefix: 'secure_tarot_',
});

// 创建临时存储实例（30分钟过期）
export const tempStorage = new StorageManager(StorageType.LOCAL, {
  expiration: 30 * 60 * 1000, // 30分钟
  prefix: 'temp_tarot_',
});

// 便捷函数
export const setItem = <T>(key: string, value: T, expiration?: number): boolean => {
  return localStorage.set(key, value, expiration);
};

export const getItem = <T>(key: string, defaultValue?: T): T | null => {
  return localStorage.get(key, defaultValue);
};

export const removeItem = (key: string): boolean => {
  return localStorage.remove(key);
};

export const clearAll = (): boolean => {
  return localStorage.clear();
};

export const hasItem = (key: string): boolean => {
  return localStorage.has(key);
};

// 缓存管理
export class CacheManager {
  private storage: StorageManager;
  private defaultTTL: number;

  constructor(
    storage: StorageManager = localStorage,
    defaultTTL: number = 5 * 60 * 1000 // 5分钟
  ) {
    this.storage = storage;
    this.defaultTTL = defaultTTL;
  }

  set<T>(key: string, value: T, ttl?: number): boolean {
    const expiration = ttl ?? this.defaultTTL;
    return this.storage.set(key, value, expiration);
  }

  get<T>(key: string, defaultValue?: T): T | null {
    return this.storage.get(key, defaultValue);
  }

  remove(key: string): boolean {
    return this.storage.remove(key);
  }

  clear(): boolean {
    return this.storage.clear();
  }

  // 获取或设置缓存（如果不存在则通过回调获取）
  async getOrSet<T>(
    key: string,
    getter: () => Promise<T> | T,
    ttl?: number
  ): Promise<T | null> {
    const cached = this.get<T>(key);

    if (cached !== null) {
      return cached;
    }

    try {
      const value = await getter();
      this.set(key, value, ttl);
      return value;
    } catch (error) {
      console.error('Cache getter failed:', error);
      return null;
    }
  }

  // 缓存装饰器
  memoize<T extends (...args: any[]) => any>(
    fn: T,
    keyGenerator?: (...args: Parameters<T>) => string,
    ttl?: number
  ): T {
    return ((...args: Parameters<T>) => {
      const key = keyGenerator ? keyGenerator(...args) : JSON.stringify(args);
      const cached = this.get(key);

      if (cached !== null) {
        return cached;
      }

      const result = fn(...args);
      this.set(key, result, ttl);
      return result;
    }) as T;
  }
}

// 创建默认缓存实例
export const cache = new CacheManager();

// 定期清理过期项（每小时执行一次）
const setupAutoCleanup = () => {
  const cleanup = () => {
    try {
      localStorage.cleanExpired();
      sessionStorage.cleanExpired();
      tempStorage.cleanExpired();
      secureLocalStorage.cleanExpired();
      console.log('Storage cleanup completed');
    } catch (error) {
      console.error('Storage cleanup failed:', error);
    }
  };

  // 立即执行一次清理
  cleanup();

  // 每小时清理一次
  setInterval(cleanup, 60 * 60 * 1000);
};

// 如果在浏览器环境中，启动自动清理
if (typeof window !== 'undefined') {
  setupAutoCleanup();
}

export { StorageManager };

export default {
  StorageManager,
  localStorage,
  sessionStorage,
  secureLocalStorage,
  tempStorage,
  setItem,
  getItem,
  removeItem,
  clearAll,
  hasItem,
  CacheManager,
  cache,
};