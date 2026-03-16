import {
  ImageQuality,
  ImageMetadata,
  ImageLoadingOptions,
  ImageCacheConfig,
  ImageLoadEvent,
  ImageManager,
  ImageUrlConfig
} from '../types/cardImages';

// 图片缓存接口
interface CacheItem {
  url: string;
  blob: Blob;
  timestamp: number;
  accessCount: number;
  lastAccessed: number;
}

// 图片加载状态
interface LoadingState {
  [url: string]: Promise<Blob>;
}

/**
 * 塔罗牌图片管理服务
 * 负责图片的加载、缓存、预加载和优化
 */
export class TarotImageService implements ImageManager {
  private cache = new Map<string, CacheItem>();
  private loadingStates: LoadingState = {};
  private config: ImageCacheConfig;
  private urlConfig: ImageUrlConfig;

  // 事件监听器
  private eventListeners: {
    load: Array<(event: ImageLoadEvent) => void>;
    error: Array<(event: ImageLoadEvent) => void>;
  } = { load: [], error: [] };

  constructor(
    cacheConfig: ImageCacheConfig = {
      maxSize: 50, // 50MB
      maxAge: 24 * 60 * 60 * 1000, // 24小时
      strategy: 'hybrid'
    },
    urlConfig: ImageUrlConfig = {
      baseUrl: '/images/tarot-cards',
      pathTemplate: '{type}/{suit}/{number:02d}-{name}.{format}',
      qualityParams: {
        thumbnail: 'w=200&h=333',
        standard: 'w=400&h=666',
        high: 'w=800&h=1332',
        ultra: 'w=1200&h=2000'
      }
    }
  ) {
    this.config = cacheConfig;
    this.urlConfig = urlConfig;

    // 定期清理过期缓存
    setInterval(() => this.cleanExpiredCache(), 5 * 60 * 1000); // 5分钟清理一次
  }

  /**
   * 生成图片URL
   */
  getImageUrl(cardId: number, quality: ImageQuality = 'standard'): string {
    // 这里需要根据cardId获取卡片信息
    // 暂时使用模拟数据结构
    const cardInfo = this.getCardInfo(cardId);

    let path = this.urlConfig.pathTemplate
      .replace('{type}', cardInfo.type)
      .replace('{suit}', cardInfo.suit || '')
      .replace('{number:02d}', cardInfo.number.toString().padStart(2, '0'))
      .replace('{number}', cardInfo.number.toString())
      .replace('{name}', cardInfo.name)
      .replace('{format}', 'jpg');

    // 移除双斜杠
    path = path.replace(/\/+/g, '/');

    const baseUrl = this.urlConfig.cdnUrl || this.urlConfig.baseUrl;
    const qualityParam = this.urlConfig.qualityParams?.[quality];

    return qualityParam
      ? `${baseUrl}${path}?${qualityParam}`
      : `${baseUrl}${path}`;
  }

  /**
   * 获取牌背图片URL
   */
  getCardBackUrl(backId: string = 'classic', quality: ImageQuality = 'standard'): string {
    const qualityParam = this.urlConfig.qualityParams?.[quality];
    const baseUrl = this.urlConfig.cdnUrl || this.urlConfig.baseUrl;
    const url = `${baseUrl}/card-backs/${backId}.jpg`;

    return qualityParam ? `${url}?${qualityParam}` : url;
  }

  /**
   * 预加载单张图片
   */
  async preloadImage(cardId: number, quality: ImageQuality = 'standard'): Promise<void> {
    const url = this.getImageUrl(cardId, quality);
    const startTime = Date.now();

    try {
      await this.loadImageBlob(url);

      const loadTime = Date.now() - startTime;
      this.emitEvent('load', {
        cardId,
        quality,
        url,
        success: true,
        loadTime
      });
    } catch (error) {
      const loadTime = Date.now() - startTime;
      this.emitEvent('error', {
        cardId,
        quality,
        url,
        success: false,
        loadTime,
        error: error as Error
      });
      throw error;
    }
  }

