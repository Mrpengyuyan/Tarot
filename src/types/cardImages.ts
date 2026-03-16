// 塔罗牌图片相关类型定义

// 图片质量级别
export type ImageQuality = 'thumbnail' | 'standard' | 'high' | 'ultra';

// 图片尺寸配置
export interface ImageDimensions {
  width: number;
  height: number;
}

// 图片元数据
export interface ImageMetadata {
  url: string;
  dimensions: ImageDimensions;
  fileSize?: number;
  format: 'jpg' | 'png' | 'webp';
  quality: ImageQuality;
}

// 塔罗牌图片资源
export interface TarotCardImages {
  // 主图片 - 不同质量级别
  main: {
    thumbnail: ImageMetadata;    // 200x333 缩略图
    standard: ImageMetadata;     // 400x666 标准图
    high: ImageMetadata;         // 800x1332 高清图
    ultra?: ImageMetadata;       // 1200x2000 超高清图（可选）
  };

  // 牌背图片
  back: {
    thumbnail: ImageMetadata;
    standard: ImageMetadata;
    high: ImageMetadata;
  };

  // 特殊效果图片（可选）
  effects?: {
    glow?: ImageMetadata;        // 发光效果边框
    shadow?: ImageMetadata;      // 阴影效果
    holographic?: ImageMetadata; // 全息效果
  };
}

// 扩展的塔罗牌类型，包含图片信息
export interface TarotCardWithImages {
  id: number;
  name_zh: string;
  name_en: string;
  card_number: number;
  card_type: 'major_arcana' | 'minor_arcana';
  suit?: string | null;

  // 原有含义字段
  upright_meaning?: string;
  reversed_meaning?: string;
  description?: string;
  keywords_upright?: string;
  keywords_reversed?: string;

  // 图片资源
  images: TarotCardImages;

  // 图片加载状态
  imageLoadingState?: {
    thumbnail: 'loading' | 'loaded' | 'error';
    standard: 'loading' | 'loaded' | 'error';
    high: 'loading' | 'loaded' | 'error';
  };
}

// 图片加载配置
export interface ImageLoadingOptions {
  quality?: ImageQuality;
  lazy?: boolean;              // 是否懒加载
  preload?: boolean;           // 是否预加载
  placeholder?: string;        // 占位图片URL
  errorFallback?: string;      // 错误时的备用图片
  retryAttempts?: number;      // 重试次数
}

// 图片缓存配置
export interface ImageCacheConfig {
  maxSize: number;             // 缓存大小限制（MB）
  maxAge: number;              // 缓存时间（毫秒）
  strategy: 'memory' | 'storage' | 'hybrid';
}

// 塔罗牌套装配置
export interface TarotDeckConfig {
  id: string;
  name: string;
  description: string;

  // 套装信息
  totalCards: number;
  majorArcanaCount: number;
  minorArcanaCount: number;

  // 图片风格配置
  style: {
    theme: 'classic' | 'modern' | 'mystical' | 'minimalist';
    colorScheme: 'gold' | 'silver' | 'rainbow' | 'monochrome';
    artistInfo?: {
      name: string;
      website?: string;
    };
  };

  // 牌背选择
  cardBacks: Array<{
    id: string;
    name: string;
    images: Pick<TarotCardImages, 'back'>;
  }>;

  // 版权信息
  copyright?: {
    year: number;
    holder: string;
    license: string;
  };
}

// 图片URL生成器配置
export interface ImageUrlConfig {
  baseUrl: string;
  cdnUrl?: string;
  pathTemplate: string;        // 例如: "{type}/{suit}/{number}-{name}.{format}"
  qualityParams?: Record<ImageQuality, string>;
}

// 图片预加载策略
export type PreloadStrategy =
  | 'none'                     // 不预加载
  | 'visible'                  // 只预加载可见卡片
  | 'deck'                     // 预加载整个牌组
  | 'smart';                   // 智能预加载（基于使用模式）

// 图片加载事件
export interface ImageLoadEvent {
  cardId: number;
  quality: ImageQuality;
  url: string;
  success: boolean;
  loadTime: number;
  error?: Error;
}

// 图片管理器接口
export interface ImageManager {
  // 获取图片URL
  getImageUrl(cardId: number, quality: ImageQuality): string;

  // 预加载图片
  preloadImage(cardId: number, quality: ImageQuality): Promise<void>;

  // 批量预加载
  preloadImages(cardIds: number[], quality: ImageQuality): Promise<void>;

  // 清理缓存
  clearCache(): void;

  // 获取缓存状态
  getCacheStats(): {
    size: number;
    count: number;
    hitRate: number;
  };
}

export default TarotCardWithImages;