  /**
   * 批量预加载图片
   */
  async preloadImages(cardIds: number[], quality: ImageQuality = 'standard'): Promise<void> {
    const batchSize = 5; // 并发加载数量
    const promises: Promise<void>[] = [];

    for (let i = 0; i < cardIds.length; i += batchSize) {
      const batch = cardIds.slice(i, i + batchSize);
      const batchPromises = batch.map(cardId =>
        this.preloadImage(cardId, quality).catch(error => {
          console.warn(`Failed to preload card ${cardId}:`, error);
        })
      );

      promises.push(...batchPromises);

      // 等待当前批次完成再开始下一批次
      if (promises.length >= batchSize) {
        await Promise.all(promises.splice(0, batchSize));
      }
    }

    // 等待剩余任务完成
    if (promises.length > 0) {
      await Promise.all(promises);
    }
  }

  /**
   * 创建图片元素并处理加载
   */
  async loadImage(
    cardId: number,
    quality: ImageQuality = 'standard',
    options: ImageLoadingOptions = {}
  ): Promise<HTMLImageElement> {
    const url = this.getImageUrl(cardId, quality);
    const img = new Image();

    // 设置跨域
    img.crossOrigin = 'anonymous';

    // 懒加载支持
    if (options.lazy && 'loading' in img) {
      img.loading = 'lazy';
    }

    return new Promise((resolve, reject) => {
      const startTime = Date.now();

      img.onload = () => {
        const loadTime = Date.now() - startTime;

        // 缓存图片
        this.cacheImageFromElement(url, img);

        this.emitEvent('load', {
          cardId,
          quality,
          url,
          success: true,
          loadTime
        });

        resolve(img);
      };

      img.onerror = () => {
        const loadTime = Date.now() - startTime;
        const error = new Error(`Failed to load image: ${url}`);

        this.emitEvent('error', {
          cardId,
          quality,
          url,
          success: false,
          loadTime,
          error
        });

        // 尝试降级到更低质量
        if (quality === 'high') {
          this.loadImage(cardId, 'standard', options).then(resolve, reject);
        } else if (quality === 'standard') {
          this.loadImage(cardId, 'thumbnail', options).then(resolve, reject);
        } else if (options.errorFallback) {
          img.src = options.errorFallback;
        } else {
          reject(error);
        }
      };

      // 设置占位图
      if (options.placeholder) {
        img.src = options.placeholder;
      }

      // 开始加载
      img.src = url;
    });
  }

  /**
   * 从Blob加载图片
   */
  private async loadImageBlob(url: string): Promise<Blob> {
    // 检查是否正在加载
    if (url in this.loadingStates) {
      return this.loadingStates[url];
    }

    // 检查缓存
    const cached = this.getCachedItem(url);
    if (cached) {
      this.updateAccessTime(url);
      return cached.blob;
    }

    // 开始加载
    const loadingPromise = this.fetchImageBlob(url);
    this.loadingStates[url] = loadingPromise;

    try {
      const blob = await loadingPromise;
      this.cacheBlob(url, blob);
      return blob;
    } finally {
      delete this.loadingStates[url];
    }
  }

  /**
   * 获取图片Blob
   */
  private async fetchImageBlob(url: string): Promise<Blob> {
    const response = await fetch(url);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const contentType = response.headers.get('content-type');
    if (!contentType?.startsWith('image/')) {
      throw new Error(`Invalid content type: ${contentType}`);
    }

    return response.blob();
  }

  /**
   * 缓存Blob数据
   */
  private cacheBlob(url: string, blob: Blob): void {
    const now = Date.now();

    this.cache.set(url, {
      url,
      blob,
      timestamp: now,
      accessCount: 1,
      lastAccessed: now
    });

    this.enforeCacheLimit();
  }

  /**
   * 从图片元素缓存
   */
  private cacheImageFromElement(url: string, img: HTMLImageElement): void {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    if (!ctx) return;

    canvas.width = img.naturalWidth;
    canvas.height = img.naturalHeight;
    ctx.drawImage(img, 0, 0);

    canvas.toBlob((blob) => {
      if (blob) {
        this.cacheBlob(url, blob);
      }
    }, 'image/jpeg', 0.9);
  }

  /**
   * 获取缓存项
   */
  private getCachedItem(url: string): CacheItem | null {
    const item = this.cache.get(url);

    if (!item) return null;

    // 检查是否过期
    if (Date.now() - item.timestamp > this.config.maxAge) {
      this.cache.delete(url);
      return null;
    }

    return item;
  }

  /**
   * 更新访问时间
   */
  private updateAccessTime(url: string): void {
    const item = this.cache.get(url);
    if (item) {
      item.lastAccessed = Date.now();
      item.accessCount++;
    }
  }

  /**
   * 强制缓存限制
   */
  private enforeCacheLimit(): void {
    const maxSizeBytes = this.config.maxSize * 1024 * 1024;
    let currentSize = 0;

    // 计算当前缓存大小
    Array.from(this.cache.values()).forEach(item => {
      currentSize += item.blob.size;
    });

    if (currentSize <= maxSizeBytes) return;

    // 按LRU清理缓存
    const items = Array.from(this.cache.entries())
      .sort(([, a], [, b]) => a.lastAccessed - b.lastAccessed);

    while (currentSize > maxSizeBytes && items.length > 0) {
      const [url, item] = items.shift()!;
      this.cache.delete(url);
      currentSize -= item.blob.size;
    }
  }

  /**
   * 清理过期缓存
   */
  private cleanExpiredCache(): void {
    const now = Date.now();

    Array.from(this.cache.entries()).forEach(([url, item]) => {
      if (now - item.timestamp > this.config.maxAge) {
        this.cache.delete(url);
      }
    });
  }

  /**
   * 清理所有缓存
   */
  clearCache(): void {
    this.cache.clear();
    this.loadingStates = {};
  }

  /**
   * 获取缓存统计
   */
  getCacheStats(): { size: number; count: number; hitRate: number } {
    let totalSize = 0;
    let totalAccess = 0;
    let totalHits = 0;

    Array.from(this.cache.values()).forEach(item => {
      totalSize += item.blob.size;
      totalAccess += item.accessCount;
      if (item.accessCount > 1) {
        totalHits += item.accessCount - 1;
      }
    });

    return {
      size: Math.round(totalSize / 1024 / 1024 * 100) / 100, // MB
      count: this.cache.size,
      hitRate: totalAccess > 0 ? totalHits / totalAccess : 0
    };
  }

  /**
   * 添加事件监听器
   */
  addEventListener(event: 'load' | 'error', listener: (event: ImageLoadEvent) => void): void {
    this.eventListeners[event].push(listener);
  }

  /**
   * 移除事件监听器
   */
  removeEventListener(event: 'load' | 'error', listener: (event: ImageLoadEvent) => void): void {
    const index = this.eventListeners[event].indexOf(listener);
    if (index > -1) {
      this.eventListeners[event].splice(index, 1);
    }
  }

  /**
   * 触发事件
   */
  private emitEvent(event: 'load' | 'error', data: ImageLoadEvent): void {
    this.eventListeners[event].forEach(listener => {
      try {
        listener(data);
      } catch (error) {
        console.error('Error in image event listener:', error);
      }
    });
  }

  /**
   * 获取卡片信息 (模拟函数，实际应该从数据源获取)
   */
  private getCardInfo(cardId: number): { type: string; suit: string; number: number; name: string } {
    // 这里应该有实际的卡片数据查询逻辑
    // 暂时返回模拟数据
    if (cardId <= 22) {
      // 大阿卡纳
      return {
        type: 'major-arcana',
        suit: '',
        number: cardId,
        name: `major-${cardId.toString().padStart(2, '0')}`
      };
    } else {
      // 小阿卡纳
      const suits = ['wands', 'cups', 'swords', 'pentacles'];
      const suitIndex = Math.floor((cardId - 23) / 14);
      const cardNumber = ((cardId - 23) % 14) + 1;

      return {
        type: 'minor-arcana',
        suit: suits[suitIndex] || 'wands',
        number: cardNumber,
        name: `${suits[suitIndex]}-${cardNumber.toString().padStart(2, '0')}`
      };
    }
  }
}

// 单例实例
export const imageService = new TarotImageService();

export default imageService;